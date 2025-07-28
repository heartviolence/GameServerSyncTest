using MessagePack;
using UnityEngine;

[MessagePackObject]
public class ActorData
{
    [Key(0)]
    public long ActorId { get; set; }
    [Key(1)]
    public bool Enable { get; set; } = true;

    [Key(2)]
    public Vector3 WorldPosition { get; set; } = Vector3.zero;

    [Key(3)]
    public Vector3 LookPoint { get; set; } = Vector3.zero;

    [Key(4)]
    public ActorCollider Collider { get; set; } = new();
}

