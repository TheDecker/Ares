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
using System.Text;

namespace Ares.Data
{
    /// <summary>
    /// Provides access to the main objects of the data module, which are singletons.
    /// </summary>
    public static class DataModule
    {
        /// <summary>
        /// Returns the project manager.
        /// </summary>
        public static IProjectManager ProjectManager { get { return s_ProjectManager;  } }

        /// <summary>
        /// Returns the element factory.
        /// </summary>
        public static IElementFactory ElementFactory { get { return s_ElementFactory; } }

        /// <summary>
        /// Returns the element repository.
        /// </summary>
        public static IElementRepository ElementRepository { get { return s_ElementRepository; } }

        internal static ElementFactory TheElementFactory { get { return s_ElementFactory; } }
        internal static ElementRepository TheElementRepository { get { return s_ElementRepository; } }

        private static ProjectManager s_ProjectManager = new ProjectManager();

        private static ElementFactory s_ElementFactory = new ElementFactory();

        private static ElementRepository s_ElementRepository = new ElementRepository();
    }
}
