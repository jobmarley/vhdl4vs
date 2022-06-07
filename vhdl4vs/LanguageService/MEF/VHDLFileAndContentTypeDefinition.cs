/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

/*
 * Defines content type for MyScript.
 * This is used by other MEF component to identify the content they are used for.
 * */
namespace vhdl4vs
{
    internal static class VHDLFileAndContentTypeDefinition
    {
        [Export]
        [Name("VHDL")]
        [BaseDefinition("code")]
        //[BaseDefinition("projection")]
        internal static ContentTypeDefinition VHDLContentTypeDefinition;

        [Export]
        [FileExtension(".vhd;.vhdl")]
        [ContentType("VHDL")]
        internal static FileExtensionToContentTypeDefinition VHDLFileExtensionDefinition;
    }
}
