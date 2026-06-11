namespace FollowCode.SDK
{
    public class FollowCodeConfig
    {
        public string ServerUrl { get; set; } = "http://localhost:42102";
        public int IntervalSeconds { get; set; } = 5;
        public int TimeoutSeconds { get; set; } = 10;
    }
}
