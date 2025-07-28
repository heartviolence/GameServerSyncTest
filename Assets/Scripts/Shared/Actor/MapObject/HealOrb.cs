
using MessagePack;
using UnityEngine;

[MessagePackObject]
public class HealOrb : CharacterBase, IActorCollider
{
    [Key(0)]
    public CharacterData _data;

    [IgnoreMember]
    public HealOrbGameObject _gameObject;

    [IgnoreMember]
    public override CharacterData CharacterData => _data;

    [IgnoreMember]
    public override ActorData ActorData => _data;

    [IgnoreMember]
    public CircleCollider2D Collider { get; set; }

    [IgnoreMember]

    public long ActorId => _data.ActorId;

    public HealOrb()
    {
        _data = new CharacterData();
        _data.CurrentHP = _data.MaxHP = 100;
        _data.Collider.Radius = 0.5f;
        _data.Collider.Type = ColliderType.NonMovable;
    }

    public override void Update()
    {
        _gameObject.transform.position = _data.WorldPosition;
        Collider.transform.position = _data.WorldPosition.D3_D2();
    }

    public override void AfterAddActor()
    {
        var prefab = ActorPreloader.Prefabs[nameof(HealOrbGameObject)];
        _gameObject = GameObject.Instantiate(prefab).GetComponentInChildren<HealOrbGameObject>();
        _gameObject.gameObject.name = _data.ActorId.ToString();
        Collider2DManager.instance.Initialize_ActorCollider(this);
        Collider.radius = _data.Collider.Radius;
    }

    public override float ApplyDamage(float damage, IAttacker attacker)
    {
        float beforeHP = _data.CurrentHP;
        _data.CurrentHP = Mathf.Clamp(_data.CurrentHP - damage, 0, _data.MaxHP); ;
        if (_data.CurrentHP == 0)
        {
            _data.Enable = false;
            ServerWorldStates.currentWorld.RegisterDisableActor(_data.ActorId);
            if (ServerWorldStates.currentWorld.Actors.TryGetValue(attacker.OwnerId, out var owner))
            {
                if (owner is CharacterBase character)
                {
                    CombatSystem.Heal(this, 50, character);
                }
            }
        }

        return beforeHP - _data.CurrentHP;
    }

    public override bool AttackCheck(float damage, IAttacker attacker)
    {
        return true;
    }

    public override void DeletePredicted(long sequenceNumber)
    {
        return;
    }

    public override void PredictUpdate()
    {
        return;
    }

    public override void PredicateUserInput(UserInput e)
    {
        return;
    }

    public override void TriggerCheck()
    {

    }

    public override void Disable()
    {
        GameObject.Destroy(_gameObject.gameObject);
        Collider2DManager.instance.DeleteActorCollider(this);
        _gameObject = null;
    }

    public override void EndOfFrame()
    {
        _gameObject.transform.position = _data.WorldPosition;
    }

    public void WallCheck()
    {
        _data.WorldPosition = Collider.transform.position.D2_D3(0.5f);
    }
}