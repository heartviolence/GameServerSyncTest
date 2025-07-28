

using MessagePack;
using UnityEditor.MPE;
using UnityEngine;

public partial class Reimu
{
    [MessagePack.Union(0, typeof(ReimuIdle))]
    [MessagePack.Union(1, typeof(ReimuDefaultAttack))]
    public abstract class ReimuCharacterState
    {
        [Key(0)]
        public float ElapsedTime { get; set; }

        public virtual void Update(Reimu me)
        {
            ElapsedTime += WorldTime.DeltaTime;
            me._data.WorldPosition = me._gameObject.SetPosition(me._data.WorldPosition);
        }

        public void EndofFrame(Reimu me)
        {
            me._data.WorldPosition = me._gameObject.SetInterpolatePosition(WorldTime.Time, me._data.WorldPosition);
        }

        public virtual bool AttackCheck(Reimu me, float damage, IAttacker attacker)
        {
            return true;
        }

        public virtual float ApplyDamage(Reimu me, float damage, IAttacker attacker)
        {
            float beforeHP = me._data.CurrentHP;
            me._data.CurrentHP = Mathf.Clamp(me._data.CurrentHP - damage, 0, me._data.MaxHP); ;
            if (me._data.CurrentHP == 0)
            {
                me._data.Enable = false;
                ServerWorldStates.currentWorld.RegisterDisableActor(me._data.ActorId);
            }

            return beforeHP - me._data.CurrentHP;
        }

        public abstract void ProcessUserInput(UserInput input, Reimu me);
        public abstract void SetStateToGameObject(Reimu me);
    }

    [MessagePackObject]
    public class ReimuIdle : ReimuCharacterState
    {
        public void Start(Reimu me)
        {
            me._gameObject.CurrentState = new ReimuGameObject.ReimuGameObjectState.StateIdle();
        }

        public override void ProcessUserInput(UserInput input, Reimu me)
        {
            me.DefaultMove(input);
            me.DefaultLook(input);

            foreach (var code in input.ActionCodes)
            {
                switch (code)
                {
                    case CharacterActionCode.A:
                        var nextState = new ReimuDefaultAttack();
                        me._data.CharacterState = nextState;
                        nextState.Start(me);
                        return;
                    default:
                        break;
                }
            }
        }
        public override void SetStateToGameObject(Reimu me)
        {
            me._gameObject.SetPosition(me._data.WorldPosition);
            me._gameObject.SetLookPoint(me._data.LookPoint);
            me._gameObject.CurrentState = new ReimuGameObject.ReimuGameObjectState.StateIdle() { ElapsedTime = this.ElapsedTime };
        }
    }

    [MessagePackObject]
    public class ReimuDefaultAttack : ReimuCharacterState
    {

        const float maxStateTime = 0.5f;
        public void Start(Reimu me)
        {
            me._gameObject.CurrentState = new ReimuGameObject.ReimuGameObjectState.StateActionA();
        }

        public override void Update(Reimu me)
        {
            base.Update(me);
            if (ElapsedTime > maxStateTime)
            {
                Shoot(me);

                var nextState = new ReimuIdle();
                me._data.CharacterState = nextState;
                nextState.Start(me);
            }
        }

        public override void ProcessUserInput(UserInput input, Reimu me)
        {
            me.DefaultLook(input);

            foreach (var code in input.ActionCodes)
            {
                switch (code)
                {
                    case CharacterActionCode.A:
                        break;
                    default:
                        break;
                }
            }
        }

        public override void SetStateToGameObject(Reimu me)
        {
            me._gameObject.SetPosition(me._data.WorldPosition);
            me._gameObject.SetLookPoint(me._data.LookPoint);
            me._gameObject.CurrentState = new ReimuGameObject.ReimuGameObjectState.StateActionA() { ElapsedTime = this.ElapsedTime };
        }

        public void Shoot(Reimu me)
        {
            var CharacterPos2D = me._data.WorldPosition.D3_D2();
            var forward = (me._data.LookPoint.D3_D2() - CharacterPos2D).normalized;
            var destination = CharacterPos2D + forward * 10;
            var danmaku = new Danmaku();
            danmaku.OwnerId = me._data.ActorId;
            danmaku.MoveTo(CharacterPos2D, 4f, destination);
            ServerWorldStates.currentWorld.RegisterActor(danmaku);
        }
    }
}