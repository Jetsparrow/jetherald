using Telegram.Bot.Types;

namespace JetHerald;
public interface IChatCommand
{
    Task<string> Execute(CommandString cmd, Update update);
}

public class ChatCommandRouter
{
    string Username { get; }
    ILogger Log { get; }

    Dictionary<string, IChatCommand> Commands { get; }

    public ChatCommandRouter(string username, ILogger log)
    {
        Log = log;
        Username = username;
        Commands = new Dictionary<string, IChatCommand>();
    }

    public async Task<string> Execute(object sender, Update update)
    {
        var text = update.Message.Text;
        if (CommandString.TryParse(text, out var cmd))
        {
            if (cmd.Username != null && cmd.Username != Username)
            {
                Log.LogDebug("Message not directed at us");
                return null;
            }
            if (Commands.ContainsKey(cmd.Command))
            {
                try
                {
                    Log.LogDebug($"Handling message via {Commands[cmd.Command].GetType().Name}");
                    return await Commands[cmd.Command].Execute(cmd, update);
                }
                catch (Exception e)
                {
                    Log.LogError(e, $"Error while executing command {cmd.Command}!");
                }
            }
            else
                Log.LogDebug($"Command {cmd.Command} not found");
        }
        return null;
    }

    public void Add(IChatCommand c, params string[] cmds)
    {
        foreach (var cmd in cmds)
        {
            if (Commands.ContainsKey(cmd))
                throw new ArgumentException($"collision for {cmd}, commands {Commands[cmd].GetType()} and {c.GetType()}");
            Commands[cmd] = c;
        }
    }
}
