namespace JetHerald.Contracts;

public class Topic
{
    public uint TopicId { get; set; }
    public uint CreatorId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ReadToken { get; set; }
    public string WriteToken { get; set; }

    public NamespacedId? Sub { get; set; }

    public override string ToString()
        => Name == Description ? Name : $"{Name}: {Description}";
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
    public NamespacedId? ForeignId { get; set; }
    public uint PlanId { get; set; }

    public int? MaxTopics { get; set; }
    public int? TimeoutMultiplier { get; set; }
}
