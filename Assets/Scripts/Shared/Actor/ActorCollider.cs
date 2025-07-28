
using MessagePack;
using UnityEngine;

[MessagePackObject]
public class ActorCollider
{
    [Key(0)]
    public float Radius { get; set; }
    [Key(1)]
    public ColliderType Type { get; set; } = ColliderType.NonMovable;
}

public enum ColliderType
{
    NonMovable = 0,
    Movable
}

public interface IActorCollider
{
    CircleCollider2D Collider { get; set; }

    long ActorId { get; }

    void WallCheck();
}

public interface IActorTrigger
{
    long ActorId { get; }
    Collider2D Trigger { get; }
}
