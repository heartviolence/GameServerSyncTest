using MessagePack;
using UnityEngine;

[MessagePack.Union(0, typeof(Reimu))]
[MessagePack.Union(1, typeof(Kakasi))]
[MessagePack.Union(2, typeof(Danmaku))]
[MessagePack.Union(3, typeof(HealOrb))]
public abstract class Actor
{
    [IgnoreMember]
    public abstract ActorData ActorData { get; }
    public virtual void AfterAddActor() { }
    public virtual void Update() { }
    public virtual bool ProcessActorMessage(ActorMessage message) { return false; }
    public virtual void ProcessEvent(ExternalWorldEvent e) { }

    public virtual void TriggerCheck() { }

    public virtual void LateUpdate() { }

    public virtual void UpdateAnimation() { }
    public abstract void Disable();

    public abstract void EndOfFrame();
}