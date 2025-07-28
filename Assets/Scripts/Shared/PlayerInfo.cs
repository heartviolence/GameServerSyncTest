
using LiteNetLib;
using MessagePack;

[MessagePackObject]
public class PlayerInfo
{
    [IgnoreMember]
    public NetPeer Peer { get; set; } // server only

    [Key(0)]
    public long PlayerId { get; set; } = -1;

    [Key(1)]
    public long ControlActorId { get; set; } = -1;

    [Key(2)]
    public long LastUserInputSequenceNumber { get; set; } = -1;
}
