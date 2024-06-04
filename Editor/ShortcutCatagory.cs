#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Services.ShortcutPresentor
{
    [System.Serializable]
    public class ShortcutCatagory<Asset> where Asset : Object
    {
        public string displayName;
        public List<ShortcutSlot<Asset>> slots = new();
    }
}
#endif