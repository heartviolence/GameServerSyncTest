
using MessagePack;
using System.Collections.Generic;
using UnityEngine;

[MessagePackObject]
public class CharacterData : ActorData
{
    [Key(5)]
    public uint TeamId { get; set; }

    [Key(6)]
    public float MaxHP { get; set; }

    [Key(7)]
    public float CurrentHP { get; set; }

    [Key(8)]
    public List<CharacterBuff> Buffs { get; set; } = new();

    public void Update()
    {
        foreach (var e in Buffs)
        {
            e.Update();
        }
        Buffs.RemoveAll(e => !e.Enable);
    }

    public void LateUpdate()
    {
        foreach (var e in Buffs)
        {
            e.LateUpdate();
        }
    }

}


