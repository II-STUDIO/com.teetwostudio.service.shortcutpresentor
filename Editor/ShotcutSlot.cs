#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Services.ShortcutPresentor
{
    [System.Serializable]
    public class ShortcutSlot<TAsset> : IShortcutSlot where TAsset : Object
    {
        public string displayName {  get; set; }
        public string assetName 
        {
            get
            {
                if (!asset)
                    return "No Asset on this slot";

                return asset.name;
            }
        }

        public string assetPath
        {
            get
            {
                if (!asset)
                    return "No Asset on this slot";

                return AssetDatabase.GetAssetPath(asset);
            }
        }

        public TAsset asset;
    }

    public interface IShortcutSlot 
    { 
        public string displayName { get; set; }
        public string assetName { get; }

        public string assetPath { get; }
    }
}
#endif