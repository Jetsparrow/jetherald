using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace JetHerald.Commands
{
    public class DeleteTopicCommand : IChatCommand
    {
        Db db;

        public DeleteTopicCommand(Db db)
        {
            this.db = db;
        }

        public string Execute(CommandString cmd, MessageEventArgs messageEventArgs)
        {
            if (cmd.Parameters.Length < 2)
                return null;
            var msg = messageEventArgs.Message;
            var chatid = msg.Chat.Id;

            if (msg.Chat.Type != ChatType.Private)
                return null;

            string name = cmd.Parameters[0];
            string adminToken = cmd.Parameters[1];

            var topic = db.DeleteTopic(name, adminToken);
            return $"deleted {name} and all its subscriptions";
        }
    }
}
