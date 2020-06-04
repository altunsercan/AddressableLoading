This unity package is designed to be used with Extenject (Zenject) to install Addressable assets as preload dependencies for runnable Contexts (ProjectContext, SceneContext, GameObjectContext). This is not an one size fits all solution to using Addressables with Extenject, as the way addressables are managed may differ between different projects and architectures. 

I am marely providing my personal approach after responding a lot of questions on the subject in different online chat rooms. This is not an officialy supported library. I will be working on a generalized Asyc support for Extenject that will cover working with addressables too. Hopefully will be in production ready quality to be merged into Extenject itself.

### Problem
Addressables introduce added complexity to object initialization in Unity, since its api is designed to support both offline and online resolution of asset dependencies. Addressable API is designed as async. 

Extenject's Kernel features like initialization management through `IInitializable` and update loop alternative `ITickable` assume dependencies can be resolved syncroniously at initialization of an object. If an object depends on addressable assets; either another manager should preload the asset before creating the object or the object code itself should manage availability and async loading of the resource.

### Solution
In order to provide a generalized solution to using Addressables, this package provides two installers.  `AddressablePreloadingKernelInstaller` replaces existing kernel in a context with preloading variant. This variant does not start trigger initialization manager or start tick manager until all required addressables are loaded. Second installer `AddressablePreloadListInstaller<T>` is an abstract ScriptableObject installer that defines these preload dependencies. 

### How to use
Extend from `AddressablePreloadListInstaller<T>` to create your own ScriptableObjectInstaller as shown below.

```csharp

	public class MyAddressableInstaller : AddressablePreloadListInstaller<MyAddressableInstaller>
    {
        public AssetReferenceGameObject myPrefab;
        public AssetReferenceT<Material> myMaterial
	   
        protected override void InstallInternalBindings()
        {
			// This is replacement for InstallBindings since the original method call is sealed to avoid misconfiguration.
        }
    }

```
Then install this scriptable object  drag & dropping into a context. Alternatively if  you want to load it from resources you can use following inside another installer.
```csharp
MyAddressableInstaller.InstallFromResource("path-to-resource", Container);
```
And thats it. The scriptable object installer will automatically use kernel replacement installer (if not already installed on this context). It will then use reflection to discover AssetReferences and bind them for preloading.

You can access these preloaded assets via AssetLoader implementation. Installer automatically binds these AssetLoaders with variable name found in reflection.
```csharp
Container.Bind<GameObjectAssetLoader>().FromResolve("myPrefab");
Container.Bind<AssetLoader<Material>>().FromResolve("myMaterial");

GameObjectAssetLoader prefabLoader;
prefabLoader.LoadedAsset; // Instance of preloaded asset.

AssetLoader<Material> materialLoader;
materialLoader.LoadedAsset; // Instance of the material
```


### Discussion
#### Can I use preloading without AddressablePreloadListInstaller?
Yes. `AddressablePreloadListInstaller` was a convention I used to reduce writing boilerplate AssetLoaderBindings.  Instead you can install the kernel replacement yourself and manually bind the AssetLoaders.

```csharp
public 	GameObjectAssetReference assetReference;

public override void InstallBindings()
{   
	AddressablePreloadingKernelInstaller.Install(Container);
	Container.Bind<GameObjectAssetLoader>().AsCached().WithArguments(assetReference)
					.OnInstantiated<GameObjectAssetLoader>(RegisterAssetLoader).NonLazy();
}

private void RegisterAssetLoader(InjectContext injectContext, AssetLoader assetLoader)
{
	var preloader = injectContext.Container.Resolve<AddressablePreloader>();
	preloader.AddAssetLoader(assetLoader);
}
```

OnInstantiated and NonLazy is necessary for loader to be created and registered to preloader for the kernel.

#### Why are you binding AssetLoaders with ID?
Another choice of convention. Assets have to be accessesed somehow but there may be multiple assets of same type (like multiple prefab) that are registered via `AddressablePreloadListInstaller`

#### Are AssetLoaders from parent container accessible from children. 
Yes. Since AssetLoaders are bound to context all children can access those preloaded addressables. However, if your child container has preload installer of its own, the preloader will be Rebinded so the parent context's assets will not be reloaded multiple times. One caveat is that if you have asset with same id of parent container you may need to unbind the parent version in child container.

