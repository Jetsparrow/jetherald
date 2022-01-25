using JetHerald.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace JetHerald.Commands;
public class ListCommand : IChatCommand
{
    Db Db { get; }
    TelegramBotClient Bot { get; }

    public ListCommand(Db db, TelegramBotClient bot)
    {
        Db = db;
        Bot = bot;
    }

    public async Task<string> Execute(CommandString cmd, Update update)
    {
        if (!await CommandHelper.CheckAdministrator(Bot, update.Message))
            return null;

        var msg = update.Message;
        var chatid = msg.Chat.Id;
        var topics = await Db.GetTopicsForSub(NamespacedId.Telegram(chatid));

        return topics.Any()
            ? "Topics:\n" + string.Join("\n", topics)
            : "No subscriptions active.";
    }
}
