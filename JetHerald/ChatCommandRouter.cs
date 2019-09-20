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

        Dictionary<string, IChatCommand> Commands { get; }

        public ChatCommandRouter(string username, ILogger log)
        {
            Log = log;
            Username = username;
            Commands = new Dictionary<string, IChatCommand>();
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
                if (Commands.ContainsKey(cmd.Command))
                {
                    try
                    {
                        Log.LogDebug($"Handling message via {Commands[cmd.Command].GetType().Name}");
                        return await Commands[cmd.Command].Execute(cmd, args);
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
}
