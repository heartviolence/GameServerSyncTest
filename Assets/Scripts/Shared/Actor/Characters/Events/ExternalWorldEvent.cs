
using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using System.Collections.Generic;
using UnityEngine;

[MessagePack.Union(0, typeof(GameStartEvent))]
[MessagePack.Union(1, typeof(JoinPlayerEvent))]
[MessagePack.Union(2, typeof(PlayerSetControlCharacterEvent))]
[MessagePack.Union(3, typeof(UserInput))]
[MessagePack.Union(4, typeof(ReimuSpawnEvent))]
[MessagePack.Union(5, typeof(KakasiSpawnEvent))]
[MessagePack.Union(6, typeof(HealOrbSpawnEvent))]
public abstract class ExternalWorldEvent
{

}

[MessagePackObject]
public class GameStartEvent : ExternalWorldEvent
{
}


[MessagePackObject]
public class JoinPlayerEvent : ExternalWorldEvent
{
    [Key(1)]
    public long PlayerId { get; set; }
    [Key(2)]
    public string PlayerName { get; set; }

    [Key(3)]
    public int CharacterType { get; set; }
}

[MessagePackObject]
public class PlayerSetControlCharacterEvent : ExternalWorldEvent
{
    [Key(1)]
    public long PlayerId { get; set; }
    [Key(2)]
    public long ActorId { get; set; }
}

public abstract class ActorSpawnEvent : ExternalWorldEvent
{
    [Key(1)]
    public Vector3 Position { get; set; }
}

[MessagePackObject]
public class ReimuSpawnEvent : ActorSpawnEvent
{
}

[MessagePackObject]
public class HealOrbSpawnEvent : ActorSpawnEvent
{

}

[MessagePackObject]
public class KakasiSpawnEvent : ActorSpawnEvent
{
}

[MessagePackObject]
public class UserInput : ExternalWorldEvent, INetSerializable
{
    [Key(1)]
    public long InputSequenceNumber { get; set; }
    [Key(2)]
    public long PlayerId { get; set; }
    [Key(3)]
    public Vector2 MoveInput { get; set; }

    [Key(4)]
    public float DeltaTime { get; set; }
    [Key(5)]
    public Vector3 LookPoint { get; set; }

    [Key(6)]
    public List<CharacterActionCode> ActionCodes { get; set; } = new();

    [Key(7)]
    public bool CanMove { get; set; }
    [Key(8)]
    public bool CanLook { get; set; }

    [IgnoreMember]
    public Vector3 MoveVectorD3 { get; set; }

    public void Deserialize(NetDataReader reader)
    {
        InputSequenceNumber = reader.GetLong();
        PlayerId = reader.GetLong();
        MoveInput = reader.GetVector2();
        DeltaTime = reader.GetFloat();
        LookPoint = reader.GetVector3();
        var actionCodeCount = reader.GetInt();
        for (int i = 0; i < actionCodeCount; i++)
        {
            ActionCodes.Add((CharacterActionCode)reader.GetInt());
        }
        CanMove = reader.GetBool();
        CanLook = reader.GetBool();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(InputSequenceNumber);
        writer.Put(PlayerId);
        writer.Put(MoveInput);
        writer.Put(DeltaTime);
        writer.Put(LookPoint);
        writer.Put(ActionCodes.Count);
        for (int i = 0; i < ActionCodes.Count; i++)
        {
            writer.Put((int)ActionCodes[i]);
        }
        writer.Put(CanMove);
        writer.Put(CanLook);
    }
}

public enum CharacterActionCode
{
    A,
    B,
    C
}