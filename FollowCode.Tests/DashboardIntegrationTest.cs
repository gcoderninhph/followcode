using System.Diagnostics;
using FollowCode.SDK;

namespace FollowCode.Tests;

public class DashboardIntegrationTest
{
    private MyObj? cpu = new MyObj { Name = "CPU", Value = 45 };
    private MyObj? mem = new MyObj { Name = "Memory", Value = 72 };
    private MyObj? dick = new MyObj { Name = "Dick", Value = 000 };

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

        // ---- Phase 1: Track objects in THIS method scope ----
        // These objects are alive as long as we hold references.


        client.Track("srv-cpu", cpu!);
        client.Track("srv-mem", mem!);

        // --> phải hiển thị lên app python
        await Task.Delay(2000);
        cpu!.Value = 90;
        mem!.Value = 33;
        await Task.Delay(2000);

        // --> trên python app thấy được thay đổi dữ liệu
        ClearObject();
        await Task.Delay(2000);
        // --> biến mất trên python app

        client.Track("Dick", dick!);
        // --> hiển thị Dick
        await Task.Delay(2000); // Give runtime GC time to run naturally
        dick!.Value = 10000;
        // --> python app hiển thị thay đổi của dick

        await Task.Delay(10000);

    }

    private void ClearObject()
    {
        cpu = null;
        mem = null;
    }

}

public class MyObj
{
    public string Name { get; set; } = "";
    public int Value { get; set; }

    public override string ToString() => $"{Name}:{Value}";
}
