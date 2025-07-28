
using MessagePack;

[MessagePack.Union(0, typeof(AttackCounterEffect))]
public abstract class CombatSystemEffect
{
    [Key(0)]
    public bool Enable { get; set; } = true;

    [Key(1)]
    public float MaxDuration { get; set; }

    [Key(2)]
    public float ElapsedTime { get; set; }
    public void Update()
    {
        ElapsedTime += WorldTime.DeltaTime;
        if (ElapsedTime >= MaxDuration)
        {
            Enable = false;
        }
    }

    public virtual bool AttackCheck(CharacterBase me, float damage, IAttacker attacker)
    {
        return true;
    }
}

[MessagePackObject]
public class AttackCounterEffect : CombatSystemEffect
{
    [Key(3)]
    public CombatMessage Message { get; set; }
    [Key(4)]
    public int Count { get; set; } = 1;
    public override bool AttackCheck(CharacterBase me, float damage, IAttacker attacker)
    {
        if (!this.Enable)
        {
            return true;
        }
        Message.MessageProcessorId = me.CharacterData.ActorId;
        Message.TargetId = attacker.OwnerId;
        me.ProcessActorMessage(Message);
        Count--;

        if (Count == 0)
        {
            this.Enable = false;
        }
        return false;
    }
}