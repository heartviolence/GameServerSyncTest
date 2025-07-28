
using LiteNetLib; 
using UnityEngine; 
using LiteNetLib.Utils;
using System.Net.Sockets;
using System.Net;
using System;
using TMPro; 
public class ClientBehaviour : MonoBehaviour, INetEventListener
{
    public static ClientBehaviour instance;

    private NetManager _netManager;
    private NetDataWriter _netDataWriter;
    private NetPeer _server;
    private int _ping;

    private NetPacketProcessor _netPacketProcessor;

    public ServerWorldStates World { get; set; } = new();

    public void Initialize()
    {
        _netManager = new NetManager(this)
        {
            AutoRecycle = true
        };
        _netDataWriter = new NetDataWriter();
        _netPacketProcessor = new NetPacketProcessor();
        _netPacketProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetVector2());
        _netPacketProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetVector3());

        _netPacketProcessor.SubscribeNetSerializable(ServerSnapshotReceived, () => new ReliablePacket<ServerWorldActorSnapshot>());
        _netPacketProcessor.SubscribeNetSerializable(FrameEventRecevied, () => new ReliablePacket<ServerWorldStates.FrameEvent>());

        _netManager.Start();
    }
    public void Update_()
    {
        _netManager.PollEvents();
        while (World.FrameEvents.TryGetValue(World.CurrentFrame, out _))
        {
            World.Update();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            Connect("localhost", null);
        }
    }

    void ServerSnapshotReceived(ReliablePacket<ServerWorldActorSnapshot> packet)
    {
        UnityEngine.Debug.Log("resyncAnswer Received");
        var snapshot = packet.MessagePackDeserialize();
        World.Resync(snapshot);
    }
    void FrameEventRecevied(ReliablePacket<ServerWorldStates.FrameEvent> packet)
    {
        var frameEvent = packet.MessagePackDeserialize();
        World.FrameEvents[frameEvent.Frame] = frameEvent;
    }

    public void RegisterCallBack<T>(Action<T, NetPeer> callback) where T : class, new()
    {
        _netPacketProcessor.SubscribeReusable(callback);
    }

    public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
    {
        if (_server == null)
            return;

        _netDataWriter.Reset();
        _netDataWriter.Put((byte)PacketType.Serialized);
        _netPacketProcessor.Write(_netDataWriter, packet);
        _server.Send(_netDataWriter, deliveryMethod);
    }

    public void SendSerializablePacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, INetSerializable, new()
    {
        if (_server == null)
            return;

        _netDataWriter.Reset();
        _netDataWriter.Put((byte)PacketType.Serialized);
        _netPacketProcessor.WriteNetSerializable(_netDataWriter,ref packet);
        _server.Send(_netDataWriter, deliveryMethod);
    }

    public void SendPacket(UserInput input,DeliveryMethod deliveryMethod)
    {
        SendSerializablePacket(input, deliveryMethod);
    }

    public void Connect(string ip, Action<DisconnectInfo> onDisconnected)
    {
        _netManager.Connect(ip, 7777, "Battle");
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.Reject();
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Debug.Log($"[Client] Network error: {socketError.ToString()}");
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        _ping = latency;
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        byte packetTypeByte = reader.GetByte();
        if (packetTypeByte >= Enum.GetValues(typeof(PacketType)).Length + 1)
        {
            Debug.LogError($"[Client] Unknown packet type: {packetTypeByte}");
            return;
        }

        PacketType packetType = (PacketType)packetTypeByte;
        switch (packetType)
        {
            case PacketType.Serialized:
                _netPacketProcessor.ReadAllPackets(reader, peer);
                break;
            default:
                Debug.LogError($"[Client] Unknown packet type: {packetType}");
                break;
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {

    }

    public void OnPeerConnected(NetPeer peer)
    {
        Debug.Log($"[Client] Connect to server {peer}");
        _server = peer;

        SendPacket(new JoinPacket { UserName = "Player" }, DeliveryMethod.ReliableOrdered);
        SendPacket(new ResyncRequest(), DeliveryMethod.ReliableOrdered);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _server = null;
        Debug.Log($"[Client] Disconnected from server: {disconnectInfo.Reason}");
    }

}