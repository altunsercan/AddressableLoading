using AssetLoading;
using AssetLoading.Installers;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zenject;

public class SamplePrefabAddressableInstaller : AddressablePreloadListInstaller<SamplePrefabAddressableInstaller>
{
    public AssetReferenceGameObject prefabToLoad;
	   
    protected override void InstallInternalBindings()
    {
        // GameObjectAssetLoader needs root to instantiate prefab in.
        Container.Bind<GameObject>().FromResolveGetter<Context>(context => context.gameObject);
        Container.Bind<GameObjectAssetLoader>().FromResolve(nameof(prefabToLoad));
        Container.BindInterfacesTo<SampleRotator>().AsSingle();
    }

    private class SampleRotator : IInitializable
    {
        private GameObjectAssetLoader loader;
        private GameObject loadedGameObject;

        public SampleRotator(GameObjectAssetLoader loader)
        {
            this.loader = loader;
        }
        
        public void Initialize()
        { 
            loadedGameObject = loader.LoadedAsset;
            
            loadedGameObject.transform.localPosition = new Vector3(0, 1, 0);
        }

    }
}
