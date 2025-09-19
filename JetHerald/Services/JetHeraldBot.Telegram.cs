using System.Threading;

using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;

using JetHerald.Commands;

namespace JetHerald.Services;
public partial class JetHeraldBot
{
    TelegramBotClient Client { get; set; }
    Telegram.Bot.Types.User Me { get; set; }
    ChatCommandRouter Commands;
    CancellationTokenSource TelegramBotShutdownToken { get; } = new(); 
    async Task StartTelegram()
    {
        if (string.IsNullOrWhiteSpace(TelegramConfig.ApiKey))
            return;

        Client = new TelegramBotClient(TelegramConfig.ApiKey);
        Me = await Client.GetMeAsync();

        Log.LogInformation("Connected to Telegram as {username}, id:{id})", Me.Username, Me.Id);

        Commands = new ChatCommandRouter(Me.Username, Log);
        Commands.Add(new SubscribeCommand(Db, Client), "subscribe", "sub");
        Commands.Add(new UnsubscribeCommand(Db, Client), "unsubscribe", "unsub");
        Commands.Add(new ListCommand(Db, Client), "list");

        var receiverOptions = new ReceiverOptions { AllowedUpdates = new[] { UpdateType.Message } };
        Client.StartReceiving(
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
        return Client.SendTextMessageAsync(id, formatted);
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
