using DSharpPlus;
using DSharpPlus.CommandsNext;
using JetHerald.Commands;
using System.Net;
using System.Net.Http;
namespace JetHerald.Services;
public partial class JetHeraldBot
{
    DiscordClient DiscordBot { get; set; }

    async Task StartDiscord()
    {
        if (string.IsNullOrWhiteSpace(DiscordConfig.Token))
        {
            Log.LogInformation("No Discord token, ignoring.");
            return;
        }

        Log.LogInformation("Starting Discord client.");

        DiscordConfiguration cfg = new()
        {
            Token = DiscordConfig.Token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged,
            LoggerFactory = LoggerFactory
        };

        if (!string.IsNullOrWhiteSpace(DiscordConfig.ProxyUrl))
        {
            cfg.Proxy = new WebProxy(DiscordConfig.ProxyUrl);
        }

        DiscordBot = new DiscordClient(cfg);

        var commands = DiscordBot.UseCommandsNext(new CommandsNextConfiguration()
        {
            StringPrefixes = new[] { "!" },
            Services = ServiceProvider
        });

        commands.RegisterCommands<DiscordCommands>();

        await DiscordBot.ConnectAsync();
    }
    Task StopDiscord() => DiscordBot.DisconnectAsync();

    async Task SendMessageToDiscordChannel(NamespacedId chat, string formatted)
    {
        var id = ulong.Parse(chat.Id);
        await DiscordBot.SendMessageAsync(await DiscordBot.GetChannelAsync(id), formatted);
    }
}
