using MySql.Data.MySqlClient;
using Dapper;
using JetHerald.Options;
using JetHerald.Contracts;

namespace JetHerald.Services;
public class Db
{
    public async Task<int> DeleteTopic(string name, uint userId)
    {
        using var c = GetConnection();
        return await c.ExecuteAsync(
            " DELETE t" +
            " FROM topic t" +
            " LEFT JOIN user u ON t.CreatorId = u.UserId" +
            " WHERE t.Name = @name AND u.UserId = @userId",
            new { name, userId });
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

    public async Task<User> GetUser(NamespacedId foreignId)
    {
        using var c = GetConnection();
        return await c.QuerySingleOrDefaultAsync<User>(
            " SELECT u.*, p.* " +
            " FROM user u " +
            " LEFT JOIN plan p ON p.PlanId = u.PlanId " +
            " WHERE u.ForeignId = @foreignId",
            new { foreignId });
    }

    public async Task<Topic> CreateTopic(uint user, string name, string descr)
    {
        using var c = GetConnection();

        await c.OpenAsync();

        await using var tx = await c.BeginTransactionAsync();

        var topicsCount = await c.QuerySingleAsync<int>(
            " SELECT COUNT(t.TopicId) " +
            " FROM user u " +
            " LEFT JOIN topic t ON t.CreatorId = u.UserId ",
            transaction: tx
        );

        var planTopicsCount = await c.QuerySingleAsync<int>(
            " SELECT p.MaxTopics " +
            " FROM user u " +
            " LEFT JOIN plan p ON p.PlanId = u.PlanId ",
            transaction: tx
        );

        if (topicsCount >= planTopicsCount) return null;

        var topic = await c.QuerySingleOrDefaultAsync<Topic>(
            " INSERT INTO topic " +
            " ( CreatorId,  Name,  Description,  ReadToken,  WriteToken) " +
            " VALUES " +
            " (@CreatorId, @Name, @Description, @ReadToken, @WriteToken); " +
            " SELECT * FROM topic WHERE TopicId = LAST_INSERT_ID(); ",
            new Topic
            {
                CreatorId = user,
                Name = name,
                Description = descr,
                ReadToken = TokenHelper.GetToken(),
                WriteToken = TokenHelper.GetToken()
            }, transaction: tx);

        await tx.CommitAsync();

        return topic;
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
        return await c.QueryFirstAsync<int>(
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

