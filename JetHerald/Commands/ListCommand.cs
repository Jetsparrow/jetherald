using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Args;

namespace JetHerald
{
    public class ListCommand : IChatCommand
    {
        Db db;

        public ListCommand(Db db)
        {
            this.db = db;
        }

        public string Execute(CommandString cmd, MessageEventArgs messageEventArgs)
        {
            var msg = messageEventArgs.Message;
            var chatid = msg.Chat.Id;
            var topics = db.GetTopicsForChat(chatid);

            return topics.Any()
                ? "Topics:\n" + string.Join("\n", topics.Select(GetTopicListing))
                : "No subscriptions active.";
        }

        static string GetTopicListing(Db.Topic t)
            => t.Name == t.Description ? t.Name : $"{t.Name}: {t.Description}";

    }
}