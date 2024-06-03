#if UNITY_EDITOR
using Services.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.GenericMenu;

namespace Services.ShortcutPresentor
{
    public class ShotcutWindowEditor : EditorWindow
    {
        delegate void PickedDoneFunction(Object pickedObject);

        private const string SODirectory = "Resources/ShortcutPresentors/";
        private const string SOName = "ShortcutSO.asset";

        private string[] taps = new string[] { "Scenes", "Default" };

        private int tapIndex = 0;
        private int pickerControlID;
        private bool isPicking = false;
        private bool isOpeningScene = false;
        private PickedDoneFunction onPickedObject;
        private Vector2 scrollPos;

        private GUIStyle middleLabelStyle;
        private GUIStyle slotStyle;
        private GUIStyle activeSceneSlotStyle;

        [MenuItem("IIStudio/ShotcutWindow")]
        private static void Open()
        {
            var window = GetWindow<ShotcutWindowEditor>("ShotcutPresentor");

            AssetFounderUtility.FoundExistOrCreateOneSO(SODirectory, SOName, out window.so);

        }

        private ShortcutSO so;

        private void OnGUI()
        {
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos))
            {
                scrollPos = scrollView.scrollPosition;
                DrawMainGUI();
            }
        }

        private void DrawMainGUI()
        {
            middleLabelStyle = middleLabelStyle.ToMiddleCenterTextStyle();

            if (slotStyle == null)
            {
                slotStyle = new GUIStyle("GroupBox");
                slotStyle.fixedHeight = 40f;
                slotStyle.fixedWidth = 0f;
            }

            if(activeSceneSlotStyle == null)
            {
                activeSceneSlotStyle = new GUIStyle("Button");
            }

            using (var verticalScope = new EditorGUILayout.VerticalScope("GroupBox"))
            {
                var targetSO = EditorGUILayout.ObjectField("SO Database", so, typeof(ShortcutSO), true) as ShortcutSO;
                if (targetSO != so)
                {
                    Undo.RecordObject(this, "ShortcutWindowEditor_Change_SO");

                    so = targetSO;
                }

                if (!so)
                {
                    EditorGUILayout.LabelField("Data SO is not exsit please close window and open again", middleLabelStyle);
                    return;
                }

                tapIndex = GUILayout.Toolbar(tapIndex, taps);
            }

            if (isPicking)
                SelectObjectChecker();

            using (var verticalScope = new EditorGUILayout.VerticalScope("Box"))
            {
                switch (tapIndex)
                {
                    case 0:
                        DrawSceneAssetList();
                        break;
                    case 1:
                        DrawDefaulAssettList();
                        break;
                }
            }
        }

        private void DrawDetectEmptySlotRightClickMenu(string createMenu, MenuFunction onCreate)
        {
            Rect clickArea = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Event current = Event.current;

            if (clickArea.Contains(current.mousePosition) && current.type == EventType.ContextClick)
            {
                //Do a thing, in this case a drop down menu

                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent(createMenu), false, onCreate);
                menu.ShowAsContext();

                current.Use();
            }
        }

        #region AssetPicker
        private void OpenAssetPiker<T>(PickedDoneFunction onPicked) where T : Object
        {
            pickerControlID = GUIUtility.GetControlID(FocusType.Passive);

            EditorGUIUtility.ShowObjectPicker<T>(null, allowSceneObjects: false, string.Empty, pickerControlID);

            isPicking = true;

            onPickedObject = onPicked;
        }

        private void SelectObjectChecker()
        {
            if (Event.current.commandName == "ObjectSelectorSelectionDone" && EditorGUIUtility.GetObjectPickerControlID() == pickerControlID)
            {
                isPicking = false;
                onPickedObject.Invoke(EditorGUIUtility.GetObjectPickerObject());
                onPickedObject = null;
            }
        }
        #endregion


        #region SceneAssetGUI
        private void DrawSceneAssetList()
        {
            if(so.sceneAssets.Count == 0)
            {
                EditorGUILayout.LabelField("sence is empty add your fist scene", middleLabelStyle);

                DrawDetectEmptySlotRightClickMenu("Create New Slot", () => { OpenAssetPiker<SceneAsset>(AddSceneAssetSlot); });

                return;
            }

            for(int i = 0; i < so.sceneAssets.Count; i++)
            {
                if (!DrawSceneAssetSlot(i))
                    return;
            }

            DrawDetectEmptySlotRightClickMenu("Create New Slot", () => { OpenAssetPiker<SceneAsset>(AddSceneAssetSlot); });
        }

        private bool DrawSceneAssetSlot(int index)
        {
            using (var horizontalScope = new EditorGUILayout.VerticalScope(slotStyle))
            {
                using (var horzontalScope = new EditorGUILayout.HorizontalScope())
                {
                    var sceneAsset = so.sceneAssets[index];

                    if (SceneManager.GetActiveScene().name == sceneAsset.name)
                    {
                        var defaultColor = GUI.backgroundColor;
                        GUI.backgroundColor = Color.green;
                        if (GUILayout.Button($"Current <{sceneAsset.name}>"))
                        {
                            //DO Nothing.
                        }
                        GUI.backgroundColor = defaultColor;
                    }
                    else
                    {
                        if (GUILayout.Button($"Open <{sceneAsset.name}>"))
                        {
                            if (isOpeningScene)
                                return true;

                            isOpeningScene = true;

                            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

                            EditorSceneManager.sceneOpened += OnOpenSceneSuccess;
                            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(sceneAsset));
                        }
                    }

                    so.sceneAssets[index] = EditorGUILayout.ObjectField(so.sceneAssets[index], typeof(SceneAsset), true,GUILayout.Width(120)) as SceneAsset;

                    var downloadIcon = EditorGUIUtility.IconContent("d_winbtn_win_close");

                    if (GUILayout.Button(downloadIcon, GUILayout.Width(60)))
                    {
                        RemoveSceneAssetSlot(index);
                        return false;
                    }
                }    
            }

            return true;
        }

        private void OnOpenSceneSuccess(Scene scene, OpenSceneMode mode)
        {
            EditorSceneManager.sceneOpened -= OnOpenSceneSuccess;
            isOpeningScene = false;
        }

        private void AddSceneAssetSlot(Object pickedObject)
        {
            if (pickedObject == null || pickedObject is not SceneAsset sceneAsset)
                return;

            Undo.RecordObject(so, "Shortcut_AddSceneAsset_Slot");

            so.sceneAssets.Add(sceneAsset);

            AssetDatabase.SaveAssetIfDirty(so);
            AssetDatabase.SaveAssets();
        }

        private void RemoveSceneAssetSlot(int index)
        {
            Undo.RecordObject(so, "Shortcut_RemoveSceneAsset_Slot");

            so.sceneAssets.RemoveAt(index);

            AssetDatabase.SaveAssetIfDirty(so);
            AssetDatabase.SaveAssets();
        }
        #endregion

        #region DefualAssetGUI
        private void DrawDefaulAssettList()
        {
            if (so.defaultAssets.Count == 0)
            {
                EditorGUILayout.LabelField("folder asset is empty add your fist scene", middleLabelStyle);

                DrawDetectEmptySlotRightClickMenu("Create New Slot", () => { OpenAssetPiker<DefaultAsset>(AddDefaultAssetSlot); });

                return;
            }

            for (int i = 0; i < so.defaultAssets.Count; i++)
            {
                if (!DrawDefaultAssetSlot(i))
                    return;
            }

            DrawDetectEmptySlotRightClickMenu("Create New Slot", () => { OpenAssetPiker<DefaultAsset>(AddDefaultAssetSlot); });
        }

        private bool DrawDefaultAssetSlot(int index)
        {
            using (var horizontalScope = new EditorGUILayout.VerticalScope(slotStyle))
            {
                using (var horzontalScope = new EditorGUILayout.HorizontalScope())
                {
                    var defaultAsset = so.defaultAssets[index];

                    if (GUILayout.Button($"Open <{defaultAsset.name}>"))
                    {
                        string assetPath = AssetDatabase.GetAssetPath(defaultAsset);
                        bool isFolder = AssetDatabase.IsValidFolder(assetPath);

                        if (isFolder)
                        {
                            AssetDatabase.OpenAsset(defaultAsset);
                        }
                        else
                        {
                            EditorGUIUtility.PingObject(defaultAsset);
                        }
                    }

                    so.defaultAssets[index] = EditorGUILayout.ObjectField(so.defaultAssets[index], typeof(DefaultAsset), true, GUILayout.Width(120)) as DefaultAsset;

                    var downloadIcon = EditorGUIUtility.IconContent("d_winbtn_win_close");

                    if (GUILayout.Button(downloadIcon, GUILayout.Width(60)))
                    {
                        RemoveDefaultAssetSlot(index);
                        return false;
                    }
                }
            }

            return true;
        }

        private void RemoveDefaultAssetSlot(int index)
        {
            Undo.RecordObject(so, "Shortcut_RemoveDefaultAsset_Slot");

            so.defaultAssets.RemoveAt(index);

            AssetDatabase.SaveAssetIfDirty(so);
            AssetDatabase.SaveAssets();
        }

        private void AddDefaultAssetSlot(Object pickedObject)
        {
            if (pickedObject == null || pickedObject is not DefaultAsset defaultAsset)
                return;

            Undo.RecordObject(so, "Shortcut_AddDefaultAsset_Slot");

            so.defaultAssets.Add(defaultAsset);

            AssetDatabase.SaveAssetIfDirty(so);
            AssetDatabase.SaveAssets();
        }
        #endregion
    }
}
#endif