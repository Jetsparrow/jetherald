using MySql.Data.MySqlClient;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using JetHerald.Services;

namespace JetHerald.Commands;
[ModuleLifespan(ModuleLifespan.Transient)]
public class DiscordCommands : BaseCommandModule
{
    public Db Db { get; set; }

    [Command("createtopic")]
    [Description("Creates a topic.")]
    [RequireDirectMessage]
    public async Task CreateTopic(
        CommandContext ctx,
        [Description("The unique name of the new topic.")]
        string name,
        [RemainingText, Description("The name displayed in service messages. Defaults to `name`")]
        string description = null)
    {
        if (description == null)
            description = name;

        _ = ctx.TriggerTypingAsync();

        var user = await Db.GetUser(NamespacedId.Discord(ctx.User.Id));

        if (user == null) return;

        try
        {
            var topic = await Db.CreateTopic(user.UserId, name, description);

            if (topic == null)
            {
                await ctx.RespondAsync("you have reached the limit of topics");
            }
            else
            {
                await ctx.RespondAsync($"created {topic.Name}\n" +
                    $"readToken\n{topic.ReadToken}\n" +
                    $"writeToken\n{topic.WriteToken}\n");
            }
        }
        catch (MySqlException myDuplicate) when (myDuplicate.Number == 1062)
        {
            await ctx.RespondAsync($"topic {name} already exists");
        }
    }

    [Command("deletetopic")]
    [Description("Deletes a topic.")]
    [RequireDirectMessage]
    public async Task DeleteTopic(
        CommandContext ctx,
        [Description("The name of the topic to be deleted.")]
        string name)
    {
        _ = ctx.TriggerTypingAsync();

        var user = await Db.GetUser(NamespacedId.Discord(ctx.User.Id));

        if (user == null) return;

        var changed = await Db.DeleteTopic(name, user.UserId);
        if (changed > 0)
            await ctx.RespondAsync($"deleted {name} and all its subscriptions");
        else
            await ctx.RespondAsync($"invalid topic name");
    }

    [Command("list")]
    [Description("List all subscriptions in this channel.")]
    [RequireUserPermissions(DSharpPlus.Permissions.ManageGuild)]
    public async Task ListSubscriptions(CommandContext ctx)
    {
        _ = ctx.TriggerTypingAsync();

        var topics = await Db.GetTopicsForSub(NamespacedId.Discord(ctx.Channel.Id));

        await ctx.RespondAsync(topics.Any()
            ? "Topics:\n" + string.Join("\n", topics)
            : "No subscriptions active.");
    }

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
