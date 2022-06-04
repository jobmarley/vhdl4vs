using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

using Run = System.Windows.Documents.Run;

namespace MyCompany.LanguageServices.VHDL
{
	internal class VHDLParameter
		: IParameter
	{
		public string Documentation { get; private set; }
		public Span Locus { get; private set; }
		public string Name { get; private set; }
		public ISignature Signature { get; private set; }
		public Span PrettyPrintedLocus { get; private set; }

		public VHDLParameter(string documentation, Span locus, string name, ISignature signature)
		{
			Documentation = documentation;
			Locus = locus;
			Name = name;
			Signature = signature;
		}
	}

	class VHDLClassifactionSpan
	{
		public VHDLClassifactionSpan(Span span, string classificationType)
		{
			Span = span;
			ClassificationType = classificationType;
		}
		public Span Span { get; set; }
		public string ClassificationType { get; set; }
	}

	class VHDLClassifiedText
	{
		public VHDLClassifiedText()
		{
			Text = "";
			ClassificationSpans = new List<VHDLClassifactionSpan>();
		}
		public VHDLClassifiedText(string text)
		{
			Text = text;
			ClassificationSpans = new List<VHDLClassifactionSpan>();
		}
		public VHDLClassifiedText(string text, string classificationType)
		{
			Text = text;
			ClassificationSpans = new List<VHDLClassifactionSpan>() { new VHDLClassifactionSpan(new Span(0, text.Length), classificationType) };
		}
		public VHDLClassifiedText(string text, IEnumerable<VHDLClassifactionSpan> classificationSpans)
		{
			Text = text;
			ClassificationSpans = new List<VHDLClassifactionSpan>(ClassificationSpans);
		}

		public string Text { get; set; }
		public List<VHDLClassifactionSpan> ClassificationSpans { get; set; }

		public void Add(VHDLClassifiedText classifiedText)
		{
			IEnumerable<VHDLClassifactionSpan> spans = classifiedText.ClassificationSpans.Select(x => new VHDLClassifactionSpan(new Span(x.Span.Start + Text.Length, x.Span.Length), x.ClassificationType));
			ClassificationSpans.AddRange(spans);
			Text += classifiedText.Text;
		}
		public void AddRange(IEnumerable<VHDLClassifiedText> classifieds)
		{
			foreach (var c in classifieds)
				Add(c);
		}
		public void Add(string text, string classificationType)
		{
			ClassificationSpans.Add(new VHDLClassifactionSpan(new Span(Text.Length, text.Length), classificationType));
			Text += text;
		}
		public void Add(string text)
		{
			Text += text;
		}
		// Must be run on the UI thread
		public TextBlock ToTextBlock()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			TextBlock block = new TextBlock();
			block.TextWrapping = System.Windows.TextWrapping.NoWrap;

			int i = 0;
			foreach (VHDLClassifactionSpan s in ClassificationSpans)
			{
				if (s.Span.End < i)
					continue;

				string text;
				if (i < s.Span.Start)
				{
					text = Text.Substring(i, s.Span.Start - i);
					block.Inlines.Add(VHDLQuickInfoHelper.RunFromClassificationType(text, "text"));
					i = s.Span.Start;
				}

				text = Text.Substring(i, s.Span.End - i);
				block.Inlines.Add(VHDLQuickInfoHelper.RunFromClassificationType(text, s.ClassificationType));
				i = s.Span.End;
			}
			if (i < Text.Length)
			{
				block.Inlines.Add(VHDLQuickInfoHelper.RunFromClassificationType(Text.Substring(i), "text"));
			}

			return block;
		}
	}
	internal class VHDLSignature
		: ISignature
	{
		private ITextBuffer m_textBuffer = null;
		private IParameter m_currentParameter = null;
		private ReadOnlyCollection<IParameter> m_parameters = null;

		internal VHDLSignature(ITextBuffer textBuffer)
		{
			m_textBuffer = textBuffer;
			m_textBuffer.Changed += new EventHandler<TextContentChangedEventArgs>(OnSubjectBufferChanged);
		}

		private VHDLClassifiedText m_classifiedText = new VHDLClassifiedText("");
		public VHDLClassifiedText ClassifiedText => m_classifiedText;

		public event EventHandler<CurrentParameterChangedEventArgs> CurrentParameterChanged;

		public IParameter CurrentParameter
		{
			get { return m_currentParameter; }
			internal set
			{
				if (m_currentParameter != value)
				{
					IParameter prevCurrentParameter = m_currentParameter;
					m_currentParameter = value;
					this.RaiseCurrentParameterChanged(prevCurrentParameter, m_currentParameter);
				}
			}
		}

		private void RaiseCurrentParameterChanged(IParameter prevCurrentParameter, IParameter newCurrentParameter)
		{
			EventHandler<CurrentParameterChangedEventArgs> tempHandler = this.CurrentParameterChanged;
			if (tempHandler != null)
			{
				tempHandler(this, new CurrentParameterChangedEventArgs(prevCurrentParameter, newCurrentParameter));
			}
		}


		internal void OnSubjectBufferChanged(object sender, TextContentChangedEventArgs e)
		{
		}

		public ITrackingSpan ApplicableToSpan { get; set; }
		public string Content => ClassifiedText.Text;
		public string Documentation { get; set; }
		public ReadOnlyCollection<IParameter> Parameters
		{
			get
			{
				return m_parameters;
			}
			set
			{
				m_parameters = value;
				if (m_parameters.Count > 0)
					m_currentParameter = m_parameters[0];
			}
		}
		public string PrettyPrintedContent => Content;
	}


	internal class VHDLSignatureHelpSource
		: ISignatureHelpSource
	{
		private ITextBuffer m_textBuffer;
		private VHDLDocumentTable m_documentTable = null;
		public VHDLSignatureHelpSource(VHDLDocumentTable documentTable, ITextBuffer textBuffer, ITextDocumentFactoryService documentFactoryService)
		{
			m_documentTable = documentTable;
			m_textBuffer = textBuffer;
		}


		public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
		{
			ITextBuffer textBuffer = m_textBuffer;
			VHDLDocument doc = m_documentTable.GetDocument(m_textBuffer);
			ITextSnapshot snapshot = m_textBuffer.CurrentSnapshot;
			Span span = new Span(session.TextView.Caret.Position.BufferPosition.TranslateTo(snapshot, PointTrackingMode.Positive).Position, 1);
			//signatures.Add(new VHDLSignature(m_textBuffer, null, snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive)));
			/*VHDLDocument doc = m_documentTable.GetDocument(m_textBuffer);
			ITextSnapshot snapshot = m_textBuffer.CurrentSnapshot;*/
			SnapshotPoint point = session.GetTriggerPoint(m_textBuffer).GetPoint(snapshot);
			
			VHDLReverseLexer lexer = new VHDLReverseLexer(snapshot);
			lexer.SetIndex(point);
			IToken previousToken = lexer.GetPreviousToken();

			int iParameter = 0;
			int level = 0;
			int lengthLimit = 20;
			Type qzqzd = session.GetType();
			while (lengthLimit > 0)
			{
				if (previousToken.Text == "(")
				{
					if (level == 0)
					{
						VHDLReverseExpressionParser parser = new VHDLReverseExpressionParser(doc, lexer);
						VHDLDeclaration decl = parser.ParseName();
						if (decl == null)
							break;

						VHDLSignature sig = decl.BuildSignature(textBuffer, snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive));
						if (sig != null)
						{
							signatures.Add(sig);
							if (sig.Parameters.Count > 0)
								sig.CurrentParameter = sig.Parameters[Math.Min(iParameter, sig.Parameters.Count - 1)];

							return;
						}

						break;
					}
					else
						--level;
				}
				else if (previousToken.Text == ",")
				{
					if (level == 0)
					{
						++iParameter;
					}
				}
				else if(previousToken.Text == ")")
				{
					++level;
				}
				previousToken = lexer.GetPreviousToken();
				--lengthLimit;
			}
			session.Dismiss();
		}

		public ISignature GetBestMatch(ISignatureHelpSession session)
		{
			return null;
		}

		private bool m_isDisposed;
		public void Dispose()
		{
			if (!m_isDisposed)
			{
				GC.SuppressFinalize(this);
				m_isDisposed = true;
			}
		}
	}
}
