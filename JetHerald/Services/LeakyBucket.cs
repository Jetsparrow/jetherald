using System.Collections.Concurrent;
namespace JetHerald.Services;
public class LeakyBucket
{
    private readonly ConcurrentDictionary<uint, DateTime> expiryDates = new();
    private readonly Options.TimeoutConfig config;
    private readonly ILogger log;

    public LeakyBucket(IOptions<Options.TimeoutConfig> cfgOptions, ILogger<LeakyBucket> log)
    {
        config = cfgOptions.Value;
        this.log = log;
    }

    public double GetUtilization(uint key)
    {
        var now = DateTime.UtcNow;
        var cur = expiryDates.GetValueOrDefault(key, now);
        var util = (cur - now).TotalSeconds / config.DebtLimitSeconds;
        return Math.Clamp(util, 0, 1);
    }

    public bool IsTimedOut(uint key)
    {
        var now = DateTime.UtcNow;
        var debtLimit = now.AddSeconds(config.DebtLimitSeconds);
        var time = expiryDates.GetValueOrDefault(key, now);
        log.LogTrace("{key} had current timedebt of {time}", key, time);
        return time > debtLimit;
    }

    public void ApplyCost(uint key, double cost)
    {
        if (cost <= 0) return;
        expiryDates.AddOrUpdate(key,
            key => DateTime.UtcNow.AddSeconds(cost),
            (key, oldDebt) =>
            {
                var now = DateTime.UtcNow;
                if (oldDebt < now)
                    return now.AddSeconds(cost);

                return oldDebt.AddSeconds(cost);
            });
    }
}
