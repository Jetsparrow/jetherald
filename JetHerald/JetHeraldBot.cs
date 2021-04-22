using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;

namespace JetHerald
{
    public partial class JetHeraldBot
    {
        Db Db { get; set; }
        Options.Telegram TelegramConfig { get; }
        Options.Discord DiscordConfig { get; }
        ILogger<JetHeraldBot> Log { get; }
        ILoggerFactory LoggerFactory { get; }
        IServiceProvider ServiceProvider { get; }

        public JetHeraldBot(Db db, IOptions<Options.Telegram> telegramCfg, IOptions<Options.Discord> discordCfg, ILogger<JetHeraldBot> log, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            Db = db;
            TelegramConfig = telegramCfg.Value;
            DiscordConfig = discordCfg.Value;

            Log = log;
            LoggerFactory = loggerFactory;
            ServiceProvider = serviceProvider;
        }

        CancellationTokenSource HeartbeatCancellation;
        Task HeartbeatTask;

        public async Task Init()
        {
            await InitTelegram();
            await InitDiscord();
        }

        public async Task Stop()
        {
            await DiscordBot.DisconnectAsync();
            TelegramBot.StopReceiving();
            HeartbeatCancellation.Cancel();
            try
            {
                await HeartbeatTask;
            }
            catch (TaskCanceledException)
            {

            }
        }

        public async Task CheckHeartbeats(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000 * 10, token);
                try
                {
                    var attacks = await Db.ProcessHeartAttacks();
                    foreach (var attack in attacks)
                    {
                        var chats = await Db.GetChatsForTopic(attack.TopicId);
                        foreach (var chat in chats)
                            await SendMessageImpl(chat, $"!{attack.Description}!:\nHeart \"{attack.Heart}\" stopped beating at {attack.ExpiryTime}");
                        await Db.MarkHeartAttackReported(attack.HeartattackId);
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

        public Task HeartbeatSent(Db.Topic topic, string heart)
            => BroadcastMessageImpl(topic, $"!{topic.Description}!:\nHeart \"{heart}\" has started beating.");

        public Task PublishMessage(Db.Topic topic, string message)
            => BroadcastMessageImpl(topic, $"|{topic.Description}|:\n{message}");

        async Task BroadcastMessageImpl(Db.Topic topic, string formatted)
        {
            var chatIds = await Db.GetChatsForTopic(topic.TopicId);
            foreach (var c in chatIds)
                await SendMessageImpl(c, formatted);
        }

        async Task SendMessageImpl(NamespacedId chat, string formatted)
        {
            try
            {
                if (chat.Namespace == "telegram")
                {
                    await SendMessageToTelegramChannel(chat, formatted);
                }
                else if (chat.Namespace == "discord")
                {
                    await SendMessageToDiscordChannel(chat, formatted);
                }
            }
            catch (Exception e) { Log.LogError(e, $"Error while sending message \"{formatted}\" to {chat}"); }
        }
    }
}
