using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using JetHerald.Commands;
using Telegram.Bot.Types.ReplyMarkups;

namespace JetHerald.Services;
public partial class JetHeraldBot
{
    TelegramBotClient TelegramBot { get; set; }
    Telegram.Bot.Types.User Me { get; set; }
    ChatCommandRouter Commands;
    CancellationTokenSource TelegramBotShutdownToken { get; } = new(); 
    async Task StartTelegram()
    {
        TelegramBot = new TelegramBotClient(TelegramConfig.ApiKey);
        Me = await TelegramBot.GetMeAsync();

        Commands = new ChatCommandRouter(Me.Username, Log);
        Commands.Add(new CreateTopicCommand(Db), "createtopic");
        Commands.Add(new DeleteTopicCommand(Db), "deletetopic");
        Commands.Add(new SubscribeCommand(Db, TelegramBot), "subscribe", "sub");
        Commands.Add(new UnsubscribeCommand(Db, TelegramBot), "unsubscribe", "unsub");
        Commands.Add(new ListCommand(Db, TelegramBot), "list");

        var receiverOptions = new ReceiverOptions { AllowedUpdates = new[] { UpdateType.Message } };
        TelegramBot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            TelegramBotShutdownToken.Token);
    }

    Task StopTelegram()
    {
        TelegramBotShutdownToken.Cancel();
        return Task.CompletedTask;
    }

    Task SendMessageToTelegramChannel(NamespacedId chat, string formatted)
    {
        var id = long.Parse(chat.Id);
        return TelegramBot.SendTextMessageAsync(id, formatted);
    }

    Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };
        Log.LogError(ErrorMessage);
        return Task.CompletedTask;
    }

    async Task HandleUpdateAsync(ITelegramBotClient sender, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update?.Message?.Type != MessageType.Text)
            return;
        var msg = update.Message!;
        try
        {
            var reply = await Commands.Execute(sender, update);
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
