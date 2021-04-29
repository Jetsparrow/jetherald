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
            var debtLimit = DateTime.Now.AddSeconds(config.DebtLimitSeconds);
            var time = expiryDates.GetValueOrDefault(key, DateTime.Now);
            Console.WriteLine(time);
            return time > debtLimit;
        }

        public void ApplyCost(uint key, int cost)
        {
            expiryDates.AddOrUpdate(key,
                key => DateTime.Now.AddSeconds(cost),
                (key, oldDebt) =>
                {
                    if (oldDebt < DateTime.Now)
                        return DateTime.Now.AddSeconds(cost);

                    return oldDebt.AddSeconds(cost);
                });
        }
    }
}