#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Services.ShortcutPresentor
{
    public class ShortcutSlotModiflyWindowEditor : EditorWindow
    {
        private IShortcutSlot slot;
        private Function_Callback onCloseCallback;
        private bool isAtMousePoint = false;

        public static void Open(IShortcutSlot slot, Function_Callback onClose)
        {
            var window = GetWindow<ShortcutSlotModiflyWindowEditor>("SlotModifly");
            window.slot = slot;
            window.onCloseCallback = onClose;
            window.maxSize = new Vector2(400, 120);
        }


        private void OnGUI()
        {
            if (!isAtMousePoint)
            {
                Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                position = new Rect(mousePos.x, mousePos.y, position.width, position.height);
                isAtMousePoint = true;
            }

            if (slot == null)
            {
                Close();
                return;
            }

            EditorGUILayout.LabelField("Display Name");

            using (var horitontalScope = new EditorGUILayout.HorizontalScope())
            {
                slot.displayName = EditorGUILayout.TextField(slot.displayName);

                if (GUILayout.Button("Use Default", GUILayout.Width(100)))
                {
                    slot.displayName = slot.assetName;
                    Repaint();
                }
            }
           
          
            EditorGUILayout.LabelField($"Asset Name : {slot.assetName}");
            EditorGUILayout.LabelField($"Asset Path : {slot.assetPath}");

            EditorGUILayout.Space();

            if (GUILayout.Button("Save"))
            {
                Close();
            }
        }

        private void OnDisable()
        {
            onCloseCallback?.Invoke();
        }
    }

    public delegate void Function_Callback();
}
#endif