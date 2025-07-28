

using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ServerWorldStates
{
    public static ServerWorldStates currentWorld;

    [MessagePackObject]
    public class FrameEvent
    {
        [Key(0)]
        public long Frame { get; set; }
        [Key(1)]
        public float Time { get; set; }
        [Key(2)]
        public float DeltaTime { get; set; }
        [Key(3)]
        public List<ExternalWorldEvent> Events { get; set; } = new List<ExternalWorldEvent>();
    }

    public bool IsServer { get; set; } = false;

    public Dictionary<long, Actor> Actors { get; set; } = new();

    public Dictionary<long, FrameEvent> FrameEvents { get; set; } = new();

    public Dictionary<long, PlayerInfo> Players { get; set; } = new();

    List<Actor> _newActors = new(); // 현재프레임에 만들어진 actor

    List<long> _disableActors = new();

    public long NextActorId { get; set; } = 1;

    public long CurrentFrame { get; set; } = 0;

    const int eventHistroyLength = 3;


    /// <summary>
    /// serverOnly
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public void AddEventsCurrentFrame(ExternalWorldEvent e)
    {
        var frameEvent = GetFrameEvent(CurrentFrame);
        frameEvent.Events.Add(e);
    }

    public FrameEvent GetFrameEvent(long frame)
    {
        if (!FrameEvents.TryGetValue(CurrentFrame, out var frameEvent) || frameEvent == null)
        {
            FrameEvents[CurrentFrame] = frameEvent = new FrameEvent();
        }
        return frameEvent;
    }

    public void RegisterActor(Actor actor)
    {
        _newActors.Add(actor);
    }

    public void RegisterDisableActor(long id)
    {
        _disableActors.Add(id);
    }

    public Actor FindActor(long id)
    {
        if (Actors.TryGetValue(id, out var actor))
        {
            return actor;
        }
        return null;
    }

    public void Update()
    {
        ServerWorldStates.currentWorld = this;

        var deleteframe = CurrentFrame;
        var frameEvent = GetFrameEvent(CurrentFrame);
        if (IsServer)
        {
            deleteframe -= eventHistroyLength;
            frameEvent.Frame = CurrentFrame;
            frameEvent.Time = Time.time;
            frameEvent.DeltaTime = Time.deltaTime;
        }

        WorldTime.Time = frameEvent.Time;
        WorldTime.DeltaTime = frameEvent.DeltaTime;

        foreach (var actor in Actors.Values)
        {
            actor.Update();
        }

        foreach (var actor in Actors.Values)
        {
            actor.LateUpdate();
        }

        foreach (var actor in Actors.Values)
        {
            actor.TriggerCheck();
        }

        for (int i = 0; i < 3; i++)
        {
            Collider2DManager.instance.ActorCollisionStep();
        }

        ProcessEvent();
        foreach (var actor in Actors.Values)
        {
            actor.EndOfFrame();
        }

        FrameEvents.Remove(deleteframe);

        foreach (var newActor in _newActors)
        {
            newActor.ActorData.ActorId = NextActorId++;
            Actors.Add(newActor.ActorData.ActorId, newActor);
        }

        foreach (var newActor in _newActors)
        {
            newActor.AfterAddActor();
        }

        _newActors.Clear();
        foreach (var id in _disableActors)
        {
            Actors.Remove(id, out Actor disableActor);
            disableActor.Disable();
        }
        _disableActors.Clear();

        CurrentFrame++;
    }

    public void UpdateAnimation(long exceptId = -1)
    {
        foreach (var pair in Actors.Where(e => e.Key != exceptId))
        {
            pair.Value.UpdateAnimation();
        }
    }

    void ProcessEvent()
    {
        var frameEvents = GetFrameEvent(CurrentFrame);

        foreach (var e in frameEvents.Events)
        {
            switch (e)
            {
                case JoinPlayerEvent joinPlayerEvent:
                    var player = new PlayerInfo() { PlayerId = joinPlayerEvent.PlayerId };
                    Players.Add(player.PlayerId, player);


                    var reimu = new Reimu();
                    reimu.ActorData.ActorId = NextActorId++;
                    Actors.Add(reimu.ActorData.ActorId, reimu);
                    reimu.AfterAddActor();
                    player.ControlActorId = reimu.ActorData.ActorId;
                    break;
                case UserInput userInput:
                    ProcessUserInput(userInput);
                    break;
                case ActorSpawnEvent spawnEvent:
                    ProcessSpawnEvent(spawnEvent);
                    break;
                case PlayerSetControlCharacterEvent setControlEvent:
                    Players[setControlEvent.PlayerId].ControlActorId = setControlEvent.ActorId;
                    break;
                default:
                    break;
            }
        }
    }

    void ProcessUserInput(UserInput userInput)
    {
        var player = Players[userInput.PlayerId];
        if (player.LastUserInputSequenceNumber >= userInput.InputSequenceNumber)
        {
            return;
        }
        var actorId = player.ControlActorId;
        if (Actors.TryGetValue(actorId, out var actor))
        {
            actor.ProcessEvent(userInput);
            player.LastUserInputSequenceNumber = userInput.InputSequenceNumber;
        }
    }

    void ProcessSpawnEvent(ActorSpawnEvent spawnEvent)
    {
        switch (spawnEvent)
        {
            case ReimuSpawnEvent reimuSpawnEvent:
                var reimu = new Reimu();
                reimu.ActorData.ActorId = NextActorId++;
                Actors.Add(reimu.ActorData.ActorId, reimu);
                reimu.CharacterData.WorldPosition = reimuSpawnEvent.Position;
                reimu.AfterAddActor();
                break;
            case KakasiSpawnEvent kakasiSpawnEvent:
                var kakasi = new Kakasi();
                kakasi.ActorData.ActorId = NextActorId++;
                Actors.Add(kakasi.ActorData.ActorId, kakasi);
                kakasi.CharacterData.WorldPosition = kakasiSpawnEvent.Position;
                kakasi.AfterAddActor();
                break;
            case HealOrbSpawnEvent healOrbSpawnEvent:
                var healOrb = new HealOrb();
                healOrb.ActorData.ActorId = NextActorId++;
                Actors.Add(healOrb.ActorData.ActorId, healOrb);
                healOrb.CharacterData.WorldPosition = healOrbSpawnEvent.Position;
                healOrb.AfterAddActor();
                break;
        }
    }

    public void Resync(ServerWorldActorSnapshot snapshot)
    {
        foreach (var actor in Actors.Values)
        {
            actor.Disable();
        }

        Actors = snapshot.Actors;
        foreach (var actor in snapshot.Actors.Values)
        {
            actor.AfterAddActor();
        }

        var deleteList = FrameEvents
            .Select(e => e.Key)
            .Where(e => e < snapshot.Frame)
            .ToList();

        foreach (var e in deleteList)
        {
            FrameEvents.Remove(e);
        }
        CurrentFrame = snapshot.Frame;
        NextActorId = snapshot.NextActorId;
        Players = snapshot.Players;
    }

    public ServerWorldActorSnapshot CreateSnapshot()
    {
        return new()
        {
            Actors = Actors,
            NextActorId = NextActorId,
            Frame = CurrentFrame,
            Players = Players
        };
    }
}

public static class WorldTime
{
    static public float Time;
    static public float DeltaTime;
}


[MessagePackObject]
public class ServerWorldActorSnapshot
{
    [Key(0)]
    public long Frame { get; set; }
    [Key(1)]
    public Dictionary<long, Actor> Actors { get; set; } = new();

    [Key(2)]
    public long NextActorId { get; set; }

    [Key(3)]
    public Dictionary<long, PlayerInfo> Players { get; set; } = new();
}


public class ReliablePacket<T> : INetSerializable
{
    public byte[] Data { get; set; }

    public void Deserialize(NetDataReader reader)
    {
        Data = reader.GetBytesWithLength();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutBytesWithLength(Data);
    }

    public T MessagePackDeserialize()
    {
        return MessagePackSerializer.Deserialize<T>(Data);
    }

    public void MessagePackSerialize(T message)
    {
        Data = MessagePackSerializer.Serialize(message);
    }
}
