using System.Data;
using System.Threading;
using System.ComponentModel;
using MySql.Data.MySqlClient;
using Dapper.Transaction;
using JetHerald.Options;
using JetHerald.Contracts;

namespace JetHerald.Services;

public class Db
{
    public Db(IOptionsMonitor<ConnectionStrings> cfg)
    {
        Config = cfg;
    }
    IOptionsMonitor<ConnectionStrings> Config { get; }
    MySqlConnection GetConnection() => new(Config.CurrentValue.DefaultConnection);
    public async Task<DbContext> GetContext(
        IsolationLevel lvl = IsolationLevel.RepeatableRead,
        CancellationToken token = default)
    {
        var conn = GetConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        var tran = await conn.BeginTransactionAsync(lvl, token);
        return new DbContext(tran);
    }
}

public class DbContext : IDisposable
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public DbContext(IDbTransaction tran)
    {
        Tran = tran;
        Conn = Tran.Connection;
    }

    IDbConnection Conn;
    IDbTransaction Tran;

    public void Commit() => Tran.Commit();
    public void Dispose()
    {
        Tran.Dispose();
        Conn.Dispose();
    }
    public Task<IEnumerable<Topic>> GetTopicsForUser(uint userId)
        => Tran.QueryAsync<Topic>(
            " SELECT * FROM topic WHERE CreatorId = @userId",
            new { userId });

    public Task UpdatePerms(uint userId, uint planId, uint roleId)
        => Tran.ExecuteAsync(@"
            UPDATE user
                SET PlanId = @planId,
                    RoleId = @roleId
                WHERE UserId = @userId",
            new { userId, planId, roleId });

    public Task<IEnumerable<Plan>> GetPlans()
        => Tran.QueryAsync<Plan>("SELECT * FROM plan");
    public Task<IEnumerable<Role>> GetRoles()
        => Tran.QueryAsync<Role>("SELECT * FROM role");
    public Task<IEnumerable<UserInvite>> GetInvites()
        => Tran.QueryAsync<UserInvite>(@"
            SELECT ui.*, u.Login as RedeemedByLogin
                FROM userinvite ui
                LEFT JOIN user u ON ui.RedeemedBy = u.UserId");
    public Task<IEnumerable<User>> GetUsers()
        => Tran.QueryAsync<User>(@"
            SELECT u.*
                FROM user u;");

    public Task<IEnumerable<Heart>> GetHeartsForUser(uint userId)
        => Tran.QueryAsync<Heart>(
            " SELECT h.* FROM heart h JOIN topic USING (TopicId) WHERE CreatorId = @userId",
            new { userId });

    public Task CreateUserInvite(uint planId, uint roleId, string inviteCode)
        => Tran.ExecuteAsync(@"
            INSERT INTO userinvite
                ( PlanId,  RoleId,  InviteCode)
                VALUES
                (@planId, @roleId, @inviteCode)",
            new { planId, roleId, inviteCode });

    public Task DeleteUserInvite(uint inviteId)
        => Tran.ExecuteAsync(@" DELETE FROM userinvite WHERE UserInviteId = @inviteId",
            new { inviteId });


    public Task<Topic> GetTopic(string name)
        => Tran.QuerySingleOrDefaultAsync<Topic>(
            "SELECT * FROM topic WHERE Name = @name",
            new { name });

    public Task<int> DeleteTopic(string name, uint userId)
        => Tran.ExecuteAsync(
            " DELETE FROM topic WHERE Name = @name AND CreatorId = @userId",
            new { name, userId });

    public Task<Topic> GetTopicForSub(string token, NamespacedId sub)
        => Tran.QuerySingleOrDefaultAsync<Topic>(
            " SELECT t.*, ts.Sub " +
            " FROM topic t " +
            " LEFT JOIN topic_sub ts ON t.TopicId = ts.TopicId AND ts.Sub = @sub " +
            " WHERE ReadToken = @token",
            new { token, sub });

    public Task<IEnumerable<Heart>> GetHeartsForTopic(uint topicId)
        => Tran.QueryAsync<Heart>(
            " SELECT * FROM heart WHERE TopicId = @topicId",
            new { topicId });
    public Task<User> GetUser(string login)
        => Tran.QuerySingleOrDefaultAsync<User>(@"
            SELECT u.*, up.*, ur.*
                FROM user u 
                JOIN plan up ON u.PlanId = up.PlanId
                JOIN role ur ON u.RoleId = ur.RoleId 
                WHERE u.Login = @login;",
            new { login });

    public async Task<Topic> CreateTopic(uint user, string name, string descr)
    {
        var topicsCount = await Tran.QuerySingleAsync<int>(
            " SELECT COUNT(*) " +
            " FROM user u " +
            " LEFT JOIN topic t ON t.CreatorId = u.UserId " +
            " WHERE u.UserId = @user",
            new { user }
        );

        var planTopicsCount = await Tran.QuerySingleAsync<int>(
            " SELECT p.MaxTopics " +
            " FROM user u " +
            " LEFT JOIN plan p ON p.PlanId = u.PlanId " +
            " WHERE u.UserId = @user",
            new { user }
        );

        if (topicsCount >= planTopicsCount) return null;

        var topic = await Tran.QuerySingleOrDefaultAsync<Topic>(
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
            });
        return topic;
    }

    public async Task<User> RegisterUser(User user)
    {
        _ = await Tran.QuerySingleOrDefaultAsync<uint>(@"
            INSERT INTO user
		        ( Login,  Name,  PasswordHash,  PasswordSalt,  HashType,  PlanId,  RoleId)
	        VALUES
		        (@Login, @Name, @PasswordHash, @PasswordSalt, @HashType, @PlanId, @RoleId);",
            param: user);
        return await GetUser(user.Login);
    }

    public Task RedeemInvite(uint inviteId, uint userId)
        => Tran.ExecuteAsync(
            @"UPDATE userinvite SET RedeemedBy = @userId WHERE UserInviteId = @inviteId",
            new { inviteId, userId });

    public Task<UserInvite> GetInviteByCode(string inviteCode)
        => Tran.QuerySingleOrDefaultAsync<UserInvite>(
            " SELECT * FROM userinvite " +
            " WHERE InviteCode = @inviteCode " +
            " AND RedeemedBy IS NULL ",
            new { inviteCode });

    public Task<IEnumerable<NamespacedId>> GetSubsForTopic(uint topicId)
        => Tran.QueryAsync<NamespacedId>(
            " SELECT Sub " +
            " FROM topic_sub " +
            " WHERE TopicId = @topicid",
            new { topicId });

    public Task<IEnumerable<Topic>> GetTopicsForSub(NamespacedId sub)
        => Tran.QueryAsync<Topic>(
            " SELECT t.*" +
            " FROM topic_sub ts" +
            " JOIN topic t USING (TopicId)" +
            " WHERE ts.Sub = @sub",
            new { sub });

    public Task CreateSubscription(uint topicId, NamespacedId sub)
        => Tran.ExecuteAsync(
            " INSERT INTO topic_sub" +
            " (TopicId, Sub)" +
            " VALUES" +
            " (@topicId, @sub)",
            new { topicId, sub });

    public Task<int> RemoveSubscription(string topicName, NamespacedId sub)
        => Tran.ExecuteAsync(
            " DELETE ts " +
            " FROM topic_sub ts" +
            " JOIN topic t USING (TopicId) " +
            " WHERE t.Name = @topicName AND ts.Sub = @sub;",
            new { topicName, sub });


    public Task<int> ReportHeartbeat(uint topicId, string heart, int timeoutSeconds)
        => Tran.QueryFirstAsync<int>(
            @"CALL report_heartbeat(@topicId, @heart, @timeoutSeconds);",
            new { topicId, heart, timeoutSeconds });

    public Task<IEnumerable<HeartEvent>> ProcessHearts()
        => Tran.QueryAsync<HeartEvent>("CALL process_hearts();");

    public Task MarkHeartAttackReported(ulong id)
        => Tran.ExecuteAsync("UPDATE heartevent SET Status = 'reported' WHERE HeartEventId = @id", new { id });

    #region TicketStore

    public Task RemoveSession(string sessionId)
        => Tran.ExecuteAsync("DELETE FROM usersession WHERE SessionId = @sessionId", new { sessionId });
    public Task<UserSession> GetSession(string sessionId)
        => Tran.QuerySingleOrDefaultAsync<UserSession>(
            "SELECT * FROM usersession WHERE SessionId = @sessionId",
            new { sessionId });

    public Task UpdateSession(string sessionId, byte[] data, DateTime expiryTs)
        => Tran.ExecuteAsync(@"
            UPDATE usersession SET
                SessionData = @data,
                ExpiryTs = @expiryTs
            WHERE SessionId = @sessionId;",
            new { sessionId, data, expiryTs });

    public async Task<string> CreateSession(string sessionId, byte[] data, DateTime expiryTs)
    {
        await Tran.ExecuteAsync(@"
            INSERT INTO usersession
                (SessionId, SessionData, ExpiryTs)
            VALUES
                (@sessionId, @data, @expiryTs);",
            new { sessionId, data, expiryTs });
        return sessionId;
    }

    #endregion
}

