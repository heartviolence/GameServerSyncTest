
using MessagePack;
using UnityEngine;

[MessagePackObject]
public class TickMessageEffect
{
    [Key(0)]
    public bool Enable { get; set; } = true;
    [Key(1)]
    public float MaxDuration { get; set; }

    [Key(2)]
    public string Description { get; set; }

    [Key(3)]
    public float ElapsedUpdateTime { get; set; }

    [Key(4)]
    public float ElapsedLateUpdateTime { get; set; }

    [Key(5)]
    public float MessageInterval { get; set; }
    [Key(6)]
    public ActorMessage Message { get; set; }

    public void Update()
    {
        if (!Enable)
        {
            return;
        }
        ElapsedUpdateTime += WorldTime.DeltaTime;
    }

    public void LateUpdate()
    {
        if (!Enable)
        {
            return;
        }
        var limit = ElapsedUpdateTime > MaxDuration ? MaxDuration : ElapsedUpdateTime;

        for (; ElapsedLateUpdateTime + MessageInterval <= limit; ElapsedLateUpdateTime += MessageInterval)
        {
            ServerWorldStates.currentWorld.Actors.TryGetValue(Message.MessageProcessorId, out var actor);
            actor.ProcessActorMessage(Message);
        }

        if (ElapsedLateUpdateTime + MessageInterval > MaxDuration)
        {
            Enable = false;
        }
    }
}