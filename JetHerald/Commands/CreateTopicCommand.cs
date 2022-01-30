using MySql.Data.MySqlClient;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using JetHerald.Services;

namespace JetHerald.Commands;
public class CreateTopicCommand : IChatCommand
{
    Db Db { get; }

    public CreateTopicCommand(Db db)
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
        string descr = name;
        if (cmd.Parameters.Length > 1)
            descr = string.Join(' ', cmd.Parameters.Skip(1));

        var user = await Db.GetUser(NamespacedId.Telegram(msg.From.Id));

        if (user == null) return null;

        try
        {
            var topic = await Db.CreateTopic(user.UserId, name, descr);

            if (topic == null)
            {
                return "you have reached the limit of topics";
            }
            else
            {
                return $"created {topic.Name}\n" +
                    $"readToken\n{topic.ReadToken}\n" +
                    $"writeToken\n{topic.WriteToken}\n";
            }
        }
        catch (MySqlException myDuplicate) when (myDuplicate.Number == 1062)
        {
            return $"topic {name} already exists";
        }
    }
}

