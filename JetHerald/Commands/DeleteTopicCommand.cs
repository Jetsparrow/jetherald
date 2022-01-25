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
        if (cmd.Parameters.Length < 2)
            return null;
        var msg = update.Message;

        if (msg.Chat.Type != ChatType.Private)
            return null;

        string name = cmd.Parameters[0];
        string adminToken = cmd.Parameters[1];

        var changed = await Db.DeleteTopic(name, adminToken);
        if (changed > 0)
            return ($"deleted {name} and all its subscriptions");
        else
            return ($"invalid topic name or admin token");
    }
}

