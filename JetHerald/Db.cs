using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Dapper;
using System.Threading.Tasks;
using System.Threading;

namespace JetHerald
{
    public class Db
    {
        public class Topic
        {
            public uint TopicId { get; set; }
            public NamespacedId Creator { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string ReadToken { get; set; }
            public string WriteToken { get; set; }
            public string AdminToken { get; set; }
            public DateTime? ExpiryTime { get; set; }
            public bool ExpiryMessageSent { get; set; }

            public NamespacedId? Chat { get; set; }

            public override string ToString()
                => Name == Description ? Name : $"{Name}: {Description}";
        }

        public class ExpiredTopicChat
        {
            public NamespacedId Chat;
            public string Description;
            public DateTime ExpiryTime { get; set; }
        }

        public async Task<int> DeleteTopic(string name, string adminToken)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(
                    " DELETE" +
                    " FROM topic" +
                    " WHERE Name = @name AND AdminToken = @adminToken",
                    new { name, adminToken });
            }
        }

        public async Task<Topic> GetTopic(string name)
        {
            using (var c = GetConnection())
                return await c.QuerySingleOrDefaultAsync<Topic>(
                    "SELECT *" +
                    " FROM topic" +
                    " WHERE Name = @name",
                    new { name });
        }

        public async Task<Topic> GetTopicForSub(string token, NamespacedId chat)
        {
            using var c = GetConnection();
            return await c.QuerySingleOrDefaultAsync<Topic>(
                " SELECT t.*, tc.Chat " +
                " FROM topic t " +
                " LEFT JOIN topic_chat tc ON t.TopicId = tc.TopicId AND tc.Chat = @chat " +
                " WHERE ReadToken = @token",
                new { token, chat });
        }

        public async Task<Topic> CreateTopic(NamespacedId user, string name, string descr)
        {
            var t = new Topic
            {
                Creator = user,
                Name = name,
                Description = descr,
                ReadToken = TokenHelper.GetToken(),
                WriteToken = TokenHelper.GetToken(),
                AdminToken = TokenHelper.GetToken()
            };
            using var c = GetConnection();
            return await c.QuerySingleOrDefaultAsync<Topic>(
                " INSERT INTO herald.topic " +
                " ( Creator,  Name,  Description,  ReadToken,  WriteToken,  AdminToken) " +
                " VALUES " +
                " (@Creator, @Name, @Description, @ReadToken, @WriteToken, @AdminToken); " +
                " SELECT * FROM topic WHERE TopicId = LAST_INSERT_ID(); ",
                t);
        }
        public async Task<IEnumerable<NamespacedId>> GetChatsForTopic(uint topicid)
        {
            using var c = GetConnection();
            return await c.QueryAsync<NamespacedId>(
                " SELECT Chat " +
                " FROM topic_chat " +
                " WHERE TopicId = @topicid",
                new { topicid });
        }

        public async Task<IEnumerable<Topic>> GetTopicsForChat(NamespacedId chat)
        {
            using var c = GetConnection();
            return await c.QueryAsync<Topic>(
                " SELECT t.*" +
                " FROM topic_chat ct" +
                " JOIN topic t on t.TopicId = ct.TopicId" +
                " WHERE ct.Chat = @chat",
                new { chat });
        }

        public async Task CreateSubscription(uint topicId, NamespacedId chat)
        {
            using var c = GetConnection();
            await c.ExecuteAsync(
                " INSERT INTO topic_chat" +
                " (Chat, TopicId)" +
                " VALUES" +
                " (@chat, @topicId)",
                new { topicId, chat });
        }

        public async Task<int> RemoveSubscription(string topicName, NamespacedId chat)
        {
            using var c = GetConnection();
            return await c.ExecuteAsync(
                " DELETE tc " +
                " FROM topic_chat tc" +
                " JOIN topic t ON tc.TopicId = t.TopicId " +
                " WHERE t.Name = @topicName AND tc.Chat = @chat;",
                new { topicName, chat });
        }

        public Task AddExpiry(string topicName, int addedTime)
        {
            using var c = GetConnection();
            return c.ExecuteAsync(
                " UPDATE topic" +
                " SET ExpiryTime = CURRENT_TIMESTAMP() + INTERVAL @addedTime SECOND," +
                " ExpiryMessageSent = 0" +
                " WHERE Name = @topicName",
                new { topicName, addedTime });
        }

        public Task<IEnumerable<ExpiredTopicChat>> GetExpiredTopics(CancellationToken token = default)
        {
            using var c = GetConnection();
            return c.QueryAsync<ExpiredTopicChat>(
                " SELECT tc.Chat, t.Description, t.ExpiryTime" +
                " FROM topic_chat tc" +
                " INNER JOIN topic t ON t.TopicId = tc.TopicId" +
                " WHERE t.ExpiryTime < CURRENT_TIMESTAMP() AND NOT t.ExpiryMessageSent",
                token);
        }

        public Task MarkExpiredTopics(CancellationToken token = default)
        {
            using var c = GetConnection();
            return c.ExecuteAsync(
                " UPDATE topic t" +
                " SET t.ExpiryMessageSent = 1" +
                " WHERE t.ExpiryTime < CURRENT_TIMESTAMP()",
                token);
        }

        public Db(IOptions<Options.ConnectionStrings> cfg)
        {
            Config = cfg.Value;
        }

        Options.ConnectionStrings Config { get; }
        MySqlConnection GetConnection() => new MySqlConnection(Config.DefaultConnection);
    }
}
