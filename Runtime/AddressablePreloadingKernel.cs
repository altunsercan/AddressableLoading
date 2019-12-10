namespace AssetLoading
{
    using System;
    using Zenject;

    public interface AddressablePreloaderKernel{}
    
    public class AddressablePreloaderSceneKernel : SceneKernel, AddressablePreloaderKernel
    {
        [InjectLocal]
        private AddressablePreloader _preloader;
        
        public override void Start()
        {
            _preloader.PreloadCompleted += OnPreloadCompleted;
            _preloader.StartPreloading();
        }

        private void OnPreloadCompleted(object sender, EventArgs e)
        {
            _preloader.PreloadCompleted -= OnPreloadCompleted;
            base.Initialize();
        }
    }
    
    public class AddressablePreloaderProjectKernel : ProjectKernel, AddressablePreloaderKernel
    {
        [InjectLocal]
        private AddressablePreloader _preloader;
        
        public override void Start()
        {
            _preloader.PreloadCompleted += OnPreloadCompleted;
            _preloader.StartPreloading();
        }

        private void OnPreloadCompleted(object sender, EventArgs e)
        {
            _preloader.PreloadCompleted -= OnPreloadCompleted;
            base.Initialize();
        }
    }
    public class AddressablePreloaderGameObjectKernel : DefaultGameObjectKernel, AddressablePreloaderKernel
    {
        [InjectLocal]
        private AddressablePreloader _preloader;
        
        public override void Start()
        {
            _preloader.PreloadCompleted += OnPreloadCompleted;
            _preloader.StartPreloading();
        }

        private void OnPreloadCompleted(object sender, EventArgs e)
        {
            _preloader.PreloadCompleted -= OnPreloadCompleted;
            base.Initialize();
        }
    }
}