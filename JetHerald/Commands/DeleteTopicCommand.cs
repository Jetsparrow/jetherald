using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using JetHerald.Services;

namespace JetHerald.Commands;
public class DeleteTopicCommand : IChatCommand
{
    Db Db { get; }

    public DeleteTopicCommand(Db db)
    {
        Db = db;
    }

    public async Task<string> Execute(CommandString cmd, Update update)
    {
        if (cmd.Parameters.Length < 1)
            return null;
        var msg = update.Message;

        if (msg.Chat.Type != ChatType.Private)
            return null;

        string name = cmd.Parameters[0];

        var user = await Db.GetUser(NamespacedId.Telegram(msg.From.Id));

        if (user == null) return null;

        var changed = await Db.DeleteTopic(name, user.UserId);
        if (changed > 0)
            return $"deleted {name} and all its subscriptions";
        else
            return $"invalid topic name";
    }
}

