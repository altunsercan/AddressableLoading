namespace AssetLoading.Installers
{
    using System;
    using UnityEngine;
    using Zenject;

    public class AddressablePreloadingKernelInstaller : Installer<AddressablePreloadingKernelInstaller>
    {   
        public override void InstallBindings()
        {
            if (Container.HasBinding<AddressablePreloaderKernel>())
            {
                // there is already a preloading kernel. Skip
                return;
            }

            (Type rebindType, Type concreteKernelType) = GetKernelType();
            if (rebindType == null || concreteKernelType == null)
            {
                Debug.Log($"{nameof(AddressablePreloadingKernelInstaller)} failed to find kernel to replace");
                return;
            }

            Container.Bind<AddressablePreloader>().AsCached();

            Container.Unbind(rebindType, rebindType);
            Container.Unbind(typeof(MonoKernel), rebindType);
            Container.Bind(rebindType, typeof(MonoKernel), typeof(AddressablePreloaderKernel))
                .To(concreteKernelType).FromNewComponentOn(_=>_.Container.DefaultParent.gameObject)
                .AsCached().NonLazy();
        }

        private (Type rebindType, Type concreteKernelType) GetKernelType()
        {   
            if (Container.HasBinding<GameObjectContext>())
            {
                return (typeof(DefaultGameObjectKernel), typeof(AddressablePreloaderGameObjectKernel));
            }
            else if (Container.HasBinding<SceneContext>())
            {
                return (typeof(SceneKernel), typeof(AddressablePreloaderSceneKernel));
            }else if (Container.HasBinding<ProjectContext>())
            {
                return (typeof(ProjectKernel), typeof(AddressablePreloaderProjectKernel));
            }
            else
            {
                // TODO: Handle cases where default GameObjectKernel is replaced with custom kernel
            }
            return (null, null);
        }
    }
}