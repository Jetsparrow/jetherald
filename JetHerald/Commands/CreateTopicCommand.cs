using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace JetHerald.Commands
{
    public class CreateTopicCommand : IChatCommand
    {
        Db db;

        public CreateTopicCommand(Db db)
        {
            this.db = db;
        }

        public async Task<string> Execute(CommandString cmd, MessageEventArgs messageEventArgs)
        {
            if (cmd.Parameters.Length < 1)
                return null;
            var msg = messageEventArgs.Message;
            var chatid = msg.Chat.Id;

            if (msg.Chat.Type != ChatType.Private)
                return null;

            string name = cmd.Parameters[0];
            string descr = name;
            if (cmd.Parameters.Length > 1)
                descr = string.Join(' ', cmd.Parameters.Skip(1));

            try
            {
                var topic = await db.CreateTopic(msg.From.Id, name, descr);
                return $"created {topic.Name}\n" +
                    $"readToken\n{topic.ReadToken}\n" +
                    $"writeToken\n{topic.WriteToken}\n" +
                    $"adminToken\n{topic.AdminToken}\n";
            }
            catch (MySqlException myDuplicate) when (myDuplicate.Number == 1062)
            {
                return $"topic {name} already exists";
            }
        }
    }
}
