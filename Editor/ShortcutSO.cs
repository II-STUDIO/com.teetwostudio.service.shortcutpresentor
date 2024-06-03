#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Services.ShortcutPresentor
{
    public class ShortcutSO : ScriptableObject
    {
        public List<SceneAsset> sceneAssets = new List<SceneAsset>();
        public List<DefaultAsset> defaultAssets = new List<DefaultAsset>();
    }
}
#endif