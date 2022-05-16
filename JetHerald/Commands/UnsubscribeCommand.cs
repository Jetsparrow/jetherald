using Telegram.Bot;
using Telegram.Bot.Types;
using JetHerald.Services;

namespace JetHerald.Commands;
public class UnsubscribeCommand : IChatCommand
{
    Db Db { get; }
    TelegramBotClient Bot { get; }

    public UnsubscribeCommand(Db db, TelegramBotClient bot)
    {
        Db = db;
        Bot = bot;
    }

    public async Task<string> Execute(CommandString cmd, Update update)
    {
        if (cmd.Parameters.Length < 1)
            return null;

        if (!await CommandHelper.CheckAdministrator(Bot, update.Message))
            return null;

        var msg = update.Message;
        var chat = NamespacedId.Telegram(msg.Chat.Id);

        var topicName = cmd.Parameters[0];
        using var ctx = await Db.GetContext();
        int affected = await ctx.RemoveSubscription(topicName, chat);
        ctx.Commit();
        if (affected >= 1)
            return $"unsubscribed from {topicName}";
        else
            return $"could not find subscription for {topicName}";
    }
}
