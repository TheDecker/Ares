/*
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
package ares.controller.gui;

import java.util.HashMap;
import java.util.Map;

import javax.swing.AbstractAction;
import javax.swing.AbstractButton;

import ares.controller.control.ComponentKeys;
import ares.controller.control.ModeCommandAction;
import ares.controllers.data.Command;
import ares.controllers.data.Mode;

final class ModeFrame extends FrameController implements CommandsPanelCreator.IActionCreator {
  
  private Map<Integer, AbstractButton> pushButtons = new HashMap<Integer, AbstractButton>();
  
  public ModeFrame(Mode mode) {
    super(mode.getTitle());
    ComponentKeys.addAlwaysAvailableKeys(getRootPane());
    initialize(mode);
  }
  
  protected void initialize(Mode mode) {
    this.setContentPane(CommandsPanelCreator.createPanel(mode.getCommands(), this, getRootPane(), 2, null, null));
    this.setDefaultCloseOperation(javax.swing.JFrame.DISPOSE_ON_CLOSE);                 
    pack();
  }
	  
  public void dispose() {
    for (Integer key : pushButtons.keySet()) {
    	CommandButtonMapping.getInstance().unregisterButton(key);
    }
    super.dispose();
  }

  public AbstractAction createAction(final Command command, final AbstractButton button) {
    CommandButtonMapping.getInstance().registerButton(command.getId(), button);
    pushButtons.put(command.getId(), button);
    return new ModeCommandAction(command.getId());
  }
  
}
