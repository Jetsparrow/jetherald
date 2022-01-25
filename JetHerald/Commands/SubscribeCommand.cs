using Telegram.Bot;
using Telegram.Bot.Types;
using JetHerald.Services;

namespace JetHerald.Commands;
public class SubscribeCommand : IChatCommand
{
    Db Db { get; }
    TelegramBotClient Bot { get; }

    public SubscribeCommand(Db db, TelegramBotClient bot)
    {
        Db = db;
        Bot = bot;
    }

    public async Task<string> Execute(CommandString cmd, Update args)
    {
        if (cmd.Parameters.Length < 1)
            return null;

        if (!await CommandHelper.CheckAdministrator(Bot, args.Message))
            return null;

        var chat = NamespacedId.Telegram(args.Message.Chat.Id);
        var token = cmd.Parameters[0];

        var topic = await Db.GetTopicForSub(token, chat);

        if (topic == null)
            return "topic not found";
        else if (topic.Sub == chat)
            return $"already subscribed to {topic.Name}";
        else if (topic.ReadToken != token)
            return "token mismatch";
        else
        {
            await Db.CreateSubscription(topic.TopicId, chat);
            return $"subscribed to {topic.Name}";
        }
    }
}
