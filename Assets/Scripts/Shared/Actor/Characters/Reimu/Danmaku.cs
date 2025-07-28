
 
using MessagePack; 
using System.Collections.Generic;
using UnityEngine;

[MessagePackObject]
public class DanmakuData : ActorData
{
    [Key(5)]
    public float EndTime { get; set; }
    [Key(6)]
    public float ElapsedTime { get; set; }
    [Key(7)]
    public Vector3 StartPos { get; set; } // 2d기준
    [Key(8)]
    public Vector3 Vector { get; set; } // 2d기준
}

[MessagePackObject(AllowPrivate = true)]
public partial class Danmaku : Actor, IAttacker, IActorTrigger
{
    [Key(1)]
    DanmakuData _data;

    [IgnoreMember]
    DanmakuGameObject _gameObject;

    [Key(2)]
    public long OwnerId { get; set; }

    public override ActorData ActorData => _data;

    [IgnoreMember]
    long IActorTrigger.ActorId => _data.ActorId;

    [IgnoreMember]
    Collider2D IActorTrigger.Trigger { get => _collider; }

    [IgnoreMember]
    CircleCollider2D _collider;

    [IgnoreMember]
    List<long> _triggerCheckCash = new();

    public Danmaku()
    {
        _data = new();
        _data.Collider.Radius = 0.3f;
    }

    public override void AfterAddActor()
    {
        var prefab = ActorPreloader.Prefabs[nameof(DanmakuGameObject)];
        var instance = GameObject.Instantiate(prefab);
        _gameObject = instance.GetComponentInChildren<DanmakuGameObject>();
        _gameObject.gameObject.name = _data.ActorId.ToString();
        _gameObject.transform.position = _data.WorldPosition.D2_D3(0.5f);
        _collider = Collider2DManager.instance.Create_CircleCollider(_data.ActorId, LayerMask.NameToLayer(LayerName.ActorTrigger));
        _collider.radius = _data.Collider.Radius;
        Collider2DManager.instance.AddActorTrigger(this);
        _collider.transform.position = _data.WorldPosition.D3_D2();
    }

    public void MoveTo(Vector3 startPos, Vector3 vector, float endTime)
    {
        _data.StartPos = startPos;
        _data.Vector = vector;
        _data.EndTime = endTime;
        _data.ElapsedTime = 0;
        _data.WorldPosition = startPos;
    }

    public void MoveTo(Vector3 startPos, float speed, Vector3 destination)
    {
        var v = (destination - startPos);
        _data.StartPos = startPos;
        _data.EndTime = v.magnitude / speed;
        _data.Vector = v / _data.EndTime;
        _data.ElapsedTime = 0;
        _data.WorldPosition = startPos;
    }

    public override void Disable()
    {
        GameObject.Destroy(_gameObject.gameObject);
        Collider2DManager.instance.DeleteActorTrigger(this);
        _gameObject = null;
    }

    public float GetBaseDamage()
    {
        return 20;
    }

    public override void Update()
    {
        if (!_data.Enable)
        {
            return;
        }

        _data.ElapsedTime += WorldTime.DeltaTime;

        if (_data.ElapsedTime >= _data.EndTime)
        {
            _data.ElapsedTime = _data.EndTime;
            _data.Enable = false;
            ServerWorldStates.currentWorld.RegisterDisableActor(_data.ActorId);
        }

        _data.WorldPosition = (_data.StartPos + (_data.Vector * _data.ElapsedTime)).D2_D3(0.5f);
        _collider.transform.position = _data.WorldPosition.D3_D2();
        _gameObject.SetPosition(_data.WorldPosition);
    }

    public override void ProcessEvent(ExternalWorldEvent e)
    {

    }

    public override void TriggerCheck()
    {
        if (!_data.Enable)
        {
            return;
        }

        var count = Collider2DManager.instance.TriggerCheck(_collider, _triggerCheckCash);
        for (int i = 0; i < count; i++)
        {
            var actorId = _triggerCheckCash[i];
            if (actorId != OwnerId &&
                ServerWorldStates.currentWorld.FindActor(actorId) is CharacterBase character)
            {
                if (CombatSystem.Attack(this, character))
                {
                    _data.Enable = false;
                    _gameObject.gameObject.SetActive(false);
                    ServerWorldStates.currentWorld.RegisterDisableActor(_data.ActorId);
                }
            }
        }
    }

    public override void LateUpdate()
    {

    }

    public void AfterApplyDamage(float finalHitDamage, CharacterBase defender)
    {
        if (finalHitDamage >= 20)
        {
            var dotEffect = new TickMessageEffect()
            {
                MaxDuration = 3,
                MessageInterval = 0.5f,
                Message = new AddDefaultDamageMessage() { MessageProcessorId = OwnerId, TargetId = defender.CharacterData.ActorId, DefaultDamage = 5 }
            };
            var debuff = new CharacterBuff() { MaxDuration = 3 };
            debuff.TickMessageEffects.Add(dotEffect);
            defender.CharacterData.Buffs.Add(debuff);
        }
    }

    public override void EndOfFrame()
    {

    }
}