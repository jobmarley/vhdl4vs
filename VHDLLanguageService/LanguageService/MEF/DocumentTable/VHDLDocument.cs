using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompany.LanguageServices.VHDL
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
