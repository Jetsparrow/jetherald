namespace JetHerald.Options
{
    public class ConnectionStrings
    {
        public string DefaultConnection { get; set; }
    }

    public class Telegram
    {
        public string ApiKey { get; set; }

        public bool UseProxy { get; set; }
        public string ProxyUrl { get; set; }
        public string ProxyPassword { get; set; }
        public string ProxyLogin { get; set; }
    }

    public class Discord
    {
        public string Token { get; set; }
    }

    public class Timeout
    {
        public int DebtLimitSeconds { get; set; }
        public int HeartbeatCost { get; set; }
        public int ReportCost { get; set; }
    }
}
