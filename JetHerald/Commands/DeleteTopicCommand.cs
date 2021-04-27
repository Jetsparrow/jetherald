using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace JetHerald.Commands
{
    public class DeleteTopicCommand : IChatCommand
    {
        readonly Db db;

        public DeleteTopicCommand(Db db)
        {
            this.db = db;
        }

        public async Task<string> Execute(CommandString cmd, MessageEventArgs messageEventArgs)
        {
            if (cmd.Parameters.Length < 2)
                return null;
            var msg = messageEventArgs.Message;

            if (msg.Chat.Type != ChatType.Private)
                return null;

            string name = cmd.Parameters[0];
            string adminToken = cmd.Parameters[1];

            var changed = await db.DeleteTopic(name, adminToken);
            if (changed > 0)
                return ($"deleted {name} and all its subscriptions");
            else
                return ($"invalid topic name or admin token");
        }
    }
}
