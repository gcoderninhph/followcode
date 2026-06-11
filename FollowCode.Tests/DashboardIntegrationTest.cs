using System.Diagnostics;
using FollowCode.SDK;

namespace FollowCode.Tests;

public class DashboardIntegrationTest
{
    private MyObj? cpu = new MyObj { Name = "CPU", Value = 45 };
    private MyObj? mem = new MyObj { Name = "Memory", Value = 72 };
    private MyObj? dick = new MyObj { Name = "Dick", Value = 0 };

    private Dictionary<string, MyObj> a = new();

    [Fact]
    public async Task Track_WeakReference_AutoCleanup_WhenObjectGoesOutOfScope()
    {
        // ============================================================
        //  API duy nhất: client.Track(key, data)
        //  Không có StartAsync, StopAsync, Untrack, Clear, CollectGarbage
        //  SDK tự gửi định kỳ, tự dọn object khi WeakReference chết
        // ============================================================

        using var client = new FollowCodeClient(new FollowCodeConfig
        {
            ServerUrl = "http://localhost:42102",
            IntervalSeconds = 1
        });

        // ---- Phase 1: Track dictionary ----
        client.Track("Dic", a);
        await Task.Delay(2000);

        a["cpu"] = cpu!;
        a["mem"] = mem!;
        await Task.Delay(2000);

        // ---- Phase 2: Clear dictionary ----
        a.Clear();
        await Task.Delay(2000);

        // ---- Phase 3: Add new entry ----
        a["dick"] = dick!;
        await Task.Delay(2000);
        dick!.Value = 10000;
        await Task.Delay(10000);
    }

}

public class MyObj
{
    public string Name { get; set; } = "";
    public int Value { get; set; }

    public override string ToString() => $"{Name}:{Value}";
}
