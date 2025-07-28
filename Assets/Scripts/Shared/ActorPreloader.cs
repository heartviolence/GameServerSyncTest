
using Cysharp.Threading.Tasks;
using LiteNetLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ActorPreloader
{
    public static Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
    readonly ServerWorldStates _worldStates;
    readonly WorldReference b;
    public ActorPreloader(ServerWorldStates worldStates, WorldReference b)
    {
        this._worldStates = worldStates;
        this.b = b;
    }

    public async UniTask PreloadMapObjects()
    {
        Prefabs[nameof(HealOrbGameObject)] = await Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/healOrb.prefab");
    }     

    public async UniTask PreloadReimu()
    {
        Prefabs[nameof(ReimuGameObject)] = await Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/reimu.prefab");
        Prefabs[nameof(DanmakuGameObject)] = await Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Danmaku.prefab");
    }

    public async UniTask PredloadKaksi()
    {
        Prefabs[nameof(KakasiGameObject)] = await Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/kakasi.prefab");
    }
}
