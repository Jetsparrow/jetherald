namespace JetHerald.Options;

public class ConnectionStrings
{
    public string DefaultConnection { get; set; }
}

public class TelegramConfig
{
    public string ApiKey { get; set; }
}

public class DiscordConfig
{
    public string Token { get; set; }
}

public class TimeoutConfig
{
    public int DebtLimitSeconds { get; set; }
    public int HeartbeatCost { get; set; }
    public int ReportCost { get; set; }
}
