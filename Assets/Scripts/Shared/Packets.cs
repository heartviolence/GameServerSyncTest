
using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum PacketType
{
    Serialized = 1,
}
public class JoinPacket
{
    public string UserName { get; set; }
    public uint Id { get; set; }
}

public class JoinPacketAnswer
{
    public long PlayerId { get; set; }
}

public class ResyncRequest
{

}

public static class Extensions
{
    public static void Put(this NetDataWriter writer, Vector2 vector)
    {
        writer.Put(vector.x);
        writer.Put(vector.y);
    }

    public static void Put(this NetDataWriter writer, Vector3 vector)
    {
        writer.Put(vector.x);
        writer.Put(vector.y);
        writer.Put(vector.z);
    }

    public static Vector2 GetVector2(this NetDataReader reader)
    {
        Vector2 v;
        v.x = reader.GetFloat();
        v.y = reader.GetFloat();
        return v;
    }

    public static Vector3 GetVector3(this NetDataReader reader)
    {
        Vector3 v;
        v.x = reader.GetFloat();
        v.y = reader.GetFloat();
        v.z = reader.GetFloat();
        return v;
    }

    public static T GetRandomElement<T>(this T[] array)
    {
        return array[UnityEngine.Random.Range(0, array.Length)];
    } 
}