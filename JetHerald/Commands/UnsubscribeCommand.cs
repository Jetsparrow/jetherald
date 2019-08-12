using System.Threading.Tasks;
using Telegram.Bot.Args;

namespace JetHerald
{
    public class UnsubscribeCommand : IChatCommand
    {
        Db db;

        public UnsubscribeCommand(Db db)
        {
            this.db = db;
        }

        public string Execute(CommandString cmd, MessageEventArgs messageEventArgs)
        {
            if (cmd.Parameters.Length < 1)
                return null;

            var msg = messageEventArgs.Message;
            var chatid = msg.Chat.Id;
            var topicName = cmd.Parameters[0];
            int affected = db.RemoveSubscription(topicName, chatid);
            if (affected >= 1)
                return $"unsubscribed from {topicName}";
            else
                return $"could not find subscription for {topicName}";
        }
    }
}