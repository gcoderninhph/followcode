using System.Net.Http;
using System.Collections;

#if UNITY_ENGINE
using UnityEngine.Networking;
using Newtonsoft.Json;
#else
using System.Net.Http.Json;
using System.Timers;
#endif

namespace FollowCode.SDK
{
    public class FollowCodeClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, TrackedObject> _objects = new();
        private readonly int _intervalMs;
        private readonly string _serverUrl;

#if UNITY_ENGINE
        private CancellationTokenSource? _cts;
#else
        private readonly System.Timers.Timer _timer;
#endif

        public FollowCodeClient(FollowCodeConfig config)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            _serverUrl = config.ServerUrl;
            _intervalMs = config.IntervalSeconds * 1000;

            _httpClient = new HttpClient { BaseAddress = new Uri(config.ServerUrl) };
            _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FollowCode-SDK/1.0");

#if UNITY_ENGINE
            _cts = new CancellationTokenSource();
            _ = TimerLoop(_cts.Token);
#else
            _timer = new System.Timers.Timer(_intervalMs);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();
#endif
        }

#if !UNITY_ENGINE
        internal FollowCodeClient(FollowCodeConfig config, HttpMessageHandler handler)
        {
            _serverUrl = config.ServerUrl;
            _intervalMs = config.IntervalSeconds * 1000;

            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(config.ServerUrl) };
            _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

            _timer = new System.Timers.Timer(_intervalMs);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }
#endif

        public void Track(string key, object data)
        {
            _objects[key] = new TrackedObject
            {
                Key = key,
                Data = data,
                UpdatedAt = DateTime.UtcNow
            };

            _ = SendObjectsAsync();
        }

#if UNITY_ENGINE
        private async Task TimerLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(_intervalMs, ct); }
                catch { break; }

                if (ct.IsCancellationRequested) break;

                GC.Collect();
                await SendObjectsAsync();
            }
        }
#else
        private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            GC.Collect();
            await SendObjectsAsync();
        }
#endif

        private async Task SendObjectsAsync()
        {
            try
            {
                CleanDeadObjects();
                var payload = BuildPayload();

#if UNITY_ENGINE
                var json = JsonConvert.SerializeObject(payload);
                var url = _serverUrl.TrimEnd('/') + "/api/objects";
                using var req = UnityWebRequest.Put(url, json);
                req.method = "POST";
                req.SetRequestHeader("Content-Type", "application/json");

                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result != UnityWebRequest.Result.Success)
                    Console.WriteLine($"[SDK] Send failed: {req.error}");
#else
                await _httpClient.PostAsJsonAsync("/api/objects", payload);
#endif
            }
            catch { }
        }

        private int CleanDeadObjects()
        {
            var deadKeys = new List<string>();
            foreach (var kvp in _objects)
            {
                if (!kvp.Value.IsAlive)
                    deadKeys.Add(kvp.Key);
            }
            foreach (var key in deadKeys)
                _objects.Remove(key);
            if (deadKeys.Count > 0)
                Console.WriteLine($"[SDK] Auto-removed {deadKeys.Count} dead object(s)");
            return deadKeys.Count;
        }

        private List<object> BuildPayload()
        {
            var payload = new List<object>();
            foreach (var kvp in _objects)
            {
                if (kvp.Value.IsAlive)
                {
                    var data = kvp.Value.Data;
                    payload.Add(new
                    {
                        key = kvp.Key,
                        data = FormatData(data),
                        updatedAt = kvp.Value.UpdatedAt
                    });
                }
            }
            return payload;
        }

        private static string FormatData(object? data)
        {
            if (data is null) return "null";

            if (data is IDictionary dict)
            {
                var entries = new List<string>();
                foreach (DictionaryEntry entry in dict)
                    entries.Add($"  {entry.Key}: {entry.Value}");
                return dict.Count == 0 ? "{ }" : "{\n" + string.Join("\n", entries) + "\n}";
            }

            if (data is string str)
                return str;

            if (data is IEnumerable enumerable)
            {
                var items = new List<string>();
                foreach (var item in enumerable)
                    items.Add($"  {item?.ToString() ?? "null"}");
                return "[\n" + string.Join("\n", items) + "\n]";
            }

            return data.ToString() ?? "null";
        }

        public void Dispose()
        {
#if UNITY_ENGINE
            _cts?.Cancel();
            _cts?.Dispose();
#else
            _timer?.Dispose();
#endif
            _objects.Clear();
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}