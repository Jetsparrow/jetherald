using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Args;

namespace JetHerald
{
    public interface IChatCommand
    {
        Task<string> Execute(CommandString cmd, MessageEventArgs messageEventArgs);
    }

    public class ChatCommandRouter
    {
        string Username { get; }
        ILogger Log { get; }

        public ChatCommandRouter(string username, ILogger log)
        {
            Log = log;
            Username = username;
        }

        public async Task<string> Execute(object sender, MessageEventArgs args)
        {
            var text = args.Message.Text;
            if (CommandString.TryParse(text, out var cmd))
            {
                if (cmd.UserName != null && cmd.UserName != Username)
                {
                    Log.LogDebug("Message not directed at us");
                    return null;
                }
                if (commands.ContainsKey(cmd.Command))
                {
                    try
                    {
                        Log.LogDebug($"Handling message via {commands[cmd.Command].GetType().Name}");
                        return await commands[cmd.Command].Execute(cmd, args);
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
                if (commands.ContainsKey(cmd))
                    throw new ArgumentException($"collision for {cmd}, commands {commands[cmd].GetType()} and {c.GetType()}");
                commands[cmd] = c;
            }
        }

        Dictionary<string, IChatCommand> commands = new Dictionary<string, IChatCommand>();
    }
}
