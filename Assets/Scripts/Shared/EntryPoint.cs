
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer.Unity;

public class EntryPoint : IAsyncStartable, IDisposable
{
    ActorPreloader _actorPreloader;
    MyCharacterController _controller1;
    MyCharacterController _controller2;
    WorldReference _box;

    CancellationTokenSource _UpdateLoopCancellationTokenSource = new();
    public EntryPoint(
        ActorPreloader loader,
        WorldReference box)
    {
        this._actorPreloader = loader;
        this._box = box;

    }

    public void Dispose()
    {
        _UpdateLoopCancellationTokenSource.Cancel();
        _UpdateLoopCancellationTokenSource.Dispose();
    }

    public async UniTask StartAsync(CancellationToken cancellation = default)
    {
        Application.targetFrameRate = 60;
        Physics2D.autoSyncTransforms = true;
        _box.s.Initialize();
        _box.c1.Initialize();
        _box.c2.Initialize();
        await _actorPreloader.PreloadMapObjects();
        await _actorPreloader.PreloadReimu();
        await _actorPreloader.PredloadKaksi();

        _controller1 = new MyCharacterController(_box.c1.World, _box.c1);
        _controller2 = new MyCharacterController(_box.c2.World, _box.c2);

        _controller1.Initialize();
        _controller2.Initialize();

        Update().Forget();
    }

    public async UniTask Update()
    {
        await foreach (var _ in UniTaskAsyncEnumerable.EveryUpdate().WithCancellation(_UpdateLoopCancellationTokenSource.Token))
        {
            SwitchServer();
            _box.s.Update_();
            SwitchClient1();
            _box.c1.Update_();

            _controller1.Update();
            _box.c1.World.UpdateAnimation(_controller1.GetControlActorId());
            ClientBehaviour.instance = _box.c1;
            _controller1.UpdateControlTarget();

            SwitchClient2();
            _box.c2.Update_();
            _controller2.Update();
            _box.c2.World.UpdateAnimation(_controller2.GetControlActorId());
            ClientBehaviour.instance = _box.c2;
            _controller2.UpdateControlTarget();
        }
    }
    void SwitchServer()
    {
        LayerName.ActorCollider = LayerName.ServerActorCollider;
        LayerName.ActorTrigger = LayerName.ServerActorTrigger;
        Collider2DManager.instance = _box.serverCollider;
    }

    void SwitchClient1()
    {
        LayerName.ActorCollider = LayerName.ClientActorCollider;
        LayerName.ActorTrigger = LayerName.ClientActorTrigger;
        Collider2DManager.instance = _box.clientCollider;
    }

    void SwitchClient2()
    {
        LayerName.ActorCollider = LayerName.ClientActorCollider2;
        LayerName.ActorTrigger = LayerName.ClientActorTrigger2;
        Collider2DManager.instance = _box.clientCollider2;
    }
}