using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace JetHerald.Commands;
public static class CommandHelper
{
    public static async Task<bool> CheckAdministrator(TelegramBotClient bot, Message msg)
    {
        if (msg.Chat.Type != ChatType.Private)
        {
            var chatMember = await bot.GetChatMemberAsync(msg.Chat.Id, msg.From.Id);
            return chatMember.Status is ChatMemberStatus.Administrator or ChatMemberStatus.Creator;
        }
        return true;
    }
}
