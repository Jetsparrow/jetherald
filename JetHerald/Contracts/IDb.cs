namespace JetHerald.Contracts;
 
public class Topic
{
    public uint TopicId { get; set; }
    public NamespacedId Creator { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ReadToken { get; set; }
    public string WriteToken { get; set; }
    public string AdminToken { get; set; }

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
