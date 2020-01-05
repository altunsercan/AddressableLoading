namespace AssetLoading.Installers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using Zenject;

    public abstract class BaseAddressablePreloadListInstaller<T> : ScriptableObjectInstaller<T> where T : BaseAddressablePreloadListInstaller<T>
    {
        protected static readonly List<(string, AssetReference)> FillList = new List<(string, AssetReference)>();
    }
    
    public abstract class AddressablePreloadListInstaller<T> : BaseAddressablePreloadListInstaller<T> where T: AddressablePreloadListInstaller<T>
    {
        public AddressablePreloadListInstaller()
        {   
            if (_cachedReflectedAccessors == null)
            {
                GenerateCachedAccessors();
            }
        }

        public void GetAssetReferenceList(List<(string, AssetReference)> referenceListToFill)
        {
            if (_cachedReflectedAccessors == null)
            {
                Debug.LogError($"{GetType().Name} accessors are not reflected.");
                return;
            }
            
            foreach (KeyValuePair<string, Func<AddressablePreloadListInstaller<T>, AssetReference>> pair in _cachedReflectedAccessors)
            {
                AssetReference address = pair.Value?.Invoke(this);
                referenceListToFill.Add((pair.Key, address));
            }
        }

        public override sealed void InstallBindings()
        {   
            // Update kernel
            AddressablePreloadingKernelInstaller.Install(Container);
            
            try
            {
                GetAssetReferenceList(FillList);

                foreach ((string, AssetReference) tuple in FillList)
                {
                    string key = tuple.Item1;
                    AssetReference untypedReference = tuple.Item2;

                    if (untypedReference is AssetReferenceGameObject gameObjectReference)
                    {
                        BindReference(key, gameObjectReference);
                    }
                    else
                    {
                        BindReference(key, untypedReference);
                    }
                }
            }
            finally
            {
                FillList.Clear();
            }
            
            InstallInternalBindings();
        }

        protected virtual void InstallInternalBindings(){}

        private void BindReference(string key, AssetReference untypedReference)
        {
            var type = untypedReference.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AssetReferenceT<>))
            {
                Type assetType = type.GetGenericArguments()[0];

                // TODO: Cache this globally since same typed loaders may be used in multiple preloaders
                var bindMethod = GetType().GetMethod("BindGenericReference", BindingFlags.NonPublic | BindingFlags.Instance)?
                    .MakeGenericMethod(assetType);

                if (bindMethod != null)
                {
                    // TODO: potentially unsafe invoke should be inside try catch
                    bindMethod.Invoke(this, new object[] {untypedReference});
                }
            }
        }

        private void BindGenericReference<T>(string key, AssetReferenceT<T> typedReference) where T:UnityEngine.Object
        {
            Container.Bind<AssetLoader<T>>().WithId(key).AsCached().WithArguments(typedReference)
                .OnInstantiated<AssetLoader<T>>(RegisterAssetLoader).NonLazy();
        }

        private void BindReference(string key, AssetReferenceGameObject gameObjectReference)
        {
            Container.Bind<GameObjectAssetLoader>().WithId(key).AsCached().WithArguments(gameObjectReference)
                .OnInstantiated<GameObjectAssetLoader>(RegisterAssetLoader).NonLazy();
            
        }

        private void RegisterAssetLoader(InjectContext injectContext, AssetLoader assetLoader)
        {
            var preloader = injectContext.Container.Resolve<AddressablePreloader>();
            preloader.AddAssetLoader(assetLoader);
        }

        #region Static Reflection
        private static Dictionary<string, Func<AddressablePreloadListInstaller<T>, AssetReference>> _cachedReflectedAccessors;
        private static void GenerateCachedAccessors()
        {
            _cachedReflectedAccessors = new Dictionary<string, Func<AddressablePreloadListInstaller<T>, AssetReference>>();

            var members = typeof(T).GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public |
                                               BindingFlags.NonPublic);
            foreach (MemberInfo memberInfo in members)
            {
                if (TryGetAccessor(memberInfo, out Func<AddressablePreloadListInstaller<T>, AssetReference> accessor))
                {
                    _cachedReflectedAccessors.Add(memberInfo.Name, accessor);
                }
            }
        }
        
        private static bool TryGetAccessor(MemberInfo memberInfo, out Func<AddressablePreloadListInstaller<T>, AssetReference> accessor)
        {
            if (memberInfo is FieldInfo fieldInfo && typeof(AssetReference).IsAssignableFrom(fieldInfo.FieldType))
            {
                accessor = instance => (AssetReference) fieldInfo.GetValue(instance);
                return true;
            }

            if (memberInfo is PropertyInfo propertyInfo && typeof(AssetReference).IsAssignableFrom(propertyInfo.PropertyType))
            {
                accessor = instance => (AssetReference) propertyInfo.GetValue(instance);
                return true;
            }

            // We don't care about other types of members
            accessor = null;
            return false;
        }
        #endregion Static Reflection
        
    }
}