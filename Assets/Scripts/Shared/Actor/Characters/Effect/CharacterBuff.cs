
using MessagePack;
using System.Collections.Generic;

[MessagePackObject]
public class CharacterBuff
{
    [Key(0)]
    public bool Enable { get; set; } = true;

    [Key(1)]
    public float MaxDuration { get; set; }

    [Key(2)]
    public float ElapsedTime { get; set; } 

    [Key(3)]
    public List<CharacterControlEffect> CharacterControlEffects { get; set; } = new();

    [Key(4)]
    public List<StatModifierEffect> StatModifierEffects { get; set; } = new();

    [Key(5)]
    public List<TickMessageEffect> TickMessageEffects { get; set; } = new();

    [Key(6)]
    public List<CombatSystemEffect> CombatSystemEffects { get; set; } = new();

    public void Update()
    {
        ElapsedTime += WorldTime.DeltaTime;
        if (ElapsedTime >= MaxDuration)
        {
            Enable = false;
        }

        foreach (var e in CharacterControlEffects)
        {
            e.Update();
        }
        CharacterControlEffects.RemoveAll(e => !e.Enable);

        foreach (var e in StatModifierEffects)
        {
            e.Update();
        }
        StatModifierEffects.RemoveAll(e => !e.Enable);

        foreach (var e in TickMessageEffects)
        {
            e.Update();
        }
        TickMessageEffects.RemoveAll(e => !e.Enable);

        foreach (var e in CombatSystemEffects)
        {
            e.Update();
        }
        CombatSystemEffects.RemoveAll(e => !e.Enable);
    }

    public void LateUpdate()
    {
        foreach (var e in TickMessageEffects)
        {
            e.LateUpdate();
        }
        TickMessageEffects.RemoveAll(e => !e.Enable);
    }
}