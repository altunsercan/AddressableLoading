namespace AssetLoading
{
    using System;
    using JetBrains.Annotations;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    
    public interface AssetLoader : IDisposable
    {
        void LoadAsset();
        bool IsReady { get; }
        event EventHandler AssetLoadedUntyped;
        
    }
    
    public class AssetLoader<TReferenceType, TReturnType> : AssetLoader where TReferenceType:UnityEngine.Object
    {
        protected readonly AssetReferenceT<TReferenceType> AssetReference;
        private AsyncOperationHandle<TReferenceType> goHandle;
        
        public event EventHandler AssetLoadedUntyped;
        public event EventHandler<TReturnType> AssetLoaded;

        public bool IsReady => goHandle.IsValid() && goHandle.Status != AsyncOperationStatus.None;

        public TReturnType LoadedAsset { get; protected set; }
        
        public AssetLoader(AssetReferenceT<TReferenceType> assetReference)
        {
            AssetReference = assetReference;
        }
        
        public async void LoadAsset()
        {
            goHandle = LoadAsync(AssetReference);
            
            if (goHandle.Task != null)
            {
                await goHandle.Task;
            }
            
            TReferenceType asset = goHandle.Result;

            if (asset == null || !TryConvertLoadedReferenceToReturnType(asset, out TReturnType convertedAsset))
            {
                return;
            }

            LoadedAsset = convertedAsset;
            
            Dispatch(convertedAsset);
        }
        
        protected virtual AsyncOperationHandle<TReferenceType> LoadAsync(AssetReferenceT<TReferenceType> assetReference)
        {
            return assetReference.LoadAssetAsync();
        }
        
        protected virtual bool TryConvertLoadedReferenceToReturnType(TReferenceType loadedResult, out TReturnType convertedResult)
        {
            if (loadedResult is TReturnType typedResult)
            {
                convertedResult = typedResult;
                return true;
            }

            Debug.LogError($"Custom asset loader {GetType().Name} does not have a overriden converter.");
            // ReSharper disable once AssignNullToNotNullAttribute
            convertedResult = default(TReturnType);
            return false;
        }

        protected virtual void Dispatch(TReturnType asset)
        {
            AssetLoadedUntyped?.Invoke(this, EventArgs.Empty);
            AssetLoaded?.Invoke(this, asset);   
        }
        
        public void Dispose()
        {
            Addressables.ReleaseInstance(goHandle);
            AssetLoaded = null;
        }
    }

    public class AssetLoader<T> : AssetLoader<T, T> where T:UnityEngine.Object
    {
        public AssetLoader(AssetReferenceT<T> assetReference) : base(assetReference){}
    }
    
    [UsedImplicitly]
    public class GameObjectAssetLoader : AssetLoader<GameObject>
    {       
        private readonly GameObject root;
        private AsyncOperationHandle<GameObject> goHandle;

        public GameObjectAssetLoader(GameObject root, AssetReferenceGameObject assetReference) : base(assetReference)
        {
            this.root = root;
        }

        protected override AsyncOperationHandle<GameObject> LoadAsync(AssetReferenceT<GameObject> assetReference)
        {
            return Addressables.InstantiateAsync(assetReference.RuntimeKey, this.root.transform, false, false);
        }
    }


    public class AnimationGameObjectAssetLoader : AssetLoader<GameObject, RuntimeAnimatorController>
    {
        public AnimationGameObjectAssetLoader(AssetReferenceT<GameObject> assetReference) : base(assetReference){}

        protected override bool TryConvertLoadedReferenceToReturnType(GameObject loadedResult, out RuntimeAnimatorController convertedResult)
        {
            RuntimeAnimatorController animator = loadedResult.GetComponent<Animator>()?.runtimeAnimatorController;
            // ReSharper disable once AssignNullToNotNullAttribute
            convertedResult = animator;
            return animator != null;
        }
    }
}