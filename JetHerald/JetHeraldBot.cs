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
    public class JetHeraldBot
    {
        Db Db { get; set; }
        Options.Telegram Config { get; }
        ILogger<JetHeraldBot> Log { get; }

        public JetHeraldBot(Db db, IOptions<Options.Telegram> cfg, ILogger<JetHeraldBot> log)
        {
            Db = db;
            Config = cfg.Value;
            Log = log;
        }

        TelegramBotClient Client { get; set; }
        ChatCommandRouter Commands;
        CancellationTokenSource HeartbeatCancellation;
        Task HeartbeatTask;
        Telegram.Bot.Types.User Me { get; set; }

        public async Task Init()
        {
            if (Config.UseProxy)
            {
                var httpProxy = new WebProxy(Config.ProxyUrl)
                { Credentials = new NetworkCredential(Config.ProxyLogin, Config.ProxyPassword) };
                Client = new TelegramBotClient(Config.ApiKey, httpProxy);
            }
            else
            {
                Client = new TelegramBotClient(Config.ApiKey);
            }
            Me = await Client.GetMeAsync();

            Commands = new ChatCommandRouter(Me.Username, Log);
            Commands.Add(new CreateTopicCommand(Db), "createtopic");
            Commands.Add(new DeleteTopicCommand(Db), "deletetopic");
            Commands.Add(new SubscribeCommand(Db), "subscribe", "sub");
            Commands.Add(new UnsubscribeCommand(Db), "unsubscribe", "unsub");
            Commands.Add(new ListCommand(Db), "list");

            HeartbeatCancellation = new();
            HeartbeatTask = CheckHeartbeats(HeartbeatCancellation.Token);

            Client.OnMessage += BotOnMessageReceived;
            Client.StartReceiving();
        }

        public async Task Stop()
        {
            Client.StopReceiving();
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

                foreach (var chatSent in await Db.GetExpiredTopics(token))
                {
                    var formatted = $"!{chatSent.Description}!:\nTimeout expired at {chatSent.ExpiryTime}";
                    await Client.SendTextMessageAsync(chatSent.ChatId, formatted, cancellationToken: token);
                }

                await Db.MarkExpiredTopics(token);
            }
        }

        public async Task HeartbeatSent(Db.Topic topic)
        {
            var chatIds = await Db.GetChatIdsForTopic(topic.TopicId);
            var formatted = $"!{topic.Description}!:\nA heartbeat has been sent.";
            foreach (var c in chatIds)
                await Client.SendTextMessageAsync(c, formatted);
        }

        public async Task PublishMessage(Db.Topic topic, string message)
        {
            var chatIds = await Db.GetChatIdsForTopic(topic.TopicId);
            var formatted = $"|{topic.Description}|:\n{message}";
            foreach (var c in chatIds)
                await Client.SendTextMessageAsync(c, formatted);
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
                    await Client.SendTextMessageAsync(
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
