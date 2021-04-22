using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;

namespace JetHerald
{
    public partial class JetHeraldBot
    {
        DiscordClient DiscordBot { get; set; }

        async Task InitDiscord()
        {
            DiscordBot = new DiscordClient(new()
            {
                Token = DiscordConfig.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged,
                LoggerFactory = LoggerFactory
            });

            var commands = DiscordBot.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { "!" },
                Services = ServiceProvider
            });

            commands.RegisterCommands<DiscordCommands>();

            await DiscordBot.ConnectAsync();
        }


        async Task SendMessageToDiscordChannel(NamespacedId chat, string formatted)
        {
            var id = ulong.Parse(chat.Id);
            await DiscordBot.SendMessageAsync(await DiscordBot.GetChannelAsync(id), formatted);
        }
    }
}