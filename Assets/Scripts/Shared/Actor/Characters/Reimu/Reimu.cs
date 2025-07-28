
using LiteNetLib;
using LiteNetLib.Utils;
using log4net.Util;
using MessagePack;
using System.Collections.Generic;
using UnityEngine;


[MessagePackObject]
public class ReimuData : CharacterData
{
    [Key(9)]
    public Reimu.ReimuCharacterState CharacterState { get; set; } = new Reimu.ReimuIdle();

    public void Update(Reimu me)
    {
        base.Update();
        CharacterState.Update(me);
    }
}

[MessagePackObject(AllowPrivate = true)]
public partial class Reimu : CharacterBase, IActorCollider
{
    [Key(0)]
    private ReimuData _data;
    [IgnoreMember]
    public ReimuGameObject _gameObject;
    [IgnoreMember]
    public override CharacterData CharacterData => _data;
    [IgnoreMember]
    public override ActorData ActorData => _data;

    [IgnoreMember]
    public CircleCollider2D Collider { get; set; }

    [IgnoreMember]
    long IActorCollider.ActorId => _data.ActorId;

    public Reimu()
    {
        _data = new();
        _data.CurrentHP = _data.MaxHP = 100;
        _data.Collider.Radius = 0.5f;
        _data.Collider.Type = ColliderType.Movable;
    }

    public override void Update()
    {
        _data.Update(this);
    }

    public override void TriggerCheck()
    {

    }

    public override void AfterAddActor()
    {
        var prefab = ActorPreloader.Prefabs[nameof(ReimuGameObject)];
        _gameObject = GameObject.Instantiate(prefab).GetComponentInChildren<ReimuGameObject>();
        _gameObject.gameObject.name = _data.ActorId.ToString();
        Collider2DManager.instance.Initialize_ActorCollider(this);
        Collider.radius = _data.Collider.Radius;
        _gameObject.Collider2D = Collider;
        Collider.transform.position = _data.WorldPosition.D3_D2();
    }

    public override void Disable()
    {
        Collider2DManager.instance.DeleteActorCollider(this);
        GameObject.Destroy(_gameObject.gameObject);
        _gameObject = null;
    }

    public override void UpdateAnimation()
    {
        _gameObject.Update_();
    }

    public override void PredictUpdate()
    {        
        _data.CharacterState.SetStateToGameObject(this);
        _gameObject.Update_(false);
    }

    public override void PredicateUserInput(UserInput e)
    {
        var vector = GetDefaultMoveVectorD3(e.MoveInput, e.DeltaTime, 3);
        e.MoveVectorD3 = vector;
        _gameObject.Predicate(e);
        ClientBehaviour.instance.SendPacket(e, DeliveryMethod.Unreliable);
    }

    public override void ProcessEvent(ExternalWorldEvent e)
    {
        if (e is UserInput userInput)
        {
            _data.CharacterState.ProcessUserInput(userInput, this);
        }
    }

    private void DefaultMove(UserInput input)
    {
        if (!input.CanMove)
        {
            return;
        }

        var moveVector = GetDefaultMoveVectorD3(input.MoveInput.normalized, input.DeltaTime, 3);
        _data.WorldPosition = _gameObject.SetPosition(_data.WorldPosition + moveVector);
        _gameObject.WalkAnim(moveVector);
    }

    private void DefaultLook(UserInput input)
    {
        if (!input.CanLook)
        {
            return;
        }
        CharacterData.LookPoint = input.LookPoint;
        _gameObject.SetLookPoint(input.LookPoint);
    }

    public override void DeletePredicted(long sequenceNumber)
    {
        _gameObject.DeleteHistory(sequenceNumber);
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

    public override bool AttackCheck(float damage, IAttacker attacker)
    {
        return _data.CharacterState.AttackCheck(this, damage, attacker);
    }

    public override float ApplyDamage(float damage, IAttacker attacker)
    {
        return _data.CharacterState.ApplyDamage(this, damage, attacker);
    }

    public override void EndOfFrame()
    {
        _data.CharacterState.EndofFrame(this);
    }

    public void WallCheck()
    {
        var worldPos = Collider.transform.position.D2_D3(_data.WorldPosition.y);
        _data.WorldPosition = _gameObject.SetPosition(worldPos);
    }
}

