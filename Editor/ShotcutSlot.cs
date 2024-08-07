#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Services.ShortcutPresentor
{
    [System.Serializable]
    public class ShortcutSlot<TAsset> : IShortcutSlot where TAsset : Object
    {
        public string displayName
        {
            get => m_displayName;
            set => m_displayName = value;
        }

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

        [SerializeField] private string m_displayName;
    }

    public interface IShortcutSlot 
    { 
        public string displayName { get; set; }
        public string assetName { get; }

        public string assetPath { get; }
    }
}
#endif