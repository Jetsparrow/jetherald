using System;
using System.Net;
using System.Threading.Tasks;
using JetHerald.Commands;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace JetHerald
{
    public partial class JetHeraldBot
    {
        TelegramBotClient TelegramBot { get; set; }
        Telegram.Bot.Types.User Me { get; set; }
        ChatCommandRouter Commands;

        async Task InitTelegram()
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

            TelegramBot.OnMessage += TelegramMessageReceived;
            TelegramBot.StartReceiving();
        }

        Task SendMessageToTelegramChannel(NamespacedId chat, string formatted)
        {
            var id = long.Parse(chat.Id);
            return TelegramBot.SendTextMessageAsync(id, formatted);
        }

        async void TelegramMessageReceived(object sender, MessageEventArgs messageEventArgs)
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