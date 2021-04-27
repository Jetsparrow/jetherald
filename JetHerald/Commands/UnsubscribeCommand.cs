using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace JetHerald.Commands
{
    public class UnsubscribeCommand : IChatCommand
    {
        readonly Db db;
        readonly TelegramBotClient bot;

        public UnsubscribeCommand(Db db, TelegramBotClient bot)
        {
            this.db = db;
            this.bot = bot;
        }

        public async Task<string> Execute(CommandString cmd, MessageEventArgs messageEventArgs)
        {
            if (cmd.Parameters.Length < 1)
                return null;

            if (!await CommandHelper.CheckAdministrator(bot, messageEventArgs.Message))
                return null;

            var msg = messageEventArgs.Message;
            var chat = NamespacedId.Telegram(msg.Chat.Id);

            var topicName = cmd.Parameters[0];
            int affected = await db.RemoveSubscription(topicName, chat);
            if (affected >= 1)
                return $"unsubscribed from {topicName}";
            else
                return $"could not find subscription for {topicName}";
        }
    }
}