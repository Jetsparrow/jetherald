using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace JetHerald
{
    public class LeakyBucket
    {
        private readonly ConcurrentDictionary<uint, DateTime> expiryDates = new();
        private readonly Options.Timeout config;

        public LeakyBucket(IOptions<Options.Timeout> cfgOptions)
        {
            config = cfgOptions.Value;
        }

        public bool IsTimedOut(uint key)
        {
            var now = DateTime.UtcNow;
            var debtLimit = now.AddSeconds(config.DebtLimitSeconds);
            var time = expiryDates.GetValueOrDefault(key, now);
            Console.WriteLine(time);
            return time > debtLimit;
        }

        public void ApplyCost(uint key, int cost)
        {
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
}