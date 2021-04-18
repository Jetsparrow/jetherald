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


        private async Task SendMessageToDiscordChannel(long chatId, string formatted)
        {
            await DiscordBot.SendMessageAsync(await DiscordBot.GetChannelAsync((ulong)chatId), formatted);
        }
    }
}