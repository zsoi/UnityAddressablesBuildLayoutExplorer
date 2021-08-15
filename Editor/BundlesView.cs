﻿//
// Addressables Build Layout Explorer for Unity. Copyright (c) 2021 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityAddressablesBuildLayoutExplorer
//
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;

namespace Oddworm.EditorFramework.BuildLayoutExplorer
{
    [BuildLayoutView]
    public class BundlesView : BuildLayoutView
    {
        BundleTreeView m_TreeView;
        SearchField m_SearchField;
        string m_StatusLabel;
        ReferencesView m_ReferencesToView;
        ReferencesView m_ReferencedByView;

        public override void Awake()
        {
            base.Awake();

            viewMenuOrder = 5;
            m_TreeView = new BundleTreeView(window);
            m_TreeView.selectedItemChanged += SelectionChanged;
            m_SearchField = new SearchField(window);

            m_ReferencesToView = CreateView<ReferencesView>();
            m_ReferencesToView.titleContent = new GUIContent("References to");

            m_ReferencedByView = CreateView<ReferencesView>();
            m_ReferencedByView.titleContent = new GUIContent("Referenced by");
        }

        public override void Rebuild(RichBuildLayout buildLayout)
        {
            base.Rebuild(buildLayout);

            m_TreeView.SetBuildLayout(buildLayout);

            var size = 0L;
            var count = 0;
            foreach(var group in buildLayout.groups)
            {
                foreach(var bundle in group.bundles)
                {
                    size += bundle.size;
                    count++;
                }
            }
            m_StatusLabel = $"{count} bundles making up {EditorUtility.FormatBytes(size)}";
        }

        public override void OnGUI()
        {
            var rect = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            m_TreeView.OnGUI(rect);

            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(window.position.height * 0.333f)))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    m_ReferencesToView.OnGUI();
                }
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    m_ReferencedByView.OnGUI();
                }
            }
        }

        public override void OnToolbarGUI()
        {
            base.OnToolbarGUI();

            GUILayout.Space(10);

            if (m_SearchField.OnToolbarGUI(GUILayout.ExpandWidth(true)))
                m_TreeView.Search(m_SearchField.text);
        }

        public override void OnStatusbarGUI()
        {
            base.OnStatusbarGUI();

            GUILayout.Label(m_StatusLabel);
        }

        public override bool CanNavigateTo(object target)
        {
            if (target is RichBuildLayout.Archive)
                return true;

            return base.CanNavigateTo(target);
        }

        public override void NavigateTo(object target)
        {
            // is the target object a bundle?
            var bundle = target as RichBuildLayout.Archive;
            if (bundle == null)
                return; // nope, we can only process bundle

            // find item that represents the bundle
            var item = m_TreeView.FindItem(bundle);
            if (item == null)
                return;

            // select the item
            m_TreeView.SetSelection(new[] { item.id }, TreeViewSelectionOptions.RevealAndFrame | TreeViewSelectionOptions.FireSelectionChanged);
            m_TreeView.SetFocus();
        }

        public override void SetBookmark(NavigationBookmark bookmark)
        {
            var bm = bookmark as Bookmark;
            if (bm == null)
            {
                Debug.LogError($"Cannot set bookmark, because the argument '{nameof(bookmark)}' is of the wrong type or null.");
                return;
            }

            m_TreeView.SetState(bm.bundlesState);
            m_TreeView.SetFocus();

            m_ReferencesToView.SetBookmark(bm.referencesToBookmark);
            m_ReferencedByView.SetBookmark(bm.referencedByBookmark);
        }

        public override NavigationBookmark GetBookmark()
        {
            var bm = new Bookmark();
            bm.bundlesState = m_TreeView.GetState();
            bm.referencesToBookmark = m_ReferencesToView.GetBookmark();
            bm.referencedByBookmark = m_ReferencedByView.GetBookmark();
            return bm;
        }

        void SelectionChanged(BuildLayoutTreeView.BaseItem item)
        {
            m_ReferencesToView.ShowReferences(Utility.GetReferencesTo(item.GetObject()));
            m_ReferencedByView.ShowReferences(Utility.GetReferencedBy(item.GetObject()));
        }

        class Bookmark : NavigationBookmark
        {
            public BuildLayoutTreeViewState bundlesState;
            public NavigationBookmark referencesToBookmark;
            public NavigationBookmark referencedByBookmark;
        }
    }
}
