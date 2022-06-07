/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;

namespace vhdl4vs
{
	[Export(typeof(ISignatureHelpSourceProvider))]
	[Name("VHDL Signature Help source")]
	[Order(Before = "default")]
	[ContentType("VHDL")]
	internal class VHDLSignatureHelpSourceProvider
		: ISignatureHelpSourceProvider
	{
		[Import]
		internal ITextDocumentFactoryService DocumentService { get; set; }
		[Import]
		internal VHDLDocumentTable VHDLDocumentTable { get; set; }

		public ISignatureHelpSource TryCreateSignatureHelpSource(ITextBuffer textBuffer)
		{
			return new VHDLSignatureHelpSource(VHDLDocumentTable, textBuffer, DocumentService);
		}
	}
}
