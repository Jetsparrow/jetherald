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

        public async Task<string> Execute(CommandString cmd, MessageEventArgs messageEventArgs)
        {
            var msg = messageEventArgs.Message;
            var chatid = msg.Chat.Id;
            var topics = await db.GetTopicsForChat(chatid, "Telegram");

            return topics.Any()
                ? "Topics:\n" + string.Join("\n", topics.Select(GetTopicListing))
                : "No subscriptions active.";
        }

        internal static string GetTopicListing(Db.Topic t)
            => t.Name == t.Description ? t.Name : $"{t.Name}: {t.Description}";

    }
}