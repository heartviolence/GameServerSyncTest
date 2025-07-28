
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ReimuGameObject : MonoBehaviour
{
    #region
    public abstract class ReimuGameObjectState
    {
        public float ElapsedTime { get; set; }

        public abstract void Update(ReimuGameObject me, float deltaTime);

        public abstract bool ActionA(ReimuGameObject me);

        public abstract bool Look(ReimuGameObject me, Vector3 lookPoint);

        public abstract bool Move(ReimuGameObject me, Vector3 v);

        public class StateIdle : ReimuGameObjectState
        {
            public override void Update(ReimuGameObject me, float deltaTime)
            {
                ElapsedTime += deltaTime;
            }

            public override bool ActionA(ReimuGameObject me)
            {
                me.CurrentState = new StateActionA();
                return true;
            }

            public override bool Move(ReimuGameObject me, Vector3 v)
            {
                return true;
            }

            public override bool Look(ReimuGameObject me, Vector3 lookPoint)
            {
                return me.SetLookPoint(lookPoint);
            }
        }

        public class StateActionA : ReimuGameObjectState
        {
            public override void Update(ReimuGameObject me, float deltaTime)
            {
                ElapsedTime += deltaTime;
            }

            public override bool ActionA(ReimuGameObject me)
            {
                return false;
            }
            public override bool Move(ReimuGameObject me, Vector3 v)
            {
                return false;
            }
            public override bool Look(ReimuGameObject me, Vector3 lookPoint)
            {
                return me.SetLookPoint(lookPoint);
            }
        }
    }
    #endregion


    enum WalkAnimState
    {
        Idle,
        WalkF,
    }
    List<(float timeStamp, Vector3 position)> _positions = new();
    WalkAnimState _walkState = WalkAnimState.Idle;
    Animator _animator;

    public CircleCollider2D Collider2D { get; set; }
    public ReimuGameObjectState CurrentState { get; set; }
    List<UserInput> _inputHistory;
    NavMeshAgent _agent;
    List<CharacterActionCode> _actionCodeCash = new();
    Vector3 _lookPoint;
    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        CurrentState = new ReimuGameObjectState.StateIdle();
        _agent = GetComponentInChildren<NavMeshAgent>();
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _inputHistory = new();
    }
    public void Update_(bool interpolate = true)
    {
        if (interpolate)
        {
            CurrentState.Update(this, Time.deltaTime);
            InterpolatePosition();
        }
        else
        {
            PredictUpdate();
        }
        transform.forward = (_lookPoint - transform.position).X0Z().normalized;
    }
    public Vector3 SetPosition(Vector3 position)
    {
        transform.position = WallCheck(position);
        Collider2D.transform.position = transform.position.D3_D2();
        return transform.position;
    }

    public Vector3 SetInterpolatePosition(float timeStamp, Vector3 position)
    {
        var finalPos = SetPosition(position);
        _positions.Add(new(timeStamp, finalPos));
        return finalPos;
    }

    void PredictUpdate()
    {
        Vector3 lastVector = Vector3.zero;

        foreach (var input in _inputHistory)
        {
            CurrentState.Update(this, input.DeltaTime);
            if (input.CanMove)
            {
                Move(input.MoveVectorD3);
                lastVector = input.MoveVectorD3;
            }
            if (input.CanLook)
            {
                SetLookPoint(input.LookPoint);
            }

            foreach (var code in input.ActionCodes)
            {
                switch (code)
                {
                    case CharacterActionCode.A:
                        CurrentState.ActionA(this);
                        break;
                    default:
                        break;
                }
            }
        }

        WalkAnim(lastVector);
    }

    void Move(Vector3 v)
    {
        _agent.nextPosition = (transform.position + v);
        Collider2D.transform.position = _agent.nextPosition.D3_D2();
        var power = Collider2DManager.instance.PredictCollisionCheck(Collider2D);
        Collider2D.transform.position += power;
        _agent.nextPosition = Collider2D.transform.position.D2_D3();
        transform.position = _agent.nextPosition;
    }


    public Vector3 WallCheck(Vector3 WorldPos)
    {
        _agent.nextPosition = WorldPos;
        return _agent.nextPosition;
    }

    #region Predict

    public void Predicate(UserInput userInput)
    {
        if (CurrentState.Move(this, userInput.MoveInput))
        {
            userInput.CanMove = true;
        }

        if (CurrentState.Look(this, userInput.LookPoint))
        {
            userInput.CanLook = true;
        }

        _actionCodeCash.Clear();
        foreach (var code in userInput.ActionCodes)
        {
            switch (code)
            {
                case CharacterActionCode.A:
                    if (CurrentState.ActionA(this))
                    {
                        _actionCodeCash.Add(code);
                    }
                    break;
                default:
                    break;
            }
        }
        userInput.ActionCodes.Clear();
        userInput.ActionCodes.AddRange(_actionCodeCash);

        _inputHistory.Add(userInput);
    }

    public bool SetLookPoint(Vector3 lookPoint)
    {
        this._lookPoint = lookPoint;
        return true;
    }

    public void DeleteHistory(long sequenceNumber)
    {
        _inputHistory.RemoveAll(e => e.InputSequenceNumber <= sequenceNumber);
    }

    #endregion


    public void WalkAnim(Vector3 walkVector)
    {
        WalkAnimState nextState = WalkAnimState.Idle;

        if (walkVector.sqrMagnitude > 0)
        {
            nextState = WalkAnimState.WalkF;
        }

        if (_walkState != nextState)
        {
            _walkState = nextState;
            switch (_walkState)
            {
                case WalkAnimState.Idle:
                    _animator.CrossFadeInFixedTime("Base Layer.WAIT01", 0.1f, 0, 0);
                    break;
                case WalkAnimState.WalkF:
                    _animator.CrossFadeInFixedTime("Base Layer.WALK00_F", 0.1f, 0, 0);
                    break;
            }
        }
    }

    void InterpolatePosition()
    {
        if (_positions.Count == 0)
        {
            return;
        }
        if (_positions.Count == 1)
        {
            transform.position = _positions[0].position;
            return;
        }
        var renterTime = Time.time - (10f / 60f) - 0.1f;

        while (_positions.Count > 2)
        {
            if (_positions[1].timeStamp < renterTime)
            {
                _positions.RemoveAt(0);
            }
            else
            {
                break;
            }
        }

        var t0 = _positions[0].timeStamp;
        var t1 = _positions[1].timeStamp;
        var lerpTime = (renterTime - t0) / (t1 - t0);

        var nextPos = Vector3.Lerp(_positions[0].position, _positions[1].position, lerpTime);
        transform.position = nextPos;

        var walkDirection = (_positions[1].position - _positions[0].position).D3_D2();
    }


}
