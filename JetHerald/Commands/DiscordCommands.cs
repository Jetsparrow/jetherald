using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MySql.Data.MySqlClient;

namespace JetHerald
{
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class DiscordCommands : BaseCommandModule
    {
        public Db db { private get; set; }

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

            await ctx.TriggerTypingAsync();

            try
            {
                var topic = await db.CreateTopic((long)ctx.User.Id, "Discord", name, description);
                await ctx.RespondAsync($"created {topic.Name}\n" +
                    $"readToken\n{topic.ReadToken}\n" +
                    $"writeToken\n{topic.WriteToken}\n" +
                    $"adminToken\n{topic.AdminToken}\n");
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
            string name,
            [Description("The admin token of the topic to be deleted.")]
            string adminToken)
        {
            await ctx.TriggerTypingAsync();
            var changed = await db.DeleteTopic(name, adminToken);
            if (changed > 0)
                await ctx.RespondAsync($"deleted {name} and all its subscriptions");
            else
                await ctx.RespondAsync($"invalid topic name or admin token");
        }

        [Command("list")]
        [Description("List all subscriptions in this channel.")]
        public async Task ListSubscriptions(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            var topics = await db.GetTopicsForChat((long)ctx.Channel.Id, "Discord");

            await ctx.RespondAsync(topics.Any()
                ? "Topics:\n" + string.Join("\n", topics)
                : "No subscriptions active.");
        }

        [Command("subscribe")]
        [Description("Subscribes to a topic.")]
        public async Task Subscribe(
            CommandContext ctx,
            [Description("The read token of the token to subscribe to.")]
            string token
        )
        {
            await ctx.TriggerTypingAsync();

            var topic = await db.GetTopic(token, (long)ctx.Channel.Id, "Discord");

            if (topic == null)
                await ctx.RespondAsync("topic not found");
            else if (topic.ChatId == (long)ctx.Channel.Id)
                await ctx.RespondAsync($"already subscribed to {topic.Name}");
            else if (topic.ReadToken != token)
                await ctx.RespondAsync("token mismatch");
            else
            {
                await db.CreateSubscription(topic.TopicId, (long)ctx.Channel.Id, "Discord");
                await ctx.RespondAsync($"subscribed to {topic.Name}");
            }
        }

        [Command("unsubscribe")]
        [Description("Unsubscribes from a topic.")]
        public async Task Unsubscribe(
            CommandContext ctx,
            [Description("The name of the topic to unsubscribe from.")]
            string name
        )
        {
            await ctx.TriggerTypingAsync();

            int affected = await db.RemoveSubscription(name, (long)ctx.Channel.Id, "Discord");
            if (affected >= 1)
                await ctx.RespondAsync($"unsubscribed from {name}");
            else
                await ctx.RespondAsync($"could not find subscription for {name}");
        }
    }
}