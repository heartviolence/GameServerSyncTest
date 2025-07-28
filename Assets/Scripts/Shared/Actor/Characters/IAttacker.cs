public interface IAttacker
{
    long OwnerId { get; }
    float GetBaseDamage();
    public void AfterApplyDamage(float finalHitDamage, CharacterBase defender);
}