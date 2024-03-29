﻿using System.Threading;
using JetHerald.Contracts;
using JetHerald.Options;
using Microsoft.Extensions.Hosting;

namespace JetHerald.Services;
public partial class JetHeraldBot : IHostedService
{
    Db Db { get; set; }
    TelegramConfig TelegramConfig { get; }
    DiscordConfig DiscordConfig { get; }
    ILogger<JetHeraldBot> Log { get; }
    ILoggerFactory LoggerFactory { get; }
    IServiceProvider ServiceProvider { get; }

    public JetHeraldBot(
        Db db,
        IOptions<TelegramConfig> telegramCfg,
        IOptions<DiscordConfig> discordCfg,
        ILogger<JetHeraldBot> log,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider)
    {
        Db = db;
        TelegramConfig = telegramCfg.Value;
        DiscordConfig = discordCfg.Value;

        Log = log;
        LoggerFactory = loggerFactory;
        ServiceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken token)
    {
        await StartTelegram();
        await StartDiscord();
    }

    public async Task StopAsync(CancellationToken token)
    {
        await StopDiscord();
        await StopTelegram();
    }

    public async Task BroadcastMessageRaw(uint topicId, string formatted)
    {
        IEnumerable<NamespacedId> chatIds;
        using (var ctx = await Db.GetContext())
            chatIds = await ctx.GetSubsForTopic(topicId);
        foreach (var c in chatIds)
            await SendMessageRaw(c, formatted);
    }

    async Task SendMessageRaw(NamespacedId chat, string formatted)
    {
        try
        {
            if (chat.Namespace == "telegram")
            {
                await SendMessageToTelegramChannel(chat, formatted);
            }
            else if (chat.Namespace == "discord")
            {
                await SendMessageToDiscordChannel(chat, formatted);
            }
        }
        catch (Exception e) { Log.LogError(e, $"Error while sending message \"{formatted}\" to {chat}"); }
    }

}

