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
