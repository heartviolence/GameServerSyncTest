
using MessagePack;

[MessagePack.Union(0, typeof(AddDefaultDamageMessage))]
public abstract class ActorMessage
{
    [Key(0)]
    public long MessageProcessorId { get; set; }
}

[MessagePack.Union(0, typeof(AddDefaultDamageMessage))] 
public abstract class CombatMessage : ActorMessage
{
    [Key(1)]
    public long TargetId { get; set; }
}

[MessagePackObject]
public class AddDefaultDamageMessage : CombatMessage
{
    [Key(2)]
    public float DefaultDamage { get; set; }
}
