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
using System.Linq;

using Ares.Data;

namespace Ares.Editor
{
    class DragAndDrop
    {
        public static IEnumerable<IElement> GetElementsFromDroppedItems(List<DraggedItem> draggedItems)
        {
            return DoGetElementsFromDroppedItems(draggedItems, Settings.Settings.Instance.MusicDirectory, Settings.Settings.Instance.SoundDirectory, null, null);
        }

        public static System.Threading.Tasks.Task<IList<IElement>> GetElementsFromDroppedItemsAsync(List<DraggedItem> draggedItems, System.Threading.CancellationTokenSource tokenSource, Ares.ModelInfo.IProgressMonitor progressMonitor)
        {
            String musicDirectory = Settings.Settings.Instance.MusicDirectory;
            String soundDirectory = Settings.Settings.Instance.SoundDirectory;
            return System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var result = new List<IElement>(DoGetElementsFromDroppedItems(draggedItems, musicDirectory, soundDirectory, tokenSource, progressMonitor));
                        return (IList<IElement>)result;
                    }
                    catch (OperationCanceledException)
                    {
                        return null;
                    }
                    catch (AggregateException)
                    {
                        return null;
                    }
                });
        }

        private static IEnumerable<IElement> DoGetElementsFromDroppedItems(List<DraggedItem> draggedItems, String musicDirectory, String soundDirectory, 
            System.Threading.CancellationTokenSource tokenSource, Ares.ModelInfo.IProgressMonitor progressMonitor)
        {
            Ares.TagsImport.SequentialProgressMonitor monitor1 = null, monitor2 = null;
            if (progressMonitor != null)
            {
                monitor1 = new TagsImport.SequentialProgressMonitor(progressMonitor, 0.1, 9.9);
            }
            Dictionary<string, DraggedItem> uniqueItems = new Dictionary<string, DraggedItem>();
            foreach (DraggedItem item in draggedItems)
            {
                AddItemsToSet(uniqueItems, item, musicDirectory, soundDirectory, tokenSource);
                if (tokenSource != null)
                    tokenSource.Token.ThrowIfCancellationRequested();
                if (monitor1 != null)
                    monitor1.IncreaseProgress(100.0 / draggedItems.Count);
            }
            if (progressMonitor != null)
                monitor2 = new TagsImport.SequentialProgressMonitor(progressMonitor, 10.0, 90.0);
            foreach (DraggedItem item in uniqueItems.Values)
            {
                yield return CreateFileElement(item, musicDirectory, soundDirectory);
                if (tokenSource != null)
                    tokenSource.Token.ThrowIfCancellationRequested();
                if (monitor2 != null)
                    monitor2.IncreaseProgress(100.0 / uniqueItems.Count);
            }
        }

        private static IElement CreateFileElement(DraggedItem item, String musicDirectory, String soundDirectory)
        {
            IFileElement element = Ares.Data.DataModule.ElementFactory.CreateFileElement(item.RelativePath, item.ItemType == FileType.Music ? SoundFileType.Music : SoundFileType.SoundEffect);
            String dir = item.ItemType == FileType.Music ? musicDirectory : soundDirectory;
            String path = System.IO.Path.Combine(dir, item.RelativePath);
            Un4seen.Bass.AddOn.Tags.TAG_INFO tag = Un4seen.Bass.AddOn.Tags.BassTags.BASS_TAG_GetFromFile(path, true, true);
            if (tag != null)
            {
                element.Title = tag.title;
            }
            if (String.IsNullOrEmpty(element.Title))
            {
                element.Title = System.IO.Path.GetFileNameWithoutExtension(path);
            }
            return element;
        }

        private static void AddItemsToSet(Dictionary<String, DraggedItem> uniqueItems, DraggedItem item, String musicDirectory, String soundDirectory, System.Threading.CancellationTokenSource tokenSource)
        {
            String baseDir = item.ItemType == FileType.Music ? musicDirectory : soundDirectory;
            String path = System.IO.Path.Combine(baseDir, item.RelativePath);
            if (item.NodeType == DraggedItemType.Directory)
            {
                if (System.IO.Directory.Exists(path))
                {
                    foreach (String file in FileSearch.GetFilesInDirectory(item.ItemType, path, true))
                    {
                        AddItemsToSet(uniqueItems, new DraggedItem { NodeType = DraggedItemType.File, ItemType = item.ItemType, RelativePath = file.Substring(baseDir.Length + 1) }, 
                            musicDirectory, soundDirectory, tokenSource);
                        if (tokenSource != null)
                            tokenSource.Token.ThrowIfCancellationRequested();
                    }
                }
            }
            else
            {
                String key = System.IO.Path.GetFullPath(path);
                if (!uniqueItems.ContainsKey(path))
                {
                    uniqueItems[key] = item;
                }
            }
        }

    }

    class Win32ContextMenu
    {
        [System.Runtime.InteropServices.DllImport("user32")]
        public static extern int TrackPopupMenu(IntPtr hMenu, int wFlags, int x, int y, int nReserved, IntPtr hwnd, IntPtr lprc);

        public const int TPM_RETURNCMD = 0x0100;
        public const int TPM_RIGHTBUTTON = 0x0002;
    }
}
