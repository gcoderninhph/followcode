using System.Net.Http.Json;
using System.Timers;
using System.Net.Http;

namespace FollowCode.SDK;

public class FollowCodeClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, TrackedObject> _objects = new();
    private readonly System.Timers.Timer _timer;

    public FollowCodeClient(FollowCodeConfig config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));

        _httpClient = new HttpClient { BaseAddress = new Uri(config.ServerUrl) };
        _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "FollowCode-SDK/1.0");

        _timer = new System.Timers.Timer(config.IntervalSeconds * 1000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Start();
    }

    internal FollowCodeClient(FollowCodeConfig config, HttpMessageHandler handler)
    {
        _httpClient = new HttpClient(handler) { BaseAddress = new Uri(config.ServerUrl) };
        _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

        _timer = new System.Timers.Timer(config.IntervalSeconds * 1000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Start();
    }

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

    private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        GC.Collect();
        await SendObjectsAsync();
    }

    private async Task SendObjectsAsync()
    {
        try
        {
            CleanDeadObjects();
            var payload = BuildPayload();
            await _httpClient.PostAsJsonAsync("/api/objects", payload);
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
                    data = data?.ToString() ?? "null",
                    updatedAt = kvp.Value.UpdatedAt
                });
            }
        }
        return payload;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _objects.Clear();
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
