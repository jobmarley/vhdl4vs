/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs
{
	class ParserChangedEventArgs
			: EventArgs
	{
		public ParserChangedEventArgs(IVHDLParser oldParser, IVHDLParser newParser)
		{
			OldParser = oldParser;
			NewParser = newParser;
		}
		public IVHDLParser OldParser { get; private set; }
		public IVHDLParser NewParser { get; private set; }
	}
	class VHDLDocument
		: IDisposable
	{
		public VHDLDocument(VHDLDocumentTable documentTable)
		{
			DocumentTable = documentTable;
			Parser = new VHDLProxyParser();
		}
		public ITextBuffer TextBuffer
		{
			get
			{
				if (Parser?.Parser is VHDLBackgroundParser)
					return (Parser.Parser as VHDLBackgroundParser).TextBuffer;
				return null;
			}
		}
		public VHDLDocumentTable DocumentTable { get; private set; } = null;
		public ITextDocument TextDocument { get; set; } = null;
		public VHDLProject Project { get; set; } = null;
		public VHDLProxyParser Parser { get; set; } = null;
		public string Filepath { get; set; }
		public bool IsOpen()
		{
			return TextDocument != null;
		}
		private bool m_disposed = false;
		public virtual void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				Parser.Dispose();
			}

			m_disposed = true;
		}
	}
	class VHDLDocumentEventArgs
		: EventArgs
	{
		public VHDLDocumentEventArgs(VHDLDocument doc)
		{
			Document = doc;
		}
		public VHDLDocument Document { get; private set; }
	}
}
