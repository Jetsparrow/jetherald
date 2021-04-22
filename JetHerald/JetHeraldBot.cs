using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

using JetHerald.Commands;
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

        TelegramBotClient TelegramBot { get; set; }
        ChatCommandRouter Commands;
        CancellationTokenSource HeartbeatCancellation;
        Task HeartbeatTask;
        Telegram.Bot.Types.User Me { get; set; }

        public async Task Init()
        {
            if (TelegramConfig.UseProxy)
            {
                var httpProxy = new WebProxy(TelegramConfig.ProxyUrl)
                { Credentials = new NetworkCredential(TelegramConfig.ProxyLogin, TelegramConfig.ProxyPassword) };
                TelegramBot = new TelegramBotClient(TelegramConfig.ApiKey, httpProxy);
            }
            else
            {
                TelegramBot = new TelegramBotClient(TelegramConfig.ApiKey);
            }
            Me = await TelegramBot.GetMeAsync();

            Commands = new ChatCommandRouter(Me.Username, Log);
            Commands.Add(new CreateTopicCommand(Db), "createtopic");
            Commands.Add(new DeleteTopicCommand(Db), "deletetopic");
            Commands.Add(new SubscribeCommand(Db), "subscribe", "sub");
            Commands.Add(new UnsubscribeCommand(Db), "unsubscribe", "unsub");
            Commands.Add(new ListCommand(Db), "list");

            HeartbeatCancellation = new();
            HeartbeatTask = CheckHeartbeats(HeartbeatCancellation.Token);

            TelegramBot.OnMessage += BotOnMessageReceived;
            TelegramBot.StartReceiving();

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
                    foreach (var chatSent in await Db.GetExpiredTopics(token))
                        await SendMessageImpl(chatSent.Chat, $"!{chatSent.Description}!:\nTimeout expired at {chatSent.ExpiryTime}");

                    await Db.MarkExpiredTopics(token);
                }
                catch (Exception e)
                {
                    Log.LogError(e, "Exception while checking heartbeats");
                }
            }
        }

        public Task HeartbeatSent(Db.Topic topic)
            => BroadcastMessageImpl(topic, $"!{topic.Description}!:\nA heartbeat has been sent.");

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
                    await TelegramBot.SendTextMessageAsync(long.Parse(chat.Id), formatted);
                }
                else if (chat.Namespace == "discord")
                {
                    await SendMessageToDiscordChannel(chat, formatted);
                }
            }
            catch (Exception e) { Log.LogError(e, $"Error while sending message \"{formatted}\" to {chat}"); }
        }

        async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var msg = messageEventArgs.Message;
            if (msg == null || msg.Type != MessageType.Text)
                return;

            try
            {
                var reply = await Commands.Execute(sender, messageEventArgs);
                if (reply != null)
                    await TelegramBot.SendTextMessageAsync(
                        chatId: msg.Chat.Id,
                        text: reply,
                        replyToMessageId: msg.MessageId);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Exception occured during handling of command: " + msg.Text);
            }
        }
    }
}
