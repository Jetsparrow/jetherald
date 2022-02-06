using JetHerald.Services;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace JetHerald.Commands;
[ModuleLifespan(ModuleLifespan.Transient)]
public class DiscordCommands : BaseCommandModule
{
    public Db Db { get; set; }

    [Command("subscribe")]
    [Description("Subscribes to a topic.")]
    [RequireUserPermissions(DSharpPlus.Permissions.ManageGuild)]
    public async Task Subscribe(
        CommandContext ctx,
        [Description("The read token of the token to subscribe to.")]
        string token
    )
    {
        _ = ctx.TriggerTypingAsync();

        var chat = NamespacedId.Discord(ctx.Channel.Id);
        var topic = await Db.GetTopicForSub(token, chat);

        if (topic == null)
            await ctx.RespondAsync("topic not found");
        else if (topic.Sub.HasValue && topic.Sub.Value == chat)
            await ctx.RespondAsync($"already subscribed to {topic.Name}");
        else if (topic.ReadToken != token)
            await ctx.RespondAsync("token mismatch");
        else
        {
            await Db.CreateSubscription(topic.TopicId, chat);
            await ctx.RespondAsync($"subscribed to {topic.Name}");
        }
    }

    [Command("unsubscribe")]
    [Description("Unsubscribes from a topic.")]
    [RequireUserPermissions(DSharpPlus.Permissions.ManageGuild)]
    public async Task Unsubscribe(
        CommandContext ctx,
        [Description("The name of the topic to unsubscribe from.")]
        string name
    )
    {
        _ = ctx.TriggerTypingAsync();

        int affected = await Db.RemoveSubscription(name, NamespacedId.Discord(ctx.Channel.Id));
        if (affected >= 1)
            await ctx.RespondAsync($"unsubscribed from {name}");
        else
            await ctx.RespondAsync($"could not find subscription for {name}");
    }
}
