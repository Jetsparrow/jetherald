using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace JetHerald.Commands
{
    public class SubscribeCommand : IChatCommand
    {
        readonly Db db;
        readonly TelegramBotClient bot;

        public SubscribeCommand(Db db, TelegramBotClient bot)
        {
            this.db = db;
            this.bot = bot;
        }

        public async Task<string> Execute(CommandString cmd, MessageEventArgs args)
        {
            if (cmd.Parameters.Length < 1)
                return null;

            if (!await CommandHelper.CheckAdministrator(bot, args.Message))
                return null;

            var chat = NamespacedId.Telegram(args.Message.Chat.Id);
            var token = cmd.Parameters[0];

            var topic = await db.GetTopicForSub(token, chat);

            if (topic == null)
                return "topic not found";
            else if (topic.Chat == chat)
                return $"already subscribed to {topic.Name}";
            else if (topic.ReadToken != token)
                return "token mismatch";
            else
            {
                await db.CreateSubscription(topic.TopicId, chat);
                return $"subscribed to {topic.Name}";
            }
        }
    }
}