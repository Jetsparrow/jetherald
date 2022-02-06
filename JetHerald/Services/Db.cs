using MySql.Data.MySqlClient;
using Dapper;
using JetHerald.Options;
using JetHerald.Contracts;

namespace JetHerald.Services;
public class Db
{
    public async Task<IEnumerable<Topic>> GetTopicsForUser(uint userId)
    {
        using var c = GetConnection();
        return await c.QueryAsync<Topic>(
            " SELECT * FROM topic WHERE CreatorId = @userId",
            new { userId });
    }
    public async Task<IEnumerable<Plan>> GetPlans()
    {
        using var c = GetConnection();
        return await c.QueryAsync<Plan>("SELECT * FROM plan");
    }

    public async Task<IEnumerable<UserInvite>> GetInvites()
    {
        using var c = GetConnection();
        return await c.QueryAsync<UserInvite>("SELECT * FROM userinvite");
    }

    public async Task<IEnumerable<Heart>> GetHeartsForUser(uint userId)
    {
        using var c = GetConnection();
        return await c.QueryAsync<Heart>(
            " SELECT h.* FROM heart h JOIN topic USING (TopicId) WHERE CreatorId = @userId",
            new { userId });
    }

    public async Task CreateUserInvite(uint planId, string inviteCode)
    {
        using var c = GetConnection();
        await c.ExecuteAsync(
            "INSERT INTO userinvite (PlanId, InviteCode) VALUES (@planId, @inviteCode)",
            new { planId, inviteCode });
    }

    public async Task<Topic> GetTopic(string name)
    {
        using var c = GetConnection();
        return await c.QuerySingleOrDefaultAsync<Topic>(
            "SELECT * FROM topic WHERE Name = @name",
            new { name });
    }

    public async Task<int> DeleteTopic(string name, uint userId)
    {
        using var c = GetConnection();
        return await c.ExecuteAsync(
            " DELETE FROM topic WHERE Name = @name AND CreatorId = @userId",
            new { name, userId });
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

    public async Task<IEnumerable<Heart>> GetHeartsForTopic(uint topicId)
    {
        using var c = GetConnection();
        return await c.QueryAsync<Heart>(
            " SELECT * FROM heart WHERE TopicId = @topicId",
            new { topicId });
    }
    public async Task<User> GetUser(string login)
    {
        using var c = GetConnection();
        return await c.QuerySingleOrDefaultAsync<User>(
            " SELECT u.*, p.* " +
            " FROM user u " +
            " LEFT JOIN plan p ON p.PlanId = u.PlanId " +
            " WHERE u.Login = @login",
            new { login });
    }

    public async Task<Topic> CreateTopic(uint user, string name, string descr)
    {
        using var c = GetConnection();

        await c.OpenAsync();

        await using var tx = await c.BeginTransactionAsync();

        var topicsCount = await c.QuerySingleAsync<int>(
            " SELECT COUNT(*) " +
            " FROM user u " +
            " LEFT JOIN topic t ON t.CreatorId = u.UserId " +
            " WHERE u.UserId = @user",
            new { user },
            transaction: tx
        );

        var planTopicsCount = await c.QuerySingleAsync<int>(
            " SELECT p.MaxTopics " +
            " FROM user u " +
            " LEFT JOIN plan p ON p.PlanId = u.PlanId " +
            " WHERE u.UserId = @user",
            new { user },
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

    public async Task<User> RegisterUserFromInvite(User user, uint inviteId)
    {
        using var c = GetConnection();
        uint userId = await c.QuerySingleOrDefaultAsync<uint>(
            "CALL register_user_from_invite(@inviteId, @Login, @Name, @PasswordHash, @PasswordSalt, @HashType);", 
            new { inviteId, user.Login, user.Name, user.PasswordHash, user.PasswordSalt, user.HashType });

        return await GetUser(user.Login);
    }

    public async Task<User> RegisterUser(User user, string plan)
    {
        using var c = GetConnection();
        uint userId = await c.QuerySingleOrDefaultAsync<uint>(
            "CALL register_user(@plan, @Login, @Name, @PasswordHash, @PasswordSalt, @HashType);",
            new { plan, user.Login, user.Name, user.PasswordHash, user.PasswordSalt, user.HashType });

        return await GetUser(user.Login);
    }

    public async Task<UserInvite> GetInviteByCode(string inviteCode)
    {
        using var c = GetConnection();
        return await c.QuerySingleOrDefaultAsync<UserInvite>(
            " SELECT * FROM userinvite " +
            " WHERE InviteCode = @inviteCode " + 
            " AND RedeemedBy IS NULL ",
            new { inviteCode });
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

    #region authorization

    public async Task RemoveSession(string sessionId)
    { 
        using var c = GetConnection();
        await c.ExecuteAsync("DELETE FROM usersession WHERE SessionId = @sessionId", new {sessionId});
    }
    public async Task<UserSession> GetSession(string sessionId)
    {
        using var c = GetConnection();
        return await c.QuerySingleOrDefaultAsync<UserSession>(
            "SELECT * FROM usersession WHERE SessionId = @sessionId",
            new { sessionId });
    }

    public async Task UpdateSession(string sessionId, byte[] data, DateTime expiryTs)
    {
        using var c = GetConnection();
        await c.ExecuteAsync(@"
            UPDATE usersession SET
                SessionData = @data,
                ExpiryTs = @expiryTs
            WHERE SessionId = @sessionId;",
            new { sessionId, data, expiryTs });
    }

    public async Task<string> CreateSession(string sessionId, byte[] data, DateTime expiryTs)
    {
        using var c = GetConnection();
        await c.ExecuteAsync(@"
            INSERT INTO usersession
                (SessionId, SessionData, ExpiryTs)
            VALUES
                (@sessionId, @data, @expiryTs);",
            new { sessionId, data, expiryTs });
        return sessionId;
    }

    #endregion

    public Db(IOptionsMonitor<ConnectionStrings> cfg)
    {
        Config = cfg;
    }
    IOptionsMonitor<ConnectionStrings> Config { get; }
    MySqlConnection GetConnection() => new(Config.CurrentValue.DefaultConnection);
}

