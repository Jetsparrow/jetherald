using Telegram.Bot.Args;

namespace JetHerald
{
    public class SubscribeCommand : IChatCommand
    {
        Db db;

        public SubscribeCommand(Db db)
        {
            this.db = db;
        }

        public string Execute(CommandString cmd, MessageEventArgs args)
        {
            if (cmd.Parameters.Length < 1)
                return null;

            var chatid = args.Message.Chat.Id;
            var token = cmd.Parameters[0];

            var topic = db.GetTopic(token, chatid);

            if (topic == null)
                return "topic not found";
            else if (topic.ChatId == chatid)
                return $"already subscribed to {topic.Name}";
            else if (topic.ReadToken != token)
                return "token mismatch";
            else
            {
                db.CreateSubscription(topic.TopicId, chatid);
                return $"subscribed to {topic.Name}";
            }
        }
    }
}