using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

using UnconfiguredProject = Microsoft.VisualStudio.ProjectSystem.UnconfiguredProject;

namespace MyCompany.LanguageServices.VHDL
{
	class VHDLSimpleParser
		: VHDLParserImplementation
	{
		private string m_filename = null;
		private FileSystemWatcher m_watcher = null;

		public VHDLSimpleParser(VHDLDocument document)
			: base(document)
		{
			m_filename = Document.Filepath;
			m_watcher = new FileSystemWatcher(Path.GetDirectoryName(m_filename));
			m_watcher.Changed += OnFileChanged;
			m_watcher.EnableRaisingEvents = true;

			RequestParse();
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			if (string.Compare(e.FullPath, m_filename, StringComparison.CurrentCultureIgnoreCase) == 0)
			{
				RequestParse();
			}
		}

		private bool m_dirty = true;
		protected override void MarkDirty()
		{
			m_dirty = true;
			TryReparse();
		}
		protected override ITextSnapshot GetSnapshot()
		{
			return null;
		}

		protected override string GetText()
		{
			return new StreamReader(new FileStream(m_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)).ReadToEnd();
		}

		private int m_parsing = 0;
		private void TryReparse()
		{
			if (Interlocked.CompareExchange(ref m_parsing, 1, 0) == 0)
			{
				try
				{
					Task task = Task.Run(() =>
					{
						while (m_dirty)
						{
							m_dirty = false;
							ReParseImpl();
						}
					});
					_ = task.ContinueWith(_ => m_parsing = 0);
				}
				catch
				{
					m_parsing = 0;
					throw;
				}
			}
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				m_watcher.Dispose();
			}

			base.Dispose(disposing);
		}

	}
}
