namespace AssetLoading
{
    using System;
    using System.Collections.Generic;

    public class AddressablePreloader
    {
        private bool initialized = false;
        private readonly List<AssetLoader> loaderList;
        private int waitCounter = 0;

        public event EventHandler PreloadCompleted;
        
        public AddressablePreloader()
        {
            this.loaderList = new List<AssetLoader>();
        }


        public void StartPreloading()
        {
            bool loadStarted = false;
            foreach (AssetLoader loader in loaderList)
            {
                if (loader.IsReady)
                {
                    continue;
                }

                waitCounter++;
                loader.AssetLoadedUntyped += OnAssetLoaded;
                loader.LoadAsset();
                loadStarted = true;
            }

            if (!loadStarted)
            {
                PreloadCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        public void AddAssetLoader(AssetLoader loader)
        {
            loaderList.Add(loader);
        }

        private void OnAssetLoaded(object sender, EventArgs args)
        {
            waitCounter--;
            if (waitCounter != 0)
            {
                return;
            }

            PreloadCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}