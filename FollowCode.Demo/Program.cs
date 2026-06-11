using FollowCode.SDK;

using var client = new FollowCodeClient(new FollowCodeConfig
{
    ServerUrl = "http://localhost:5000",
    IntervalSeconds = 1
});

// ===== Track objects =====
var cpu = new MyObj { Name = "CPU", Value = 45 };
var mem = new MyObj { Name = "Memory", Value = 72 };
var order = new MyObj { Name = "Order", Value = 1001 };

client.Track("srv-cpu", cpu);
client.Track("srv-mem", mem);
client.Track("ord-1001", order);

Console.WriteLine("=== FollowCode SDK — Track only ===");
Console.WriteLine("Tracked: srv-cpu | srv-mem | ord-1001");
await Task.Delay(2000);

// ===== Direct mutation =====
cpu.Value = 88;
mem.Value = 55;
order.Value = 2002;
Console.WriteLine("Mutated: CPU->88 | Memory->55 | Order->2002");
await Task.Delay(2000);

// ===== Track via scope → auto-cleanup when GC runs =====
TrackScoped(client, "temp-X");
TrackScoped(client, "temp-Y");
Console.WriteLine("Scoped objects tracked — will auto-cleanup when GC'd");
await Task.Delay(5000);

// ===== Remaining objects still alive =====
cpu.Value = 99;
Console.WriteLine("CPU->99 — dashboard still shows srv-cpu, srv-mem, ord-1001");
Console.WriteLine("temp-X & temp-Y auto-removed by SDK");
await Task.Delay(2000);

Console.WriteLine("Done. Just Track(). SDK handles everything else.");

static void TrackScoped(FollowCodeClient client, string key)
{
    var obj = new MyObj { Name = "Temp", Value = key.Length * 10 };
    client.Track(key, obj);
}

public class MyObj
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
    public override string ToString() => $"{Name}:{Value}";
}
