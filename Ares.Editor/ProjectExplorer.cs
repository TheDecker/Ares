﻿/*
 Copyright (c) 2010 [Joerg Ruedenauer]
 
 This file is part of Ares.

 Ares is free software; you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation; either version 2 of the License, or
 (at your option) any later version.

 Ares is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with Ares; if not, write to the Free Software
 Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ares.Editor.Actions;
using Ares.Data;

namespace Ares.Editor
{
    partial class ProjectExplorer : WeifenLuo.WinFormsUI.Docking.DockContent
    {
        public ProjectExplorer()
        {
            InitializeComponent();
            HideOnClose = true;
            RecreateTree();
            ElementChanges.Instance.AddListener(-1, ElementChanged);
        }

        private System.Action m_AfterEditAction;
        private bool listenForContainerChanges = true;

        protected override String GetPersistString()
        {
            return "ProjectExplorer";
        }

        public void SetProject(IProject project)
        {
            m_Project = project;
            RecreateTree();
        }

        public ContextMenuStrip EditContextMenu
        {
            get
            {
                ContextMenuStrip menu = SelectedNode != null ? SelectedNode.ContextMenuStrip : null;
                if (menu != null)
                {
                    UpdateContextMenuDueToPlaying(menu);
                    UpdateVisiblityDueToModeElement(menu);
                }
                return menu;
            }
        }

        private void RecreateTree()
        {
            projectTree.BeginUpdate();
            projectTree.Nodes.Clear();
            if (m_Project != null)
            {
                TreeNode projectNode = new TreeNode(m_Project.Title);
                projectNode.Tag = m_Project;
                AddModeNodes(projectNode, m_Project);
                projectNode.ContextMenuStrip = projectContextMenu;
                projectTree.Nodes.Add(projectNode);
            }
            else
            {
                projectTree.Nodes.Add(StringResources.NoOpenedProject);
            }
            projectTree.ExpandAll();
            projectTree.EndUpdate();
        }

        private void AddModeNodes(TreeNode projectNode, IProject project)
        {
            KeysConverter converter = new KeysConverter();
            foreach (IMode mode in project.GetModes())
            {
                TreeNode modeNode = new TreeNode(mode.GetNodeTitle());
                projectNode.Nodes.Add(modeNode);
                modeNode.Tag = mode;
                modeNode.ContextMenuStrip = modeContextMenu;
                foreach (IModeElement element in mode.GetElements())
                {
                    AddModeElement(modeNode, element);
                }
            }
        }

        private void AddModeElement(TreeNode modeNode, IModeElement modeElement)
        {
            TreeNode node = CreateModeElementNode(modeElement);
            modeNode.Nodes.Add(node);
            IElement startElement = modeElement.StartElement;
            if (startElement is IGeneralElementContainer)
            {
                AddSubElements(node, (startElement as IGeneralElementContainer).GetGeneralElements());
            }
            else if (startElement is IBackgroundSounds)
            {
                AddSubElements(node, (startElement as IBackgroundSounds).GetElements());
            }
        }

        private void AddSubElements(TreeNode parent, IList<IContainerElement> subElements)
        {
            foreach (IContainerElement subElement in subElements)
            {
                IElement innerElement = subElement.InnerElement;
                if (innerElement is IFileElement)
                {
                    continue;
                }
                TreeNode node = CreateElementNode(innerElement);
                parent.Nodes.Add(node);
                if (innerElement is IGeneralElementContainer)
                {
                    AddSubElements(node, (innerElement as IGeneralElementContainer).GetGeneralElements());
                }
            }
        }

        private void AddSubElements(TreeNode parent, IList<IBackgroundSoundChoice> subElements)
        {
            foreach (IBackgroundSoundChoice subElement in subElements)
            {
                TreeNode node = CreateElementNode(subElement);
                parent.Nodes.Add(node);
            }
        }

        private TreeNode CreateModeElementNode(IModeElement modeElement)
        {
            TreeNode node = CreateElementNode(modeElement.StartElement);
            node.Text = modeElement.GetNodeTitle();
            node.Tag = modeElement;
            return node;
        }

        private TreeNode CreateElementNode(IElement element)
        {
            TreeNode node = new TreeNode(element.Title);
            node.Tag = element;
            if (element is IBackgroundSounds)
            {
                node.ContextMenuStrip = bgSoundsContextMenu;
            }
            else if (element is IBackgroundSoundChoice)
            {
                node.ContextMenuStrip = elementContextMenu;
            }
            else if (element is IRandomBackgroundMusicList)
            {
                node.ContextMenuStrip = elementContextMenu;
            }
            else if (element is ISequentialBackgroundMusicList)
            {
                node.ContextMenuStrip = elementContextMenu;
            }
            else if (element is IGeneralElementContainer)
            {
                node.ContextMenuStrip = containerContextMenu;
            }
            return node;
        }

        private static IElement GetElement(TreeNode node)
        {
            if (node.Tag is IModeElement)
            {
                return (node.Tag as IModeElement).StartElement;
            }
            else
            {
                return node.Tag as IElement;
            }
        }

        private IProject m_Project;

        private TreeNode m_SelectedNode;
        private TreeNode SelectedNode
        {
            get
            {
                return m_SelectedNode;
            }

            set
            {
                m_SelectedNode = value;
                setKeyButton.Enabled = (m_SelectedNode.Tag is IMode) || (m_SelectedNode.Tag is IModeElement);
            }
        }


        private bool cancelExpand = false;

        private void projectTree_MouseDown(object sender, MouseEventArgs e)
        {
            SelectedNode = projectTree.GetNodeAt(e.X, e.Y);
            cancelExpand = e.Clicks > 1;
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RenameProject();
        }

        public void InitNewProject()
        {
            SelectedNode = projectTree.Nodes[0];
            AddMode(false);
            TreeNode modeNode = SelectedNode;
            m_AfterEditAction = () =>
                {
                    m_AfterEditAction = null;
                    SelectedNode = modeNode;
                    RenameMode();
                };
            SelectedNode = projectTree.Nodes[0];
            RenameProject();
        }

        public void RenameProject()
        {
            TreeNode projectNode = projectTree.Nodes[0];
            projectTree.SelectedNode = projectNode;
            projectTree.LabelEdit = true;
            if (!projectNode.IsEditing)
                projectNode.BeginEdit();
        }

        private void projectTree_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            String text = e.Label;
            if (e.Node.Tag == m_Project)
            {
                if (e.Label == null)
                    return;
                Actions.Actions.Instance.AddNew(new RenameProjectAction(e.Node, text));
            }
            else if (e.Node.Tag is IMode)
            {
                if (e.Label == null)
                {
                    e.Node.Text = (e.Node.Tag as IMode).GetNodeTitle();
                    return;
                }
                IMode mode = e.Node.Tag as IMode;
                Actions.Actions.Instance.AddNew(new RenameModeAction(e.Node, text));
                e.CancelEdit = true; // the text is already changed by the action
                // TODO: check for empty or equal titles, output warning
            }
            else if (e.Node.Tag is IModeElement)
            {
                if (e.Label == null)
                {
                    e.Node.Text = (e.Node.Tag as IModeElement).GetNodeTitle();
                    return;
                }
                IModeElement modeElement = e.Node.Tag as IModeElement;
                Actions.Actions.Instance.AddNew(new RenameModeElementAction(e.Node, text));
                e.CancelEdit = true; // the text is already changed by the action
            }
            else
            {
                if (e.Label == null)
                {
                    return;
                }
                IElement element = e.Node.Tag as IElement;
                Actions.Actions.Instance.AddNew(new RenameElementAction(e.Node, text));
            }
            projectTree.LabelEdit = false;
            if (m_AfterEditAction != null)
                m_AfterEditAction();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AddMode(true);
        }

        private void AddMode(bool immediateRename)
        {
            TreeNode modeNode;
            Actions.Actions.Instance.AddNew(new AddModeAction(SelectedNode, out modeNode));
            modeNode.ContextMenuStrip = modeContextMenu;
            SelectedNode = modeNode;
            projectTree.SelectedNode = modeNode;
            if (immediateRename)
            {
                RenameMode();
            }
        }

        private void RenameMode()
        {
            projectTree.SelectedNode = SelectedNode;
            projectTree.LabelEdit = true;
            if (!SelectedNode.IsEditing)
            {
                SelectedNode.Text = (SelectedNode.Tag as IMode).Title;
                SelectedNode.BeginEdit();
            }
        }

        private void AddRandomPlaylist()
        {
            String name = StringResources.NewPlaylist;
            IRandomBackgroundMusicList element = DataModule.ElementFactory.CreateRandomBackgroundMusicList(name);
            if (SelectedNode.Tag is IMode)
            {
                AddModeElement(element, name);
            }
            else
            {
                AddContainerElement(element);
            }
            RenameElement();
        }

        private void AddSequentialPlaylist()
        {
            String name = StringResources.NewPlaylist;
            ISequentialBackgroundMusicList element = DataModule.ElementFactory.CreateSequentialBackgroundMusicList(name);
            if (SelectedNode.Tag is IMode)
            {
                AddModeElement(element, name);
            }
            else
            {
                AddContainerElement(element);
            }
            RenameElement();
        }

        private void AddBackgroundSounds()
        {
            String name = StringResources.NewBackgroundSounds;
            IBackgroundSounds element = DataModule.ElementFactory.CreateBackgroundSounds(name);
            if (SelectedNode.Tag is IMode)
            {
                AddModeElement(element, name);
            }
            else
            {
                AddContainerElement(element);
            }
            m_AfterEditAction = () =>
                {
                    m_AfterEditAction = null;
                    AddSoundChoice(true);
                };
            RenameElement();
        }

        private void AddSoundChoice(bool renameImmediately)
        {
            bool oldListen = listenForContainerChanges;
            listenForContainerChanges = false;
            String name = StringResources.NewSoundChoice;
            TreeNode node;
            Actions.Actions.Instance.AddNew(new Actions.AddSoundChoiceAction(SelectedNode, 
                GetElement(SelectedNode) as IBackgroundSounds, 
                name, CreateElementNode, out node));
            SelectedNode.Expand();
            SelectedNode = node;
            if (renameImmediately)
                RenameElement();
            listenForContainerChanges = oldListen;
        }

        private void AddParallelList()
        {
            bool oldListen = listenForContainerChanges;
            listenForContainerChanges = false;
            String name = StringResources.NewParallelList;
            IElementContainer<IParallelElement> element = DataModule.ElementFactory.CreateParallelContainer(name);
            if (SelectedNode.Tag is IMode)
            {
                AddModeElement(element, name);
            }
            else
            {
                AddContainerElement(element);
            }
            RenameElement();
            listenForContainerChanges = oldListen;
        }

        private void AddSequentialList()
        {
            bool oldListen = listenForContainerChanges;
            listenForContainerChanges = false;
            String name = StringResources.NewSequentialList;
            ISequentialContainer element = DataModule.ElementFactory.CreateSequentialContainer(name);
            if (SelectedNode.Tag is IMode)
            {
                AddModeElement(element, name);
            }
            else
            {
                AddContainerElement(element);
            }
            RenameElement();
            listenForContainerChanges = oldListen;
        }

        private void AddRandomList()
        {
            bool oldListen = listenForContainerChanges;
            listenForContainerChanges = false;
            String name = StringResources.NewRandomList;
            IElementContainer<IChoiceElement> element = DataModule.ElementFactory.CreateChoiceContainer(name);
            if (SelectedNode.Tag is IMode)
            {
                AddModeElement(element, name);
            }
            else
            {
                AddContainerElement(element);
            }
            RenameElement();
            listenForContainerChanges = oldListen;
        }

        private void AddScenario()
        {
            bool oldListen = listenForContainerChanges;
            listenForContainerChanges = false;
            String name = StringResources.NewScenario;
            IElementContainer<IParallelElement> element = DataModule.ElementFactory.CreateParallelContainer(name);
            AddModeElement(element, name);
            TreeNode scenarioNode = SelectedNode;
            IModeElement modeElement = scenarioNode.Tag as IModeElement;
            modeElement.Trigger = DataModule.ElementFactory.CreateNoTrigger();
            modeElement.Trigger.StopSounds = true;
            String name2 = StringResources.Music;
            IRandomBackgroundMusicList element2 = DataModule.ElementFactory.CreateRandomBackgroundMusicList(name2);
            AddContainerElement(element2);
            SelectedNode = scenarioNode;
            String name3 = StringResources.Sounds;
            IBackgroundSounds element3 = DataModule.ElementFactory.CreateBackgroundSounds(name3);
            AddContainerElement(element3);
            AddSoundChoice(false);
            TreeNode soundChoiceNode = SelectedNode;
            SelectedNode = scenarioNode;
            m_AfterEditAction = () =>
                {
                    m_AfterEditAction = null;
                    SelectedNode = soundChoiceNode;
                    RenameElement();
                };
            RenameElement();
            listenForContainerChanges = oldListen;
        }

        private void AddModeElement(IElement startElement, String title)
        {
            IModeElement modeElement = DataModule.ElementFactory.CreateModeElement(title, startElement);
            TreeNode node = CreateModeElementNode(modeElement);
            Actions.Actions.Instance.AddNew(new AddModeElementAction(SelectedNode, modeElement, node));
            SelectedNode.Expand();
            SelectedNode = node;
        }

        private void AddContainerElement(IElement element)
        {
            bool oldListen = listenForContainerChanges;
            listenForContainerChanges = false;
            TreeNode node;
            Actions.Actions.Instance.AddNew(new AddElementAction(SelectedNode, 
                GetElement(SelectedNode) as IGeneralElementContainer, element, CreateElementNode, out node));
            SelectedNode.Expand();
            SelectedNode = node;
            listenForContainerChanges = oldListen;
        }

        private void RenameElement()
        {
            projectTree.SelectedNode = SelectedNode;
            projectTree.LabelEdit = true;
            if (!SelectedNode.IsEditing)
            {
                SelectedNode.Text = (SelectedNode.Tag as IElement).Title;
                SelectedNode.BeginEdit();
            }
        }

        private void addScenarioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddScenario();
        }

        private void renameToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            RenameMode();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteMode();
        }

        private void DeleteMode()
        {
            IMode mode = SelectedNode as IMode;
            if (SelectedNode != null)
            {
                Actions.Actions.Instance.AddNew(new DeleteModeAction(SelectedNode));
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            SelectModeKey();
        }

        private void SelectModeKey()
        {
            int keyCode = 0;
            DialogResult result = Dialogs.KeyDialog.Show(this, out keyCode);
            if (result != DialogResult.Cancel)
            {
                Actions.Actions.Instance.AddNew(new Actions.SetModeKeyAction(SelectedNode, keyCode));
            }
        }

        private void SelectModeElementKey()
        {
            int keyCode = 0;
            DialogResult result = Dialogs.KeyDialog.Show(this, out keyCode);
            if (result != DialogResult.Cancel)
            {
                IModeElement modeElement = SelectedNode.Tag as IModeElement;
                IKeyTrigger keyTrigger = Data.DataModule.ElementFactory.CreateKeyTrigger();
                keyTrigger.TargetElementId = modeElement.Id;
                keyTrigger.KeyCode = keyCode;
                if (modeElement.Trigger != null && modeElement.Trigger.TriggerType == TriggerType.Key)
                {
                    IKeyTrigger oldTrigger = modeElement.Trigger as IKeyTrigger;
                    keyTrigger.StopSounds = oldTrigger.StopSounds;
                    keyTrigger.StopMusic = oldTrigger.StopMusic;
                }
                else
                {
                    keyTrigger.StopMusic = keyTrigger.StopSounds = false;
                }
                Actions.Actions.Instance.AddNew(new Actions.SetModeElementTriggerAction(modeElement, keyTrigger));
            }
        }

        private void ElementChanged(int elementId, ElementChanges.ChangeType changeType)
        {
            if (changeType == ElementChanges.ChangeType.TriggerChanged)
            {
                foreach (TreeNode node in projectTree.Nodes[0].Nodes)
                {
                    if (node.Tag is IMode)
                    {
                        foreach (TreeNode node2 in node.Nodes)
                        {
                            IModeElement modeElement = node2.Tag as IModeElement;
                            if (modeElement != null && modeElement.Id == elementId)
                            {
                                node2.Text = modeElement.GetNodeTitle();
                                return;
                            }
                        }
                    }
                }
            }
            else if (changeType == ElementChanges.ChangeType.Changed && listenForContainerChanges)
            {
                TreeNode node = FindNodeForElement(elementId, projectTree.Nodes[0]);
                if (node != null && GetElement(node) is IGeneralElementContainer)
                {
                    node.Nodes.Clear();
                    AddSubElements(node, (GetElement(node) as IGeneralElementContainer).GetGeneralElements());
                }
            }
        }

        private TreeNode FindNodeForElement(int elementId, TreeNode node)
        {
            if (node.Tag is IElement && (node.Tag as IElement).Id == elementId)
                return node;
            if (GetElement(node) != null && GetElement(node).Id == elementId)
                return node;
            foreach (TreeNode subNode in node.Nodes)
            {
                TreeNode foundNode = FindNodeForElement(elementId, subNode);
                if (foundNode != null)
                {
                    return foundNode;
                }
            }
            return null;
        }

        private void EditModeElementTrigger()
        {
            IModeElement element = SelectedNode.Tag as IModeElement;
            ElementEditors.Editors.ShowTriggerEditor(element, DockPanel);
        }

        private void containerContextMenu_Opening(object sender, CancelEventArgs e)
        {
            UpdateContextMenuDueToPlaying(sender as ContextMenuStrip);
            UpdateVisiblityDueToModeElement(sender as ContextMenuStrip);
        }

        private void elementContextMenu_Opening(object sender, CancelEventArgs e)
        {
            UpdateContextMenuDueToPlaying(sender as ContextMenuStrip);
            UpdateVisiblityDueToModeElement(sender as ContextMenuStrip);
        }

        private void addRandomPlaylistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddRandomPlaylist();
        }

        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DeleteElement();
        }

        private void DeleteElement()
        {
            bool oldListen = listenForContainerChanges;
            listenForContainerChanges = false;
            if (SelectedNode.Parent.Tag is IMode)
            {
                Actions.Actions.Instance.AddNew(new DeleteModeElementAction(SelectedNode));
            }
            else if (SelectedNode.Parent.Tag is IBackgroundSounds)
            {
                Actions.Actions.Instance.AddNew(new DeleteBackgroundSoundChoiceAction(SelectedNode));
            }
            else
            {
                Actions.Actions.Instance.AddNew(new DeleteElementAction(SelectedNode));
            }
            listenForContainerChanges = oldListen;
        }

        private void EditElement(IElement element)
        {
            ElementEditors.Editors.ShowEditor(element, DockPanel);
        }

        private void deleteToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            DeleteElement();
        }

        private void renameToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            RenameElement();
        }

        private void renameToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            RenameElement();
        }

        private void selectKeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditModeElementTrigger();
        }

        private void selectKeyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            EditModeElementTrigger();
        }

        private void addSequentialPlaylistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddSequentialPlaylist();
        }

        private void addBackgroundSoundsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBackgroundSounds();
        }

        private void addParallelElementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddParallelList();
        }

        private void addSequentialElementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddSequentialList();
        }

        private void addChoiceListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddRandomList();
        }

        private void addChoiceListToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AddRandomList();
        }

        private void addSequentialElementToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AddSequentialList();
        }

        private void addParallelElementToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AddParallelList();
        }

        private void addBackgroundSoundsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AddBackgroundSounds();
        }

        private void addSequentialPlaylistToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AddSequentialPlaylist();
        }

        private void addRandomPlaylistToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AddRandomPlaylist();
        }

        private void projectTree_DoubleClick(object sender, EventArgs e)
        {
            if (SelectedNode != null)
            {
                if (SelectedNode.Tag is IMode)
                {
                    SelectModeKey();
                }
                else if (SelectedNode.Tag is IModeElement && GetElement(SelectedNode) is IBackgroundSounds)
                {
                    EditModeElementTrigger();
                }
                else if (SelectedNode.Tag is IElement)
                {
                    EditElement(GetElement(SelectedNode));
                }
            }
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditElement(GetElement(SelectedNode));
        }

        private void editToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            EditElement(GetElement(SelectedNode));
        }

        private void projectTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (cancelExpand)
            {
                e.Cancel = true;
                cancelExpand = false;
            }
        }

        private void projectTree_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            if (cancelExpand)
            {
                e.Cancel = true;
                cancelExpand = false;
            }
        }

        private void renameToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            RenameElement();
        }

        private void deleteToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            DeleteElement();
        }

        private void addSoundChoiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddSoundChoice(true);
        }

        private void bgSoundsContextMenu_Opening(object sender, CancelEventArgs e)
        {
            UpdateContextMenuDueToPlaying(sender as ContextMenuStrip);
            UpdateVisiblityDueToModeElement(sender as ContextMenuStrip);
        }

        private void selectKeyToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            EditModeElementTrigger();
        }

        private IElement m_PlayedElement;

        private void playButton_Click(object sender, EventArgs e)
        {
            IElement element = GetElement(SelectedNode);
            if (element != null)
            {
                m_PlayedElement = element;
                Actions.Playing.Instance.PlayElement(element, this, () =>
                     {
                         m_PlayedElement = null;
                         stopButton.Enabled = false;
                         playButton.Enabled = PlayingPossible;
                     });
                stopButton.Enabled = true;
                playButton.Enabled = false;
            }
        }

        private bool PlayingPossible
        {
            get
            {
                if (m_PlayedElement != null)
                    return false;
                if (SelectedNode == null)
                    return false;
                IElement element = GetElement(SelectedNode);
                if (element == null)
                    return false;
                if (Actions.Playing.Instance.IsElementOrSubElementPlaying(element))
                    return false;
                return true;
            }
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            if (m_PlayedElement != null)
            {
                Actions.Playing.Instance.StopElement(m_PlayedElement);
            }
        }

        private void projectTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            playButton.Enabled = PlayingPossible;
            AddChangeListener();
        }

        private void projectTree_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            RemoveChangeListener();
        }

        private void AddChangeListener()
        {
            if (SelectedNode != null)
            {
                IElement element = GetElement(SelectedNode);
                if (element != null)
                {
                    Actions.ElementChanges.Instance.AddListener(element.Id, UpdateAfterElementChange);
                }
            }
        }

        
        private void RemoveChangeListener()
        {
            if (SelectedNode != null)
            {
                IElement element = GetElement(SelectedNode);
                if (element != null)
                {
                    Actions.ElementChanges.Instance.RemoveListener(element.Id, UpdateAfterElementChange);
                }
            }
        }

        private void UpdateAfterElementChange(int id, Actions.ElementChanges.ChangeType changeType)
        {
            if (SelectedNode != null)
            {
                IElement element = GetElement(SelectedNode);
                if (element != null)
                {
                    if (changeType == ElementChanges.ChangeType.Stopped || changeType == ElementChanges.ChangeType.Played)
                    {
                        playButton.Enabled = PlayingPossible;
                    }
                }
            }
        }

        private void modeContextMenu_Opening(object sender, CancelEventArgs e)
        {
            UpdateContextMenuDueToPlaying(sender as ContextMenuStrip);
        }

        private static bool IsNodePlaying(TreeNode node)
        {
            IElement element = GetElement(node);
            if (element != null && Actions.Playing.Instance.IsElementOrSubElementPlaying(element))
            {
                return true;
            }
            else if (node.Tag is IMode)
            {
                foreach (TreeNode subNode in node.Nodes)
                {
                    if (IsNodePlaying(subNode))
                        return true;
                }
            }
            return false;
        }

        private void UpdateContextMenuDueToPlaying(ContextMenuStrip contextMenu)
        {
            bool disable = (SelectedNode != null && IsNodePlaying(SelectedNode));
            foreach (ToolStripItem item in contextMenu.Items)
            {
                if (item is ToolStripMenuItem)
                    item.Enabled = !disable;
            }
        }

        private void UpdateVisiblityDueToModeElement(ContextMenuStrip contextMenu)
        {
            bool visible = SelectedNode != null && SelectedNode.Tag is IModeElement;
            foreach (ToolStripItem item in contextMenu.Items)
            {
                if ("OnlyMode".Equals(item.Tag))
                {
                    item.Visible = visible;
                }
            }
        }

        private void projectTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                if (SelectedNode != null)
                {
                    if (SelectedNode == projectTree.Nodes[0])
                        RenameProject();
                    else if (SelectedNode.Tag is IMode)
                        RenameMode();
                    else
                        RenameElement();
                }
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            SelectModeElementKey();
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            SelectModeElementKey();
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            SelectModeElementKey();
        }

        private void setKeyButton_Click(object sender, EventArgs e)
        {
            if (SelectedNode.Tag is IMode)
            {
                SelectModeKey();
            }
            else
            {
                SelectModeElementKey();
            }
        }
    }
}