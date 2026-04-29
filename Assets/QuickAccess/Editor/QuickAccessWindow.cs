#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuickAccess.Editor
{
    /// <summary>
    /// Editor window: browse sections, ping/remove items, drag-and-drop to add entries.
    /// </summary>
    public sealed class QuickAccessWindow : EditorWindow
    {
        const string MenuPath = "Tools/Quick Access";

        const float SectionHeaderButtonSize = 28f;

        static readonly Color SectionHeaderPenTint = new Color(0.25f, 0.92f, 1f, 1f);
        static readonly Color SectionHeaderBinTint = new Color(0.95f, 0.28f, 0.28f, 1f);

        const string SectionRenameGlyph = "✏️";
        const string SectionDeleteGlyph = "💩";
        const string SectionToggleGlyph = "👀";
        const string ItemPingGlyph = "🔗";
        const string ItemDeleteGlyph = "❌";

        QuickAccessDatabase _database;
        Vector2 _scroll;
        int _editingSectionNameIndex = -1;

        int _itemReorderSection = -1;
        int _itemReorderFromIndex = -1;
        int _itemReorderInsertBefore;
        readonly List<Rect> _itemReorderRowRects = new List<Rect>(32);
        readonly List<Rect> _itemReorderCachedRects = new List<Rect>(32);

        GUIStyle _sectionTitleStyle;

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
            EnsureSectionHeaderStyles();
        }

        void EnsureSectionHeaderStyles()
        {
            if (_sectionTitleStyle == null)
            {
                // EditorStyles may not be ready in OnEnable; basis must never be null or GUILayout.Label NREs.
                var basis = EditorStyles.boldLabel ?? EditorStyles.label ?? new GUIStyle();
                _sectionTitleStyle = new GUIStyle(basis)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false,
                };
            }

            // Keep size / color in sync (skin, DPI) — larger green title, centered in title strip.
            _sectionTitleStyle.fontSize = 22;
            _sectionTitleStyle.alignment = TextAnchor.MiddleCenter;
            var titleGreen = EditorGUIUtility.isProSkin
                ? new Color(0.45f, 0.98f, 0.58f)
                : new Color(0.05f, 0.52f, 0.2f);
            _sectionTitleStyle.normal.textColor = titleGreen;
            _sectionTitleStyle.active.textColor = titleGreen;
            _sectionTitleStyle.focused.textColor = titleGreen;
            _sectionTitleStyle.hover.textColor = titleGreen;
        }

        void OnGUI()
        {
            EnsureSectionHeaderStyles();

            if (_database == null)
            {
                _database = QuickAccessDatabaseUtility.GetOrCreate();
            }

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
            FinishItemReorderIfNeeded();
        }

        void DrawSection(int sectionIndex, QuickAccessSection section)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (DrawSectionHeaderRow(sectionIndex, section))
                {
                    return;
                }

                if (section.ItemsExpanded)
                {
                    EditorGUILayout.Space(4f);
                    DrawSectionDropArea(sectionIndex);

                    var items = section.Items;
                    _itemReorderRowRects.Clear();
                    for (var j = 0; j < items.Count; j++)
                    {
                        _itemReorderRowRects.Add(DrawItemRow(sectionIndex, j, items[j]));
                    }

                    if (sectionIndex == _itemReorderSection && _itemReorderFromIndex >= 0 && _itemReorderRowRects.Count > 0)
                    {
                        _itemReorderCachedRects.Clear();
                        _itemReorderCachedRects.AddRange(_itemReorderRowRects);
                        _itemReorderInsertBefore = ComputeInsertBeforeIndex(
                            _itemReorderRowRects,
                            Event.current.mousePosition.y);
                        if (Event.current.type == EventType.Repaint && Input.GetMouseButton(0))
                        {
                            DrawReorderInsertIndicator(_itemReorderRowRects, _itemReorderInsertBefore);
                        }
                    }

                    if (items.Count == 0)
                    {
                        EditorGUILayout.LabelField("No items yet.", EditorStyles.centeredGreyMiniLabel);
                    }
                }
            }
        }

        /// <returns>True if the section was removed (caller must stop drawing this block).</returns>
        bool DrawSectionHeaderRow(int sectionIndex, QuickAccessSection section)
        {
            var removed = false;
            using (new EditorGUILayout.HorizontalScope(GUILayout.MinHeight(SectionHeaderButtonSize + 4f)))
            {
                if (GUILayout.Button(
                        new GUIContent(
                            SectionToggleGlyph,
                            section.ItemsExpanded ? "Collapse items" : "Expand items"),
                        GUILayout.Width(SectionHeaderButtonSize),
                        GUILayout.Height(SectionHeaderButtonSize)))
                {
                    section.ItemsExpanded = !section.ItemsExpanded;
                    if (!section.ItemsExpanded && _itemReorderSection == sectionIndex)
                    {
                        _itemReorderSection = -1;
                        _itemReorderFromIndex = -1;
                    }

                    SaveDatabase();
                }

                var title = string.IsNullOrEmpty(section.SectionName) ? "(Untitled)" : section.SectionName;

                if (_editingSectionNameIndex == sectionIndex)
                {
                    GUI.SetNextControlName("SectionRename_" + sectionIndex);
                    EditorGUI.BeginChangeCheck();
                    section.SectionName = EditorGUILayout.DelayedTextField(
                        section.SectionName,
                        GUILayout.ExpandWidth(true),
                        GUILayout.Height(SectionHeaderButtonSize));
                    if (EditorGUI.EndChangeCheck())
                    {
                        SaveDatabase();
                    }
                }
                else
                {
                    GUILayout.Label(title, _sectionTitleStyle, GUILayout.ExpandWidth(true), GUILayout.Height(SectionHeaderButtonSize + 4f));
                }

                var oldColor = GUI.color;
                GUI.color = SectionHeaderPenTint;
                if (GUILayout.Button(
                        new GUIContent(SectionRenameGlyph, "Rename section"),
                        GUILayout.Width(SectionHeaderButtonSize),
                        GUILayout.Height(SectionHeaderButtonSize)))
                {
                    if (_editingSectionNameIndex == sectionIndex)
                    {
                        _editingSectionNameIndex = -1;
                        GUI.FocusControl(null);
                    }
                    else
                    {
                        _editingSectionNameIndex = sectionIndex;
                        var idx = sectionIndex;
                        EditorApplication.delayCall += () =>
                        {
                            EditorGUI.FocusTextInControl("SectionRename_" + idx);
                        };
                    }
                }

                GUI.color = SectionHeaderBinTint;
                EditorGUI.BeginDisabledGroup(section.IsBuiltIn);
                if (GUILayout.Button(
                        new GUIContent(SectionDeleteGlyph, "Delete section"),
                        GUILayout.Width(SectionHeaderButtonSize),
                        GUILayout.Height(SectionHeaderButtonSize)))
                {
                    if (EditorUtility.DisplayDialog(
                            "Delete section",
                            $"Delete section \"{section.SectionName}\"? This cannot be undone.",
                            "Delete",
                            "Cancel"))
                    {
                        _editingSectionNameIndex = -1;
                        RemoveSectionAt(sectionIndex);
                        removed = true;
                    }
                }

                EditorGUI.EndDisabledGroup();
                GUI.color = oldColor;
            }

            return removed;
        }

        void DrawSectionDropArea(int sectionIndex)
        {
            var dropH = EditorGUIUtility.singleLineHeight + 6f;
            var rect = GUILayoutUtility.GetRect(0f, dropH, GUILayout.ExpandWidth(true));
            GUI.Box(rect, GUIContent.none, EditorStyles.textField);
            var hint = EditorStyles.centeredGreyMiniLabel;
            GUI.Label(rect, "Drop assets here", hint);
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

        Rect DrawItemRow(int sectionIndex, int itemIndex, QuickAccessItem item)
        {
            const float dragW = 16f;
            const float kindW = 40f;
            const float pingW = 28f;
            const float delW = 28f;
            const float gap = 2f;

            var full = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4f, GUILayout.ExpandWidth(true));
            var x = full.x;
            var y = full.y;
            var h = full.height;
            var right = full.xMax;

            var delRect = new Rect(right - delW, y, delW, h);
            right -= delW;
            var pingRect = new Rect(right - pingW - 2f, y, pingW, h);
            right -= pingW;
            right -= gap;

            var handleRect = new Rect(x, y, dragW, h);
            x += dragW;
            var kindRect = new Rect(x, y, kindW, h);
            x += kindW;
            var openW = Mathf.Max(40f, right - x);
            var openRect = new Rect(x - 2f, y, openW, h);

            DrawReorderDragHandle(handleRect, sectionIndex, itemIndex);

            var kind = QuickAccessTypeDetector.Classify(item.Target);
            var suffix = QuickAccessTypeDetector.KindLabel(kind) ?? string.Empty;
            EditorGUI.LabelField(kindRect, suffix, EditorStyles.miniLabel);

            var displayLabel = GetItemButtonLabel(item);
            EditorGUI.BeginDisabledGroup(item.Target == null);
            if (GUI.Button(openRect, displayLabel))
            {
                HandleItemOpen(item);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(item.Target == null);
            if (GUI.Button(pingRect, ItemPingGlyph))
            {
                EditorGUIUtility.PingObject(item.Target);
            }

            EditorGUI.EndDisabledGroup();

            if (GUI.Button(delRect, ItemDeleteGlyph))
            {
                RemoveItemAt(sectionIndex, itemIndex);
            }

            return full;
        }

        void DrawReorderDragHandle(Rect rect, int sectionIndex, int itemIndex)
        {
            var isSource = _itemReorderSection == sectionIndex && _itemReorderFromIndex == itemIndex;
            if (isSource && Input.GetMouseButton(0))
            {
                EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.07f));
            }

            EditorGUI.LabelField(rect, "\u2261", EditorStyles.centeredGreyMiniLabel);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.MoveArrow);
            var evt = Event.current;
            if (evt.type == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition))
            {
                _itemReorderSection = sectionIndex;
                _itemReorderFromIndex = itemIndex;
                evt.Use();
            }
        }

        static int ComputeInsertBeforeIndex(List<Rect> rowRects, float mouseY)
        {
            if (rowRects.Count == 0)
            {
                return 0;
            }

            for (var i = 0; i < rowRects.Count; i++)
            {
                var midY = rowRects[i].yMin + rowRects[i].height * 0.5f;
                if (mouseY < midY)
                {
                    return i;
                }
            }

            return rowRects.Count;
        }

        static void DrawReorderInsertIndicator(List<Rect> rowRects, int insertBefore)
        {
            if (rowRects.Count == 0)
            {
                return;
            }

            float y;
            if (insertBefore <= 0)
            {
                y = rowRects[0].yMin;
            }
            else if (insertBefore >= rowRects.Count)
            {
                y = rowRects[rowRects.Count - 1].yMax;
            }
            else
            {
                y = (rowRects[insertBefore - 1].yMax + rowRects[insertBefore].yMin) * 0.5f;
            }

            var xMin = rowRects[0].xMin;
            var xMax = rowRects[rowRects.Count - 1].xMax;
            EditorGUI.DrawRect(new Rect(xMin, y - 1f, xMax - xMin, 2f), new Color(0.35f, 0.65f, 1f, 0.95f));
        }

        void FinishItemReorderIfNeeded()
        {
            var evt = Event.current;
            if (evt.type != EventType.MouseUp || _itemReorderFromIndex < 0)
            {
                return;
            }

            if (_database != null &&
                _itemReorderSection >= 0 &&
                _itemReorderSection < _database.SectionsMutable.Count)
            {
                var list = _database.SectionsMutable[_itemReorderSection].Items;
                var insertBefore = _itemReorderCachedRects.Count > 0
                    ? ComputeInsertBeforeIndex(_itemReorderCachedRects, evt.mousePosition.y)
                    : _itemReorderInsertBefore;
                if (TryMoveItem(list, _itemReorderFromIndex, insertBefore))
                {
                    SaveDatabase();
                }
            }

            _itemReorderSection = -1;
            _itemReorderFromIndex = -1;
            evt.Use();
        }

        static bool TryMoveItem(List<QuickAccessItem> list, int fromIndex, int insertBefore)
        {
            if (fromIndex < 0 || fromIndex >= list.Count)
            {
                return false;
            }

            if (insertBefore < 0)
            {
                insertBefore = 0;
            }

            if (insertBefore > list.Count)
            {
                insertBefore = list.Count;
            }

            if (insertBefore == fromIndex || insertBefore == fromIndex + 1)
            {
                return false;
            }

            var item = list[fromIndex];
            list.RemoveAt(fromIndex);
            var newIndex = insertBefore > fromIndex ? insertBefore - 1 : insertBefore;
            list.Insert(newIndex, item);
            return true;
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
            if (_editingSectionNameIndex == index)
            {
                _editingSectionNameIndex = -1;
            }
            else if (_editingSectionNameIndex > index)
            {
                _editingSectionNameIndex--;
            }

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
