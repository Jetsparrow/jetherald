using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace JetHerald
{
    public class CommandString
    {
        public CommandString(string command, string username, params string[] parameters)
        {
            Command = command;
            Username = username;
            Parameters = parameters;
        }

        public string Command { get; }
        public string Username { get; }
        public string[] Parameters { get; }

        static readonly char[] WS_CHARS = new[] { ' ', '\r', '\n', '\n' };

        public static bool TryParse(string s, out CommandString result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(s) || s[0] != '/')
                return false;

            string[] words = s.Split(WS_CHARS, StringSplitOptions.RemoveEmptyEntries);

            var cmdRegex = new Regex(@"/(?<cmd>\w+)(@(?<name>\w+))?");
            var match = cmdRegex.Match(words.First());
            if (!match.Success)
                return false;

            string cmd = match.Groups["cmd"].Captures[0].Value;
            string username = match.Groups["name"].Captures.Count > 0 ? match.Groups["name"].Captures[0].Value : null;
            string[] parameters = words.Skip(1).ToArray();

            result = new CommandString(cmd, username, parameters);
            return true;
        }
    }
}
