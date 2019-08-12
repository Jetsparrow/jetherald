using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Dapper;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;

namespace JetHerald
{
    public static class TokenHelper
    {
        static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        static byte[] buf = new byte[24];

        public static string GetToken()
        {
            rng.GetBytes(buf);
            return Convert.ToBase64String(buf).Replace('+', '_').Replace('/','_');
        }
    }

    public class Db
    {
        public class Topic
        {
            public uint TopicId { get; set; }
            public long CreatorId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string ReadToken { get; set; }
            public string WriteToken { get; set; }
            public string AdminToken { get; set; }

            public long? ChatId { get; set; }
        }

        public int DeleteTopic(string name, string adminToken)
        {
            using (var c = GetConnection())
            {
                return c.Execute("DELETE FROM topic WHERE Name = @name AND AdminToken = @adminToken", new { name, adminToken });
            }
        }

        public Topic GetTopic(string name)
        {
            using (var c = GetConnection())
                return  c.QuerySingleOrDefault<Topic>("SELECT * FROM topic WHERE Name = @name", new { name });
        }

        public Topic GetTopic(string token, long chatId)
        {
            using (var c = GetConnection())
                return  c.QuerySingleOrDefault<Topic>(
                    "SELECT t.*, tc.ChatId " +
                    "FROM topic t LEFT JOIN topic_chat tc ON t.TopicId = tc.TopicId AND tc.ChatId = @chatId " +
                    "WHERE ReadToken = @token", new { token, chatId});
        }

        public Topic CreateTopic(long userId, string name, string descr)
        {
            var t = new Topic
            {
                CreatorId = userId,
                Name = name,
                Description = descr,
                ReadToken = TokenHelper.GetToken(),
                WriteToken = TokenHelper.GetToken(),
                AdminToken = TokenHelper.GetToken()
            };
            using (var c = GetConnection())
            {
                return c.QuerySingleOrDefault<Topic>(
                " INSERT INTO herald.topic " +
                " (TopicId,  CreatorId,  Name,  Description,  ReadToken,  WriteToken,  AdminToken) " +
                " VALUES " +
                " (NULL,    @CreatorId, @Name, @Description, @ReadToken, @WriteToken, @AdminToken); " +
                " SELECT * FROM topic WHERE TopicId = LAST_INSERT_ID(); ",
                    t);
            }
        }
        public IEnumerable<long> GetChatIdsForTopic(uint topicid)
        {
            using (var c = GetConnection())
                return c.Query<long>("SELECT ChatId FROM topic_chat WHERE TopicId = @topicid", new { topicid });
        }

        public IEnumerable<Topic> GetTopicsForChat(long chatid)
        {
            using (var c = GetConnection())
                return c.Query<Topic>("SELECT t.* FROM topic_chat ct JOIN topic t on t.TopicId = ct.TopicId WHERE ct.ChatId = @chatid", new { chatid });
        }

        public void CreateSubscription(uint topicId, long chatId)
        {
            using (var c = GetConnection())
                c.Execute("INSERT INTO topic_chat (ChatId, TopicId ) VALUES (@chatId, @topicId)", new { topicId, chatId });
        }

        public int RemoveSubscription(string topicName, long chatId)
        {
            using (var c = GetConnection())
                return c.Execute(
                    "DELETE tc " +
                    "FROM topic_chat tc JOIN topic t ON tc.TopicId = t.TopicId " +
                    "WHERE t.Name = @topicName AND tc.ChatId = @chatId;",
                    new { topicName, chatId });
        }

        public Db(IOptions<Options.ConnectionStrings> cfg)
        {
            Config = cfg;
        }

        IOptions<Options.ConnectionStrings> Config { get; }
        MySqlConnection GetConnection() => new MySqlConnection(Config.Value.DefaultConnection);
    }
}
