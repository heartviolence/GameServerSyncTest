
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Collider2DManager : MonoBehaviour
{
    public static Collider2DManager instance;

    [SerializeField]
    GameObject _circleColliderPrefab;

    List<IActorTrigger> _actorTrigger;
    public List<IActorCollider> _actorColliders;

    List<Collider2D> _collisionCheckCash;
    List<Vector3> _powerCash;

    private void Awake()
    {
        _actorTrigger = new();
        _actorColliders = new();
        _powerCash = new();
        _collisionCheckCash = new();
    }

    public void AddActorCollider(IActorCollider collider)
    {
        collider.Collider.transform.SetParent(this.transform);
        _actorColliders.Add(collider);
    }

    public void DeleteActorCollider(IActorCollider collider)
    {
        _actorColliders.Remove(collider);
        GameObject.Destroy(collider.Collider.gameObject);
    }

    public void AddActorTrigger(IActorTrigger trigger)
    {
        trigger.Trigger.transform.SetParent(this.transform);
        _actorTrigger.Add(trigger);
    }

    public void DeleteActorTrigger(IActorTrigger trigger)
    {
        _actorTrigger.Remove(trigger);
        GameObject.Destroy(trigger.Trigger.gameObject);
    }

    public CircleCollider2D Create_CircleCollider(long actorId, int layer)
    {
        var instance = GameObject.Instantiate(_circleColliderPrefab);
        instance.gameObject.name = actorId.ToString();
        instance.layer = layer;
        return instance.GetComponent<CircleCollider2D>();
    }

    public void Initialize_ActorCollider(IActorCollider actor)
    {
        var instance = GameObject.Instantiate(_circleColliderPrefab);
        instance.gameObject.name = actor.ActorId.ToString();
        actor.Collider = instance.GetComponent<CircleCollider2D>();
        actor.Collider.gameObject.layer = LayerMask.NameToLayer(LayerName.ActorCollider);
        AddActorCollider(actor);
    }

    public void ActorCollisionStep()
    {
        _powerCash.Clear();
        for (int i = 0; i < _actorColliders.Count; i++)
        {
            var c = _actorColliders[i];
            var actor = ServerWorldStates.currentWorld.FindActor(c.ActorId);
            Vector3 power = Vector3.zero;
            if (actor != null && actor.ActorData.Collider.Type == ColliderType.Movable)
            {
                power = CollisionCheck(c.Collider);
            }
            _powerCash.Add(power);
        }

        for (int i = 0; i < _powerCash.Count; i++)
        {
            _actorColliders[i].Collider.transform.position += _powerCash[i];
            _actorColliders[i].WallCheck();
        }
    }

    public int TriggerCheck(Collider2D trigger, List<long> results)
    {
        results.Clear();
        ContactFilter2D filter = new();
        filter.SetLayerMask(LayerMask.GetMask(LayerName.ActorCollider));
        var count = Physics2D.OverlapCollider(trigger, filter, _collisionCheckCash);

        for (int i = 0; i < count; i++)
        {
            if (long.TryParse(_collisionCheckCash[i].gameObject.name, out long id))
            {
                results.Add(id);
            }
        }
        results.Sort();
        return results.Count;
    }

    public Vector3 PredictCollisionCheck(CircleCollider2D collider)
    {
        return CollisionCheck(collider,false);
    }

    public Vector3 CollisionCheck(CircleCollider2D collider,bool pushOther = true)
    {
        ContactFilter2D filter = new();
        filter.SetLayerMask(LayerMask.GetMask(LayerName.ActorCollider));
        var count = Physics2D.OverlapCollider(collider, filter, _collisionCheckCash);
        var power = Vector3.zero;
        var a = collider;

        for (int i = 0; i < count; i++)
        {
            if (_collisionCheckCash[i] is CircleCollider2D b)
            {
                Vector3 bToa = a.transform.position - b.transform.position;
                bToa.z = 0;
                var distance = bToa.magnitude;
                var abRadius = a.radius + b.radius;

                if (distance < abRadius)
                {
                    long.TryParse(b.gameObject.name, out long bID);
                    var bType = ServerWorldStates.currentWorld.Actors[bID].ActorData.Collider.Type;
                    if (bType == ColliderType.Movable && pushOther == true)
                    {
                        //둘다 밀릴경우
                        power += bToa.normalized * (abRadius - distance) / 2f;
                    }
                    else
                    {
                        //A만 밀릴경우
                        power += bToa.normalized * (abRadius - distance);
                    }
                }
            }
        }
        return power;
    }

}