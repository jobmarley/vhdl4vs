using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Antlr4.Runtime;

using UnconfiguredProject = Microsoft.VisualStudio.ProjectSystem.UnconfiguredProject;

namespace MyCompany.LanguageServices.VHDL
{
	class VHDLBackgroundParser
		: VHDLParserImplementation
	{
		private TaskScheduler m_taskScheduler = null;
		private Timer m_timer = null;
		private TimeSpan m_reparseDelay;
		private DateTimeOffset m_lastEdit;

		private WeakReference<ITextBuffer> m_textBuffer = null;
		public ITextBuffer TextBuffer
		{
			get
			{
				ITextBuffer target;
				if (m_textBuffer.TryGetTarget(out target))
					return target;
				else
					return null;
			}
		}

		public VHDLBackgroundParser(
			ITextBuffer textBuffer,
			TaskScheduler taskScheduler,
			VHDLDocument document)
			: base(document)
		{
			m_textBuffer = new WeakReference<ITextBuffer>(textBuffer);

			m_taskScheduler = taskScheduler;
			m_reparseDelay = TimeSpan.FromMilliseconds(300);
			m_timer = new Timer(ParseTimerCallback, null, m_reparseDelay, m_reparseDelay);
			m_lastEdit = DateTimeOffset.MinValue;

			textBuffer.PostChanged += TextBufferPostChanged;

			UseDeepAnalysis = true;
			RequestParse();
		}
		void TextBufferPostChanged(object sender, EventArgs e)
		{
			RequestParse();
		}
		public TimeSpan ReparseDelay
		{
			get
			{
				return m_reparseDelay;
			}

			set
			{
				TimeSpan originalDelay = m_reparseDelay;
				try
				{
					m_reparseDelay = value;
					m_timer.Change(value, value);
				}
				catch (ArgumentException)
				{
					m_reparseDelay = originalDelay;
				}
			}
		}

		private bool m_dirty = true;
		protected override void MarkDirty()
		{
			m_dirty = true;
			m_lastEdit = DateTimeOffset.Now;

			//if (resetTimer)
			//	m_timer.Change(m_reparseDelay, m_reparseDelay);
		}
		private void ParseTimerCallback(object state)
		{
			if (TextBuffer == null)
			{
				Dispose();
				return;
			}

			TryReparse(m_dirty);
		}
		private int m_parsing = 0;
		private void TryReparse(bool forceReparse)
		{
			if (!m_dirty && !forceReparse)
				return;

			if (DateTimeOffset.Now - m_lastEdit < ReparseDelay)
				return;

			if (Interlocked.CompareExchange(ref m_parsing, 1, 0) == 0)
			{
				try
				{
					m_dirty = false;
					Task task = Task.Factory.StartNew(ReParseImpl, CancellationToken.None, TaskCreationOptions.None, m_taskScheduler);
					task.ContinueWith(_ => m_parsing = 0);
				}
				catch
				{
					m_parsing = 0;
					throw;
				}
			}
		}

		protected override ITextSnapshot GetSnapshot()
		{
			return TextBuffer?.CurrentSnapshot;
		}

		protected override string GetText()
		{
			return TextBuffer?.CurrentSnapshot?.GetText();
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				m_timer.Dispose();
			}

			base.Dispose(disposing);
		}

	}
}
