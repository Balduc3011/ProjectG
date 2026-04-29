#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuickAccess.Editor
{
    /// <summary>
    /// Editor window: browse sections, ping/remove items, drag-and-drop and object picker to add entries.
    /// </summary>
    public sealed class QuickAccessWindow : EditorWindow
    {
        const string MenuPath = "Tools/Quick Access";

        QuickAccessDatabase _database;
        Vector2 _scroll;
        int _pickerSectionIndex = -1;
        int _activeObjectPickerControlId = -1;

        [MenuItem(MenuPath)]
        static void Open()
        {
            var win = GetWindow<QuickAccessWindow>();
            win.titleContent = new GUIContent("Quick Access");
            win.minSize = new Vector2(420f, 280f);
            win.Show();
        }

        void OnEnable()
        {
            _database = QuickAccessDatabaseUtility.GetOrCreate();
        }

        void OnGUI()
        {
            if (_database == null)
            {
                _database = QuickAccessDatabaseUtility.GetOrCreate();
            }

            HandleObjectPickerCommands();

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Sections", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("➕ Add Section", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                {
                    AddCustomSection();
                }
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            var sections = _database.SectionsMutable;
            for (var i = 0; i < sections.Count; i++)
            {
                DrawSection(i, sections[i]);
                EditorGUILayout.Space(6f);
            }

            EditorGUILayout.EndScrollView();
        }

        void HandleObjectPickerCommands()
        {
            var evt = Event.current;
            if (evt.type != EventType.ExecuteCommand)
            {
                return;
            }

            if (_activeObjectPickerControlId < 0)
            {
                return;
            }

            if (EditorGUIUtility.GetObjectPickerControlID() != _activeObjectPickerControlId)
            {
                return;
            }

            switch (evt.commandName)
            {
                case "ObjectSelectorClosed":
                    var picked = EditorGUIUtility.GetObjectPickerObject();
                    if (picked != null && _pickerSectionIndex >= 0)
                    {
                        TryAddObjectToSection(_pickerSectionIndex, picked, preferAutoName: true);
                    }

                    _pickerSectionIndex = -1;
                    _activeObjectPickerControlId = -1;
                    evt.Use();
                    break;
                case "ObjectSelectorCanceled":
                    _pickerSectionIndex = -1;
                    _activeObjectPickerControlId = -1;
                    evt.Use();
                    break;
            }
        }

        void DrawSection(int sectionIndex, QuickAccessSection section)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Section", GUILayout.Width(52f));
                    EditorGUI.BeginChangeCheck();
                    section.SectionName = EditorGUILayout.TextField(section.SectionName);
                    if (EditorGUI.EndChangeCheck())
                    {
                        SaveDatabase();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("➕ Add Item", GUILayout.Width(96f)))
                    {
                        _pickerSectionIndex = sectionIndex;
                        _activeObjectPickerControlId = EditorGUIUtility.GetControlID(FocusType.Passive);
                        EditorGUIUtility.ShowObjectPicker<UnityEngine.Object>(
                            null,
                            false,
                            string.Empty,
                            _activeObjectPickerControlId);
                    }

                    EditorGUI.BeginDisabledGroup(section.IsBuiltIn);
                    if (GUILayout.Button(new GUIContent("❌ Delete Section", "Only custom sections can be removed."), GUILayout.Width(132f)))
                    {
                        RemoveSectionAt(sectionIndex);
                        return;
                    }

                    EditorGUI.EndDisabledGroup();
                    GUILayout.FlexibleSpace();
                }

                DrawSectionDropArea(sectionIndex);

                var items = section.Items;
                for (var j = items.Count - 1; j >= 0; j--)
                {
                    DrawItemRow(sectionIndex, j, items[j]);
                }

                if (items.Count == 0)
                {
                    EditorGUILayout.LabelField("No items yet.", EditorStyles.centeredGreyMiniLabel);
                }
            }
        }

        void DrawSectionDropArea(int sectionIndex)
        {
            var rect = GUILayoutUtility.GetRect(0f, 24f, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "Drop assets here", EditorStyles.miniLabel);
            HandleDragIntoSection(sectionIndex, rect);
        }

        void HandleDragIntoSection(int sectionIndex, Rect rect)
        {
            var evt = Event.current;
            if (!rect.Contains(evt.mousePosition))
            {
                return;
            }

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0)
                    {
                        return;
                    }

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        if (_database != null)
                        {
                            foreach (var obj in DragAndDrop.objectReferences)
                            {
                                if (obj != null)
                                {
                                    TryAddObjectToSection(sectionIndex, obj, preferAutoName: true);
                                }
                            }
                        }

                        evt.Use();
                    }
                    else
                    {
                        evt.Use();
                    }

                    break;
            }
        }

        void DrawItemRow(int sectionIndex, int itemIndex, QuickAccessItem item)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var kind = QuickAccessTypeDetector.Classify(item.Target);
                var suffix = QuickAccessTypeDetector.KindLabel(kind);
                if (!string.IsNullOrEmpty(suffix))
                {
                    GUILayout.Label(suffix, EditorStyles.miniLabel, GUILayout.Width(40f));
                }
                else
                {
                    GUILayout.Space(40f);
                }

                var displayLabel = GetItemButtonLabel(item);
                EditorGUI.BeginDisabledGroup(item.Target == null);
                if (GUILayout.Button(displayLabel, GUILayout.MinWidth(80f), GUILayout.ExpandWidth(true)))
                {
                    HandleItemOpen(item);
                }

                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(item.Target == null);
                if (GUILayout.Button("Ping", GUILayout.Width(44f)))
                {
                    EditorGUIUtility.PingObject(item.Target);
                }

                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("❌", GUILayout.Width(28f)))
                {
                    RemoveItemAt(sectionIndex, itemIndex);
                }
            }
        }

        static string GetItemButtonLabel(QuickAccessItem item)
        {
            if (item.Target == null)
            {
                return string.IsNullOrEmpty(item.Name) ? "(missing)" : $"{item.Name} (missing)";
            }

            if (!string.IsNullOrEmpty(item.Name))
            {
                return item.Name;
            }

            return BuildDefaultDisplayName(item.Target);
        }

        static void HandleItemOpen(QuickAccessItem item)
        {
            var target = item.Target;
            if (target == null)
            {
                return;
            }

            var kind = QuickAccessTypeDetector.Classify(target);
            switch (kind)
            {
                case QuickAccessTypeDetector.AssetKind.Scene:
                    if (target is SceneAsset sceneAsset)
                    {
                        var path = AssetDatabase.GetAssetPath(sceneAsset);
                        if (!string.IsNullOrEmpty(path) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                        }
                    }

                    break;
                case QuickAccessTypeDetector.AssetKind.Prefab:
                    AssetDatabase.OpenAsset(target);
                    break;
                case QuickAccessTypeDetector.AssetKind.ScriptableObject:
                    Selection.activeObject = target;
                    EditorGUIUtility.PingObject(target);
                    break;
                default:
                    Selection.activeObject = target;
                    EditorGUIUtility.PingObject(target);
                    break;
            }
        }

        void TryAddObjectToSection(int sectionIndex, Object obj, bool preferAutoName)
        {
            if (_database == null || sectionIndex < 0 || sectionIndex >= _database.SectionsMutable.Count)
            {
                return;
            }

            var section = _database.SectionsMutable[sectionIndex];
            var list = section.Items;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Target == obj)
                {
                    return;
                }
            }

            var name = preferAutoName ? BuildDefaultDisplayName(obj) : obj.name;
            list.Add(new QuickAccessItem(name, obj));
            SaveDatabase();
            Repaint();
        }

        static string BuildDefaultDisplayName(Object obj)
        {
            if (obj == null)
            {
                return "Empty";
            }

            var path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
            {
                return Path.GetFileNameWithoutExtension(path);
            }

            return obj.name;
        }

        void AddCustomSection()
        {
            var sections = _database.SectionsMutable;
            var baseName = "New Section";
            var name = baseName;
            var n = 1;
            while (SectionNameExists(name))
            {
                n++;
                name = $"{baseName} {n}";
            }

            sections.Add(new QuickAccessSection(name, false));
            SaveDatabase();
        }

        bool SectionNameExists(string name)
        {
            foreach (var s in _database.SectionsMutable)
            {
                if (s.SectionName == name)
                {
                    return true;
                }
            }

            return false;
        }

        void RemoveSectionAt(int index)
        {
            var sections = _database.SectionsMutable;
            if (index < 0 || index >= sections.Count)
            {
                return;
            }

            if (sections[index].IsBuiltIn)
            {
                return;
            }

            sections.RemoveAt(index);
            SaveDatabase();
        }

        void RemoveItemAt(int sectionIndex, int itemIndex)
        {
            var sections = _database.SectionsMutable;
            if (sectionIndex < 0 || sectionIndex >= sections.Count)
            {
                return;
            }

            var items = sections[sectionIndex].Items;
            if (itemIndex < 0 || itemIndex >= items.Count)
            {
                return;
            }

            items.RemoveAt(itemIndex);
            SaveDatabase();
        }

        void SaveDatabase()
        {
            if (_database == null)
            {
                return;
            }

            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
