﻿/*
 Copyright (c) 2015 [Joerg Ruedenauer]
 
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
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Ares.Players;
using Ares.CommonGUI;

namespace Ares.Player
{
	partial class MessagesForm : Ares.CommonGUI.InvokableForm
    {

        public MessagesForm()
        {
			InitializeComponent();
            Messages.Instance.MessageReceived += new MessageReceivedHandler(MessageReceived);
            filterBox.SelectedIndex = Ares.Settings.Settings.Instance.MessageFilterLevel;
        }

	    private void RefillGrid()
        {
            messagesGrid.SuspendLayout();
            messagesGrid.Rows.Clear();
            foreach (Ares.Players.Message m in Messages.Instance.GetAllMessages())
            {
                AddMessage(m);
            }
            messagesGrid.ResumeLayout();
        }

        private void MessageReceived(Ares.Players.Message m)
        {
            if (IsDisposed)
                return;
			if (this.IsInvokeRequired())
            {
                BeginInvoke(new MethodInvoker(() => AddMessage(m)));
            }
            else
            {
                AddMessage(m);
            }
        }

        private void AddMessage(Ares.Players.Message m)
        {
            if (m.Type >= FilterLevel)
            {
                messagesGrid.Rows.Add(GetIcon(m.Type), m.Text);
            }
        }

        private static object GetIcon(MessageType type)
        {
            switch (type)
            {
                case MessageType.Debug:
                    return Properties.Resources.gear;
                case MessageType.Info:
                    return Properties.Resources.eventlogInfo.ToBitmap();
                case MessageType.Warning:
                    return Properties.Resources.eventlogWarn.ToBitmap();
                case MessageType.Error:
                    return Properties.Resources.eventlogError.ToBitmap();
                default:
                    return null;
            }
        }

        public MessageType FilterLevel { get; set; }

        private void filterBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FilterLevel = (MessageType)filterBox.SelectedIndex;
            Ares.Settings.Settings.Instance.MessageFilterLevel = filterBox.SelectedIndex;
            RefillGrid();
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            messagesGrid.Rows.Clear();
            Messages.Instance.Clear();
        }
    }
}
