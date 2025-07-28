

using MessagePack;
using UnityEngine;

[MessagePackObject]
public sealed class DefaultCharacterState
{
    public bool AttackCheck(CharacterBase me, float damage, IAttacker attacker)
    {
        foreach (var buff in me.CharacterData.Buffs)
        {
            foreach (var combatEffect in buff.CombatSystemEffects)
            {
                if (combatEffect.AttackCheck(me, damage, attacker) == false)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public float ApplyDamage(CharacterBase me, float damage, IAttacker attacker)
    {
        return DefaultApplyDamage(me, damage, attacker);
    }

    public static float DefaultApplyDamage(CharacterBase me, float damage, IAttacker attacker)
    {
        float beforeHP = me.CharacterData.CurrentHP;
        me.CharacterData.CurrentHP = Mathf.Clamp(me.CharacterData.CurrentHP - damage, 0, me.CharacterData.MaxHP); ;
        if (me.CharacterData.CurrentHP == 0)
        {
            me.CharacterData.Enable = false;
            ServerWorldStates.currentWorld.RegisterDisableActor(me.CharacterData.ActorId);
        }

        return beforeHP - me.CharacterData.CurrentHP;
    }
}