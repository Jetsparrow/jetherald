using System.Threading;
using Microsoft.Extensions.Hosting;

namespace JetHerald.Services;
public class HeartMonitor : BackgroundService
{
    public HeartMonitor(
        Db db,
        JetHeraldBot herald,
        ILogger<HeartMonitor> log)
    {
        Db = db;
        Herald = herald;
        Log = log;
    }

    Db Db { get; }
    JetHeraldBot Herald { get; }
    ILogger<HeartMonitor> Log { get; }
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(1000 * 10, token);
            try
            {
                using var ctx = await Db.GetContext();
                var attacks = await ctx.ProcessHearts();
                foreach (var a in attacks)
                {
                    await Herald.BroadcastMessageRaw(
                        a.TopicId,
                        $"!{a.Description}!:\nHeart \"{a.Heart}\" stopped beating at {a.CreateTs:O}");

                    await ctx.MarkHeartAttackReported(a.HeartEventId);
                    
                    if (token.IsCancellationRequested)
                        return;
                }
            }
            catch (Exception e)
            {
                Log.LogError(e, "Exception while checking heartbeats");
            }
        }
    }
}
