using MessagePack;
using UnityEngine;

[MessagePack.Union(0, typeof(Reimu))]
public abstract class CharacterBase : Actor
{
    [IgnoreMember]
    public abstract CharacterData CharacterData { get; }

    protected Vector3 GetDefaultMoveVectorD3(Vector3 moveInput, float deltaTime, float speed)
    {
        return moveInput.D2_D3() * deltaTime * speed;
    }
    public abstract void PredictUpdate();

    public abstract void DeletePredicted(long sequenceNumber);

    public abstract void PredicateUserInput(UserInput e);

    public virtual void HitDamage(float damage)
    {
        CharacterData.CurrentHP = Mathf.Clamp(CharacterData.CurrentHP - damage, 0, CharacterData.MaxHP);
        if (CharacterData.CurrentHP == 0)
        {
            CharacterData.Enable = false;
            ServerWorldStates.currentWorld.RegisterDisableActor(CharacterData.ActorId);
        }
    }

    public abstract bool AttackCheck(float damage, IAttacker attacker);

    public abstract float ApplyDamage(float damage, IAttacker attacker);

    public override void Update()
    {
        CharacterData.Update();
    }

    public override void LateUpdate()
    {
        CharacterData.LateUpdate();
    }
}

