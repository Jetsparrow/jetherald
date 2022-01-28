using MySql.Data.MySqlClient;
using Dapper;
using JetHerald.Options;
using JetHerald.Contracts;

namespace JetHerald.Services;
public class Db
{
    public async Task<int> DeleteTopic(string name, string adminToken)
    {
        using var c = GetConnection();
        return await c.ExecuteAsync(
            " DELETE" +
            " FROM topic" +
            " WHERE Name = @name AND AdminToken = @adminToken",
            new { name, adminToken });
    }

    public async Task<Topic> GetTopic(string name)
    {
        using var c = GetConnection();
        return await c.QuerySingleOrDefaultAsync<Topic>(
            " SELECT *" +
            " FROM topic" +
            " WHERE Name = @name",
            new { name });
    }

    public async Task<Topic> GetTopicForSub(string token, NamespacedId sub)
    {
        using var c = GetConnection();
        return await c.QuerySingleOrDefaultAsync<Topic>(
            " SELECT t.*, ts.Sub " +
            " FROM topic t " +
            " LEFT JOIN topic_sub ts ON t.TopicId = ts.TopicId AND ts.Sub = @sub " +
            " WHERE ReadToken = @token",
            new { token, sub });
    }

    public async Task<Topic> CreateTopic(NamespacedId user, string name, string descr)
    {
        using var c = GetConnection();
        return await c.QuerySingleOrDefaultAsync<Topic>(
            " INSERT INTO topic " +
            " ( Creator,  Name,  Description,  ReadToken,  WriteToken,  AdminToken) " +
            " VALUES " +
            " (@Creator, @Name, @Description, @ReadToken, @WriteToken, @AdminToken); " +
            " SELECT * FROM topic WHERE TopicId = LAST_INSERT_ID(); ",
            new Topic
            {
                Creator = user,
                Name = name,
                Description = descr,
                ReadToken = TokenHelper.GetToken(),
                WriteToken = TokenHelper.GetToken(),
                AdminToken = TokenHelper.GetToken()
            });
    }
    public async Task<IEnumerable<NamespacedId>> GetSubsForTopic(uint topicId)
    {
        using var c = GetConnection();
        return await c.QueryAsync<NamespacedId>(
            " SELECT Sub " +
            " FROM topic_sub " +
            " WHERE TopicId = @topicid",
            new { topicId });
    }

    public async Task<IEnumerable<Topic>> GetTopicsForSub(NamespacedId sub)
    {
        using var c = GetConnection();
        return await c.QueryAsync<Topic>(
            " SELECT t.*" +
            " FROM topic_sub ts" +
            " JOIN topic t USING (TopicId)" +
            " WHERE ts.Sub = @sub",
            new { sub });
    }

    public async Task CreateSubscription(uint topicId, NamespacedId sub)
    {
        using var c = GetConnection();
        await c.ExecuteAsync(
            " INSERT INTO topic_sub" +
            " (TopicId, Sub)" +
            " VALUES" +
            " (@topicId, @sub)",
            new { topicId, sub });
    }

    public async Task<int> RemoveSubscription(string topicName, NamespacedId sub)
    {
        using var c = GetConnection();
        return await c.ExecuteAsync(
            " DELETE ts " +
            " FROM topic_sub ts" +
            " JOIN topic t USING (TopicId) " +
            " WHERE t.Name = @topicName AND ts.Sub = @sub;",
            new { topicName, sub });
    }


    public async Task<int> ReportHeartbeat(uint topicId, string heart, int timeoutSeconds)
    {
        using var c = GetConnection();
        return await c.ExecuteAsync(
            @"CALL report_heartbeat(@topicId, @heart, @timeoutSeconds);",
            new { topicId, heart, timeoutSeconds });
    }

    public async Task<IEnumerable<HeartEvent>> ProcessHearts()
    {
        using var c = GetConnection();
        return await c.QueryAsync<HeartEvent>("CALL process_hearts();");
    }

    public async Task MarkHeartAttackReported(ulong id)
    {
        using var c = GetConnection();
        await c.ExecuteAsync("UPDATE heartevent SET Status = 'reported' WHERE HeartEventId = @id", new { id });
    }

    public Db(IOptionsMonitor<ConnectionStrings> cfg)
    {
        Config = cfg;
    }
    IOptionsMonitor<ConnectionStrings> Config { get; }
    MySqlConnection GetConnection() => new(Config.CurrentValue.DefaultConnection);
}

