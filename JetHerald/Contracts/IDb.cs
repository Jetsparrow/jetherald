namespace JetHerald.Contracts;

public class Topic
{
    public uint TopicId { get; private set; }
    public uint CreatorId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ReadToken { get; set; }
    public string WriteToken { get; set; }


    // joined
    public NamespacedId? Sub { get; private set; }
    public string? Login { get; private set; }
    public double TimeoutMultiplier { get; private set; } = 1;

    public override string ToString()
        => Name == Description ? Name : $"{Name}: {Description}";
}

public class Heart
{
    public uint HeartId { get; set; }
    public uint TopicId { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
    public DateTime LastBeatTs { get; set; }
    public DateTime ExpiryTs { get; set; }
    public DateTime CreateTs { get; set; }
}

public class HeartEvent
{
    public ulong HeartEventId { get; set; }
    public uint TopicId { get; set; }
    public string Heart { get; set; }
    public DateTime CreateTs { get; set; }

    public string Description { get; set; }
}

public class User
{
    public uint UserId { get; set; }
    public string Login { get; set; }
    public string Name { get; set; }
    public byte[] PasswordHash { get; set; }
    public byte[] PasswordSalt { get; set; }
    public int HashType { get; set; }
    public uint PlanId { get; set; }
    public uint RoleId { get; set; }

    public string Allow { get; set; }
   
    public int? MaxTopics { get; set; }
    public double? TimeoutMultiplier { get; set; }

    public DateTime CreateTs { get; set; }
}
public class UserInvite
{
    public uint UserInviteId { get; set; }
    public string InviteCode { get; set; }
    public uint PlanId { get; set; }
    public uint RoleId { get; set; }
    public uint RedeemedBy { get; set; }
    public string? RedeemedByLogin { get; set; }
}

public class UserSession
{
    public string SessionId { get; set; }
    public byte[] SessionData { get; set; }
    public DateTime ExpiryTs { get; set; }
}

public class Plan
{
    public uint PlanId { get; set; }
    public string Name { get; set; }
    public int MaxTopics { get; set; }
    public double TimeoutMultiplier { get; set; }
}

public class Role
{
    public uint RoleId { get; set; }
    public string Name { get; set; }
    public string Allow { get; set; }
}
