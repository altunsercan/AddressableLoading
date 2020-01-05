namespace AssetLoading.Installers
{
    using System;
    using UnityEngine;
    using Zenject;

    public class AddressablePreloadingKernelInstaller : Installer< AddressablePreloadingKernelInstaller>
    {
        [Inject] private Context context;
        
        public override void InstallBindings()
        {
            if (Container.HasBindingId<AddressablePreloaderKernel>(context))
            {
                // there is already a preloading kernel. Skip
                return;
            }else if (Container.HasBinding<AddressablePreloader>())
            {
                // remove parent context's binding
                Container.Unbind<AddressablePreloader>();
            }

            (Type rebindType, Type concreteKernelType) = GetKernelType();
            if (rebindType == null || concreteKernelType == null)
            {
                Debug.Log($"{nameof(AddressablePreloadingKernelInstaller)} failed to find kernel to replace");
                return;
            }

            Container.Rebind<AddressablePreloader>().AsCached();
            
            Container.Unbind(rebindType, rebindType);
            Container.Unbind(typeof(MonoKernel), rebindType);
            Container.Bind(rebindType, typeof(MonoKernel))
                .To(concreteKernelType).FromNewComponentOn(_ =>
                {
                    var ctx = _.Container.Resolve<Context>();
                    return ctx.gameObject;
                })
                .AsCached().NonLazy();
            Container.Bind<MonoKernel>().WithId(context).FromResolve().AsCached();
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