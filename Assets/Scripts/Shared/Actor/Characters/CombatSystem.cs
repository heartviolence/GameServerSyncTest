
using UnityEngine;

public static class CombatSystem
{
    static private DefaultDamageAttacker attackerCash = new();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="defender"></param>
    /// <returns> false = 공격대상아님,true = 공격이 무효화되거나 공격이 적용되었음</returns>
    public static bool Attack(IAttacker attacker, CharacterBase defender)
    {
        if (attacker == null ||
            defender == null ||
            defender.CharacterData.Enable == false)
        {
            return false;
        }

        //데미지계산
        float defaultDamage = attacker.GetBaseDamage();

        float finalDamage = defaultDamage;

        var beforeHP = defender.CharacterData.CurrentHP;

        //공격무효화 타이밍
        if (!defender.AttackCheck(finalDamage, attacker))
        {
            return true;
        }

        //데미지 적용타이밍
        var applyDamage = defender.ApplyDamage(finalDamage, attacker);
        attacker.AfterApplyDamage(applyDamage, defender);

        var afterHP = defender.CharacterData.CurrentHP;
        UnityEngine.Debug.Log($"{defender.CharacterData.ActorId} : {beforeHP} -> {afterHP}");

        return true;
    }

    public static void AttackWithDefaultDamage(CharacterBase attacker, float defaultDamage, CharacterBase defender)
    {
        attackerCash.DefaultDamage = defaultDamage;
        attackerCash.OwnerId = attacker.CharacterData.ActorId;
        Attack(attackerCash, defender);
    }

    public static float Heal(CharacterBase healer, float healPoint, CharacterBase target)
    {
        var beforeHp = target.CharacterData.CurrentHP;
        target.CharacterData.CurrentHP = Mathf.Clamp(beforeHp + healPoint, 0, target.CharacterData.MaxHP);
        UnityEngine.Debug.Log($"{target.ActorData.ActorId}: {beforeHp} -> {target.CharacterData.CurrentHP}");
        return target.CharacterData.CurrentHP - beforeHp;
    }
}


public class DefaultDamageAttacker : IAttacker
{
    public float DefaultDamage { get; set; }

    public long OwnerId { get; set; }

    public void AfterApplyDamage(float finalHitDamage, CharacterBase defender)
    {

    }

    public float GetBaseDamage()
    {
        return DefaultDamage;
    }
}