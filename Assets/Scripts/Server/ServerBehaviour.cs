
using Codice.CM.Client.Differences;
using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEditor.Sprites;
using UnityEngine;
using UnityEngine.LightTransport;

public class ServerBehaviour : MonoBehaviour, INetEventListener
{

    private NetManager _netManager;
    private NetPacketProcessor _netPacketProcessor;
    private NetDataWriter _netDataWriter;

    public ServerWorldStates World { get; set; } = new() { IsServer = true };

    Dictionary<long, PlayerInfo> players = new();

    long nextPlayerId = 1; 


    public void Initialize()
    {
        _netManager = new NetManager(this)
        {
            AutoRecycle = true
        };
        _netPacketProcessor = new NetPacketProcessor();
        _netDataWriter = new NetDataWriter();
        _netManager.SimulateLatency = true;
        _netManager.SimulationMinLatency = 150;
        _netManager.SimulationMaxLatency = 151;
        _netPacketProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetVector2());
        _netPacketProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetVector3());
        _netPacketProcessor.Subscribe<JoinPacket, NetPeer>(OnJoinReceived, () => new JoinPacket());
        _netPacketProcessor.SubscribeNetSerializable<UserInput, NetPeer>(OnUserInputReceived, () => new UserInput());
        _netPacketProcessor.SubscribeReusable<ResyncRequest, NetPeer>(OnResyncReceived);

        var result = _netManager.Start(7777);

        World.AddEventsCurrentFrame(new KakasiSpawnEvent() { Position = new Vector3(0, 0, 5) });
        World.AddEventsCurrentFrame(new HealOrbSpawnEvent() { Position = new Vector3(3, 0, 3) });
    }

    public void SendWorldEvent()
    {
        foreach (var pair in World.FrameEvents)
        {
            if (pair.Key > World.CurrentFrame)
            {
                break;
            }

            var frameEvent = pair.Value;
            ReliablePacket<ServerWorldStates.FrameEvent> sendPacket = new();
            sendPacket.MessagePackSerialize(frameEvent);
            _netDataWriter.Reset();
            _netDataWriter.Put((byte)PacketType.Serialized);
            _netPacketProcessor.WriteNetSerializable(_netDataWriter, ref sendPacket);

            //send to Player
            foreach (var player in players.Values)
            {
                player.Peer.Send(_netDataWriter, DeliveryMethod.ReliableOrdered);
            }
        }
    }

    public void Update_()
    {

        _netManager.PollEvents();
        World.Update();
        //World.UpdateAnimation();
        SendWorldEvent();
    }

    #region packet

    public void OnResyncReceived(ResyncRequest request, NetPeer netpeer)
    {
        UnityEngine.Debug.Log("ResyncReceived server");
        var actorSnapshot = World.CreateSnapshot();

        var resyncAnswer = new ReliablePacket<ServerWorldActorSnapshot>();
        resyncAnswer.MessagePackSerialize(actorSnapshot);

        netpeer.Send(WritePacketSerializable(resyncAnswer), DeliveryMethod.ReliableOrdered);
    }

    public void OnJoinReceived(JoinPacket joinpacket, NetPeer netPeer)
    {
        var id = nextPlayerId++;
        netPeer.Tag = id;
        players.Add(id, new PlayerInfo() { Peer = netPeer, PlayerId = id });
        World.AddEventsCurrentFrame(new JoinPlayerEvent() { PlayerId = id, PlayerName = joinpacket.UserName, CharacterType = 0 });

        UnityEngine.Debug.Log($"[Server] Join packet received from {netPeer} with username: {joinpacket.UserName}");
        netPeer.Send(WritePacket(new JoinPacketAnswer() { PlayerId = id }), DeliveryMethod.ReliableUnordered);
    }

    public void OnUserInputReceived(UserInput input, NetPeer peer)
    {
        input.PlayerId = (long)peer.Tag;
        var player = players[(long)peer.Tag];

        if (player.LastUserInputSequenceNumber < input.InputSequenceNumber)
        {
            player.LastUserInputSequenceNumber = input.InputSequenceNumber;
            World.AddEventsCurrentFrame(input);
        }
        else
        {
            UnityEngine.Debug.Log($"id:{input.PlayerId},LastInput:{player.LastUserInputSequenceNumber},received:{input.InputSequenceNumber}");
        }
    }

    public NetDataWriter WritePacket<T>(T packet) where T : class, new()
    {
        _netDataWriter.Reset();
        _netDataWriter.Put((byte)PacketType.Serialized);
        _netPacketProcessor.Write(_netDataWriter, packet);
        return _netDataWriter;
    }

    public NetDataWriter WritePacketSerializable<T>(T packet) where T : class, INetSerializable, new()
    {
        _netDataWriter.Reset();
        _netDataWriter.Put((byte)PacketType.Serialized);
        _netPacketProcessor.WriteNetSerializable(_netDataWriter, ref packet);
        return _netDataWriter;
    }


    #endregion
    public void OnConnectionRequest(ConnectionRequest request)
    {
        UnityEngine.Debug.Log($"[Server] Connection request from {request.RemoteEndPoint}");
        request.AcceptIfKey("Battle");
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        UnityEngine.Debug.LogError($"[Server] Network error: {socketError.ToString()}");
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        //throw new System.NotImplementedException();
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {

        byte packetTypeByte = reader.GetByte();
        if (packetTypeByte >= Enum.GetValues(typeof(PacketType)).Length + 1)
        {
            UnityEngine.Debug.LogError($"[Server] Unknown packet type: {packetTypeByte}");
            return;
        }

        PacketType packetType = (PacketType)packetTypeByte;
        switch (packetType)
        {
            case PacketType.Serialized:
                _netPacketProcessor.ReadAllPackets(reader, peer);
                break;
            default:
                UnityEngine.Debug.LogError($"[Server] Unknown packet type: {packetType}");
                break;
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {

    }

    public void OnPeerConnected(NetPeer peer)
    {
        UnityEngine.Debug.Log($"[Server] peer Connected {peer}");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        UnityEngine.Debug.Log($"[Server] peer Disconnected {disconnectInfo.Reason}");


    }
}