
using MessagePack;
using UnityEngine;

[MessagePackObject]
public class KakasiData : CharacterData
{
    [Key(9)]
    public DefaultCharacterState CharacterState { get; set; } = new();
}

[MessagePackObject(AllowPrivate = true)]
public partial class Kakasi : CharacterBase, IActorCollider
{
    [Key(1)]
    KakasiData _data;

    [IgnoreMember]
    public KakasiGameObject _gameObject;

    [IgnoreMember]
    public override CharacterData CharacterData => _data;

    public override ActorData ActorData => _data;

    [IgnoreMember]
    public CircleCollider2D Collider { get; set; }

    [IgnoreMember]
    long IActorCollider.ActorId => _data.ActorId;

    public Kakasi()
    {
        _data = new();
        _data.CurrentHP = _data.MaxHP = 100;
        _data.Collider.Radius = 0.5f;
        _data.Collider.Type = ColliderType.NonMovable;
        var buff = new CharacterBuff() { MaxDuration = 100000f };
        var combatMessage = new AddDefaultDamageMessage() { DefaultDamage = 50 };
        var counterEffect = new AttackCounterEffect() { Message = combatMessage, MaxDuration = 100000f, Count = 1000 };
        buff.CombatSystemEffects.Add(counterEffect);
        _data.Buffs.Add(buff);
    }

    public override void AfterAddActor()
    {
        var prefab = ActorPreloader.Prefabs[nameof(KakasiGameObject)];
        var instance = GameObject.Instantiate(prefab);
        _gameObject = instance.GetComponentInChildren<KakasiGameObject>();

        _gameObject.gameObject.name = _data.ActorId.ToString();
        Collider2DManager.instance.Initialize_ActorCollider(this);
        Collider.radius = _data.Collider.Radius;
        Collider.transform.position = _data.WorldPosition.D3_D2();
    }

    public override void Disable()
    {
        GameObject.Destroy(_gameObject.gameObject);
        Collider2DManager.instance.DeleteActorCollider(this);
        _gameObject = null;
    }

    public override bool ProcessActorMessage(ActorMessage message)
    {
        switch (message)
        {
            case AddDefaultDamageMessage addDefaultDamage:
                ServerWorldStates.currentWorld.Actors.TryGetValue(addDefaultDamage.TargetId, out var defender);
                CombatSystem.AttackWithDefaultDamage(this, addDefaultDamage.DefaultDamage, defender as CharacterBase);
                return true;
            default:
                break;
        }
        return false;
    }

    public override void PredictUpdate()
    {

    }

    public override void TriggerCheck()
    {
    }

    public override void Update()
    {
        _gameObject.transform.position = _data.WorldPosition;
        Collider.transform.position = _data.WorldPosition.D3_D2();
        base.Update();
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
    }

    public override bool AttackCheck(float damage, IAttacker attacker)
    {
        return _data.CharacterState.AttackCheck(this, damage, attacker);
    }

    public override float ApplyDamage(float damage, IAttacker attacker)
    {
        return _data.CharacterState.ApplyDamage(this, damage, attacker);
    }

    public override void PredicateUserInput(UserInput e)
    {

    }

    public override void DeletePredicted(long sequenceNumber)
    {

    }

    public override void EndOfFrame()
    {
        _gameObject.transform.position = _data.WorldPosition;
    }

    public void WallCheck()
    {
        _data.WorldPosition = Collider.transform.position.D2_D3();
    }
}