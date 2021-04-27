using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace JetHerald.Commands
{
    public class ListCommand : IChatCommand
    {
        readonly Db db;
        readonly TelegramBotClient bot;

        public ListCommand(Db db, TelegramBotClient bot)
        {
            this.db = db;
            this.bot = bot;
        }

        public async Task<string> Execute(CommandString cmd, MessageEventArgs messageEventArgs)
        {
            if (!await CommandHelper.CheckAdministrator(bot, messageEventArgs.Message))
                return null;

            var msg = messageEventArgs.Message;
            var chatid = msg.Chat.Id;
            var topics = await db.GetTopicsForChat(NamespacedId.Telegram(chatid));

            return topics.Any()
                ? "Topics:\n" + string.Join("\n", topics)
                : "No subscriptions active.";
        }
    }
}