#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Services.ShortcutPresentor
{
    public class ShortcutSO : ScriptableObject
    {
        public bool showAssetPath = true;
        public bool explainOnOpen = true;

        public List<ShortcutCatagory<SceneAsset>> sceneCatagory = new();
        public List<ShortcutCatagory<DefaultAsset>> defaultCatagory = new();
    }
}
#endif