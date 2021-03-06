/*
 Copyright (c) 2011 [Joerg Ruedenauer]
 
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
package ares.controller.control;

import java.awt.event.ActionEvent;

import javax.swing.AbstractAction;

import ares.controllers.data.KeyStroke;
import ares.controllers.control.Control;

public class KeyAction extends AbstractAction {
  public KeyAction(KeyStroke key) {
    this.key = key;
  }
  
  public void actionPerformed(ActionEvent e) {
	  if (sEnabled) {
		  Control.getInstance().sendKey(key);
	  }
  }
  
  public static void setKeysEnabled(boolean enabled) {
	  sEnabled = enabled;
  }
  
  public static boolean areKeysEnabled() {
	  return sEnabled;
  }
  
  private static boolean sEnabled = true;
  
  private KeyStroke key; 
}