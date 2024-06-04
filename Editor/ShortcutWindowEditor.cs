#if UNITY_EDITOR
using Services.Utility;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.GenericMenu;

namespace Services.ShortcutPresentor
{
    public class ShortcutWindowEditor : EditorWindow
    {
        delegate void PickedDoneFunction(Object pickedObject);
        delegate void ExistAssetSlotDrawer<T>(ShortcutSlot<T> slot) where T : Object;

        private const string SODirectory = "Resources/ShortcutPresentors/";
        private const string SOName = "ShortcutSO.asset";

        private string[] taps = new string[] { "Scenes", "Default" };

        private int tapIndex = 0;
        private int pickerControlID;
        private bool isPicking = false;
        private bool isOpeningScene = false;
        private bool isInteractable = true;
        private PickedDoneFunction onPickedObject;
        private Vector2 scrollPos;

        private GUIStyle middleLabelStyle;
        private GUIStyle catagoryTitleStyle;
        private GUIStyle slotStyle;
        private GUIStyle catagoryStyle;
        private GUIStyle activeSceneSlotStyle;

        private AnimBoolGroupController<ShortcutCatagory<SceneAsset>> sceneCatagoryAnimBools = new();
        private AnimBoolGroupController<ShortcutCatagory<DefaultAsset>> defaultCatagoryAnimBools = new();

        private ShortcutSO so;

        public class MenuInfo
        {
            public string name;
            public MenuFunction callback;
            public bool isDisabled;

            public MenuInfo(string name, MenuFunction callback)
            {
                this.name = name;
                this.callback = callback;
            }
        }

        [MenuItem("IIStudio/ShotcutWindow")]
        private static void Open()
        {
            var window = GetWindow<ShortcutWindowEditor>("ShotcutPresentor");

            AssetFounderUtility.FoundExistOrCreateOneSO(SODirectory, SOName, out window.so);

            window.SetupAnimBool();
        }

        private void SetupAnimBool()
        {
            for (int i = 0; i < so.sceneCatagory.Count; i++)
            {
                var catagory = so.sceneCatagory[i];

                sceneCatagoryAnimBools.Add(catagory, startValue: so.explainOnOpen);
            }
        }

        private void OnGUI()
        {
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos))
            {
                scrollPos = scrollView.scrollPosition;
                DrawMainGUI();
            }

            Repaint();
        }

        private void OnDisable()
        {
            SaveSO();
        }

        private void DrawMainGUI()
        {
            middleLabelStyle = middleLabelStyle.ToMiddleCenterTextStyle();

            if (slotStyle == null)
            {
                slotStyle = new GUIStyle("Box");
            }

            if(catagoryStyle == null)
            {
                catagoryStyle = new GUIStyle("GroupBox");
            }

            if(catagoryTitleStyle == null)
            {
                catagoryTitleStyle = new GUIStyle("TextField");
                catagoryTitleStyle.normal = GUI.skin.label.normal;
                catagoryTitleStyle.alignment = TextAnchor.MiddleCenter;
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

                so.showAssetPath = EditorGUILayout.Toggle("Show asset path", so.showAssetPath);
                so.explainOnOpen = EditorGUILayout.Toggle("Explain on open", so.explainOnOpen);

                EditorGUILayout.Space();

                tapIndex = GUILayout.Toolbar(tapIndex, taps);
            }

            if (isPicking)
                SelectObjectChecker();


            using (var verticalScope = new EditorGUILayout.VerticalScope("Box"))
            {
                switch (tapIndex)
                {
                    case 0:
                        DrawMainGUI(so.sceneCatagory, sceneCatagoryAnimBools, ExistAssetSceneSlotDrawer);
                        break;
                    case 1:
                        DrawMainGUI(so.defaultCatagory, defaultCatagoryAnimBools, ExistAssetDefaultSlotDrawer);
                        break;
                }
            }
        }


        private void ExistAssetSceneSlotDrawer(ShortcutSlot<SceneAsset> slot)
        {
            if (SceneManager.GetActiveScene().name == slot.asset.name)
            {
                var defaultColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button($"Current <{slot.displayName}>")) { }
                GUI.backgroundColor = defaultColor;
            }
            else
            {
                if (GUILayout.Button($"Open <{slot.displayName}>") && isInteractable)
                {
                    if (isOpeningScene)
                        return;

                    isOpeningScene = true;

                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

                    EditorSceneManager.sceneOpened += OnOpenSceneSuccess;
                    EditorSceneManager.OpenScene(slot.assetPath);
                }
            }
        }

        private void ExistAssetDefaultSlotDrawer(ShortcutSlot<DefaultAsset> slot)
        {
            if (GUILayout.Button($"Open <{slot.displayName}>"))
            {
                bool isFolder = AssetDatabase.IsValidFolder(slot.assetPath);

                if (isFolder)
                {
                    AssetDatabase.OpenAsset(slot.asset);
                }
                else
                {
                    EditorGUIUtility.PingObject(slot.asset);
                }
            }
        }

        #region ClickDetector
        private void CheckRightClickOnArea(System.Action onClick)
        {
            Rect clickArea = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            CheckRightClickOnArea(clickArea, onClick);
        }

        private void CheckRightClickOnArea(Rect clickArea, System.Action onClick)
        {
            Event current = Event.current;

            if (clickArea.Contains(current.mousePosition) && current.type == EventType.ContextClick)
            {
                onClick();

                current.Use();
            }
        }
        #endregion

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

        private void DrawMainGUI<T>(List<ShortcutCatagory<T>> catagoryList,AnimBoolGroupController<ShortcutCatagory<T>> animBoolGroup, ExistAssetSlotDrawer<T> existAssetSlotDrawer) where T : Object
        {
            if(catagoryList.Count == 0)
            {
                EditorGUILayout.LabelField("sence is empty add your fist catagory", middleLabelStyle);

                CheckRightClickOnArea(() =>
                {
                    ShowDropdownMenu(new MenuInfo("Add Catagory", () => 
                    { 
                        AddCatagory(catagoryList, animBoolGroup);
                    }));
                });

                return;
            }

            for(int i = 0; i < catagoryList.Count; i++)
            {
                DrawCatagoryList(catagoryList, animBoolGroup, i, existAssetSlotDrawer);
            }

            CheckRightClickOnArea(() =>
            {
                ShowDropdownMenu(new MenuInfo("Add Catagory", () =>
                {
                    AddCatagory(catagoryList, animBoolGroup);
                }));
            });
        }

        public void ShowDropdownMenu(params MenuInfo[] menuInfos)
        {
            GenericMenu menu = new GenericMenu();

            for(int i = 0;i < menuInfos.Length;i++)
            {
                MenuInfo info = menuInfos[i];

                if (info.isDisabled)
                    menu.AddDisabledItem(new GUIContent(info.name));
                else
                    menu.AddItem(new GUIContent(info.name), false, info.callback);
            }

            menu.ShowAsContext();
        }

        private void DrawCatagoryList<T>(List<ShortcutCatagory<T>> catagoryList, AnimBoolGroupController<ShortcutCatagory<T>> animBoolGroup, int catagoryIndex, ExistAssetSlotDrawer<T> existAssetSlotDrawer) where T : Object
        {
            var catagory = catagoryList[catagoryIndex];

            using (var horizontalScope = new EditorGUILayout.VerticalScope(catagoryStyle))
            {
                catagory.displayName = EditorView.DetectTextField(so, catagory.displayName, "Shortcut_UpdateCatagoryName", catagoryTitleStyle);

                EditorGUILayout.Space();

                var animBool = animBoolGroup.Get(catagory, startValue: false);
                animBool.target = EditorGUILayout.BeginFoldoutHeaderGroup(animBool.target, $"{catagory.slots.Count} item");

                using (var group = new EditorGUILayout.FadeGroupScope(animBool.faded))
                {
                    if (group.visible)
                    {
                        for (int i = 0; i < catagory.slots.Count; i++)
                        {
                            DrawAssetSlot(catagoryList, catagoryIndex, i, existAssetSlotDrawer);
                        }
                    }
                }

                EditorGUILayout.EndFoldoutHeaderGroup();

                if (isInteractable)
                    CheckRightClickOnArea(horizontalScope.rect, () =>
                    {
                        MenuInfo createSlotMenu = new MenuInfo("Add Slot", () =>
                        {
                            OpenAssetPiker<T>((asset) =>
                            {
                                AddAssetSlot(catagoryList, asset, catagoryIndex);
                            });
                        });

                        MenuInfo removeCatagoryMenu = new MenuInfo("Remove Catagory", () =>
                        {
                            RemoveCatagory(catagoryList, animBoolGroup, catagoryIndex);
                        });

                        ShowDropdownMenu(createSlotMenu, removeCatagoryMenu);
                    });
            }

            return;
        }

        private void DrawAssetSlot<T>(List<ShortcutCatagory<T>> catagoryList, int catagoryIndex, int slotIndex, ExistAssetSlotDrawer<T> existAssetSlotDrawer) where T : Object
        {
            var catagory = catagoryList[catagoryIndex];
            var slot = catagory.slots[slotIndex];

            using (var horizontalScope = new EditorGUILayout.VerticalScope(slotStyle))
            {
                using (var horzontalScope = new EditorGUILayout.HorizontalScope())
                {
                    if (slot.asset)
                    {
                        existAssetSlotDrawer(slot);

                        var pingIcon = EditorGUIUtility.IconContent("d_Import");

                        if (GUILayout.Button(pingIcon, GUILayout.Width(40)) && isInteractable)
                        {
                            EditorGUIUtility.PingObject(slot.asset);
                        }
                    }
                    else
                    {
                        slot.asset = EditorGUILayout.ObjectField(slot.asset, typeof(T), true) as T;
                    }

                    var downloadIcon = EditorGUIUtility.IconContent("d__Menu");

                    if (GUILayout.Button(downloadIcon, GUILayout.Width(40)) && isInteractable)
                    {
                        //RemoveSceneAssetSlot(catagoryIndex,slotIndex);
                        int catagoryCount = catagoryList.Count;
                        MenuInfo[] slotMenus = new MenuInfo[catagoryCount + 2];

                        for (int i = 0; i < catagoryCount; i++)
                        {
                            var targetCatagory = catagoryList[i];

                            int targetCatagoryIndex = i;

                            var menu = new MenuInfo($"Change Catagory/{targetCatagory.displayName}", () =>
                            {
                                ChangeAssetSlotCatagory(catagoryList, catagoryIndex, targetCatagoryIndex, slotIndex);
                            });
                            menu.isDisabled = catagoryIndex == i;

                            slotMenus[i] = menu;
                        }

                        slotMenus[catagoryCount] = new MenuInfo("Edit", () =>
                        {
                            isInteractable = false;

                            ShortcutSlotModiflyWindowEditor.Open(slot, () =>
                            {
                                SaveSO();

                                isInteractable = true;
                            });
                        });

                        slotMenus[catagoryCount + 1] = new MenuInfo("Remove", () =>
                        {
                            RemoveAssetSlot(catagoryList, catagoryIndex, slotIndex);
                        });

                        ShowDropdownMenu(slotMenus);
                    }

                    CheckRightClickOnArea(horizontalScope.rect, () =>
                    {
                       //Just block.
                    });
                }

                if (so.showAssetPath)
                    EditorGUILayout.LabelField(slot.assetPath);
            }

            return;
        }

        private void OnOpenSceneSuccess(Scene scene, OpenSceneMode mode)
        {
            EditorSceneManager.sceneOpened -= OnOpenSceneSuccess;
            isOpeningScene = false;
        }

        private void AddCatagory<T>(List<ShortcutCatagory<T>> catagoryList, AnimBoolGroupController<ShortcutCatagory<T>> animBoolGroup) where T : Object 
        {
            UndoRecordSO("Shortcut_AddCatagory");

            var catagory = new ShortcutCatagory<T>();

            catagory.displayName = $"Untitled ({catagoryList.Count + 1})";

            catagoryList.Add(catagory);
            animBoolGroup.Add(catagory, startValue: true);

            SaveSO();
        }

        private void RemoveCatagory<T>(List<ShortcutCatagory<T>> catagoryList, AnimBoolGroupController<ShortcutCatagory<T>> animBoolGroup, int catagoryIndex) where T : Object
        {
            UndoRecordSO("Shortcut_RemoveCatagory");

            animBoolGroup.Remove(catagoryList[catagoryIndex]);
            catagoryList.RemoveAt(catagoryIndex);

            SaveSO();
        }

        private void AddAssetSlot<T>(List<ShortcutCatagory<T>> catagoryList, Object pickedObject, int catagoryIndex) where T: Object
        {
            if (pickedObject == null || pickedObject is not T asset)
                return;

            UndoRecordSO("Shortcut_AddAsset_Slot");

            var slot = new ShortcutSlot<T>();
            slot.displayName = asset.name;
            slot.asset = asset;

            var catagory = catagoryList[catagoryIndex];
            catagory.slots.Add(slot);

            SaveSO();
        }

        private void RemoveAssetSlot<T>(List<ShortcutCatagory<T>> catagoryList, int catagoryIndex, int index) where T : Object
        {
            UndoRecordSO("Shortcut_RemoveAsset_Slot");

            var catagory = catagoryList[catagoryIndex];
            catagory.slots.RemoveAt(index);

            SaveSO();
        }

        private void ChangeAssetSlotCatagory<T>(List<ShortcutCatagory<T>> catagoryList, int originCatagoryIndex, int targetCatagoryIndex, int slotIndex) where T : Object
        {
            UndoRecordSO("Shortcut_ChangeAssetSlotCatagory");

            var originCatagory = catagoryList[originCatagoryIndex];
            var targetCatagory = catagoryList[targetCatagoryIndex];

            var slot = originCatagory.slots[slotIndex];
            targetCatagory.slots.Add(slot);
            originCatagory.slots.RemoveAt(slotIndex);

            SaveSO();
        }

        private void UndoRecordSO(string name)
        {
            Undo.RecordObject(so, name);
        }
        private void SaveSO()
        {
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssetIfDirty(so);
        }
    }
}
#endif