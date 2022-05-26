using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.Threading;
using System.Threading.Tasks;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime.Misc;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;
//using Microsoft.VisualStudio.Debugger.Script;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;

using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;

namespace MyCompany.LanguageServices.VHDL
{

	[Export(typeof(IVsTextViewCreationListener))]
	[Name("VHDL Token Completion Handler")]
	[ContentType("VHDL")]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	internal class VHDLCompletionHandlerProvider
		: IVsTextViewCreationListener
	{
		[Import]
		internal IVsEditorAdaptersFactoryService AdapterService { get; set; }
		[Import]
		internal IAsyncCompletionBroker CompletionBroker { get; set; }
		[Import]
		internal SVsServiceProvider ServiceProvider { get; set; }
		[Import]
		internal VHDLDocumentTable VHDLDocTable { get; set; }
		[Import]
		internal ISignatureHelpBroker SignatureHelpBroker;

		public void VsTextViewCreated(IVsTextView textViewAdapter)
		{
			ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
			if (textView == null)
				return;
			
			Func<VHDLCompletionCommandHandler> createCommandHandler = delegate () { return new VHDLCompletionCommandHandler(textViewAdapter, textView, CompletionBroker, VHDLDocTable, ServiceProvider, AdapterService, SignatureHelpBroker); };
			textView.Properties.GetOrCreateSingletonProperty(createCommandHandler);
		}
	}


	internal class VHDLCompletionCommandHandler
		: IOleCommandTarget
	{
		private IOleCommandTarget m_nextCommandHandler;
		private ITextView m_textView;
		private IAsyncCompletionBroker m_completionBroker;
		private VHDLDocumentTable m_vhdlDocTable;
		private IVsTextManager m_textManager = null;
		private IVsUIShellOpenDocument m_uiShellOpenDocument = null;
		private IVsEditorAdaptersFactoryService m_editorAdaptersFactoryService = null;
		private ISignatureHelpBroker m_signatureHelpBroker = null;
		private ISignatureHelpSession m_signatureHelpSession = null;

		internal VHDLCompletionCommandHandler(IVsTextView textViewAdapter,
			ITextView textView,
			IAsyncCompletionBroker completionBroker,
			VHDLDocumentTable vhdlDocTable,
			System.IServiceProvider serviceProvider,
			IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
			ISignatureHelpBroker signatureHelpBroker)
		{
			m_textManager = serviceProvider.GetService(typeof(VsTextManagerClass)) as IVsTextManager;
			m_uiShellOpenDocument = serviceProvider.GetService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
			m_editorAdaptersFactoryService = editorAdaptersFactoryService;
			m_textView = textView;
			m_completionBroker = completionBroker;
			m_vhdlDocTable = vhdlDocTable;
			m_signatureHelpBroker = signatureHelpBroker;

			//add the command to the command chain
			textViewAdapter.AddCommandFilter(this, out m_nextCommandHandler);
		}

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			int result = m_nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
			if (result != VSConstants.S_OK)
				return result;

			if (pguidCmdGroup == VSConstants.VSStd2K)
			{
				for (uint i = 0; i < cCmds; i++)
				{
					switch ((VSConstants.VSStd2KCmdID)prgCmds[i].cmdID)
					{
						//case VSConstants.VSStd2KCmdID.OUTLN_COLLAPSE_TO_DEF:
						case VSConstants.VSStd2KCmdID.FORMATDOCUMENT:
						case VSConstants.VSStd2KCmdID.FORMATSELECTION:
						case VSConstants.VSStd2KCmdID.COMMENT_BLOCK:
						case VSConstants.VSStd2KCmdID.COMMENTBLOCK:
						case VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK:
						case VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK:
							prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
							break;
					}
				}
			}
			else if (pguidCmdGroup == typeof(VSConstants.VSStd97CmdID).GUID)
			{
				for (uint i = 0; i < cCmds; i++)
				{
					switch ((VSConstants.VSStd97CmdID)prgCmds[i].cmdID)
					{
						//case VSConstants.VSStd97CmdID.GotoDecl:
						case VSConstants.VSStd97CmdID.GotoDefn:
							prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
							return VSConstants.S_OK;
					}
				}
			}
			else if (pguidCmdGroup == typeof(VSConstants.VSStd12CmdID).GUID)
			{
				for (uint i = 0; i < cCmds; i++)
				{
					switch ((VSConstants.VSStd12CmdID)prgCmds[i].cmdID)
					{
						case VSConstants.VSStd12CmdID.PeekDefinition:
							prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
							return VSConstants.S_OK;
					}
				}
			}

			return result;
		}

		private string GetStartingWhitespace(string s)
		{
			string space = "";
			foreach (char c in s)
			{
				if (char.IsWhiteSpace(c))
					space += c;
				else
					break;
			}
			return space;
		}

		SnapshotSpan? GetWordExtent(SnapshotPoint point)
		{
			ITextSnapshotLine line = point.Snapshot.GetLineFromPosition(point.Position);
			int col = point.Position - line.Start.Position;
			vhdlLexer lexer = new vhdlLexer(new Antlr4.Runtime.AntlrInputStream(line.GetText()));

			IList<Antlr4.Runtime.IToken> tokens = lexer.GetAllTokens();
			foreach (Antlr4.Runtime.IToken token in tokens)
			{
				if (col >= token.Column && col <= token.Column + (token.StopIndex - token.StartIndex))
					return new SnapshotSpan(point.Snapshot, token.StartIndex + line.Start, token.StopIndex - token.StartIndex + 1);
			}

			return null;
		}

		async Task NavigateToAsync(VHDLDocument doc, Span span)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			Guid logicalView = VSConstants.LOGVIEWID.TextView_guid;

			IVsTextBuffer vsTextBuffer = null;
			int err = VSConstants.S_OK;
			IVsTextView vsTextView = null;
			if (!doc.IsOpen())
			{
				Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp = null;
				IVsUIHierarchy uiHierarchy = null;
				uint itemID = 0;
				IVsWindowFrame windowFrame = null;
				err = m_uiShellOpenDocument.OpenDocumentViaProject(doc.Filepath, ref logicalView, out sp, out uiHierarchy, out itemID, out windowFrame);
				if (err != VSConstants.S_OK)
					throw new COMException("m_uiShellOpenDocument.OpenDocumentViaProject raised an exception", err);
				err = windowFrame.Show();
				if (err != VSConstants.S_OK)
					throw new COMException("windowFrame.Show raised an exception", err);

				// If we just m_editorAdaptersFactoryService.GetBufferAdapter at that point, sometimes doc.TextDocument
				// is still null. I think this is because we are on the UI thread, so events are not dispatched yet
				object docView = null;
				err = windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out docView);
				if (err != VSConstants.S_OK)
					throw new COMException("windowFrame.GetProperty raised an exception", err);

				IVsCodeWindow codeWindow = (IVsCodeWindow)docView;
				err = codeWindow.GetLastActiveView(out vsTextView);
				if (err != VSConstants.S_OK)
				{
					err = codeWindow.GetPrimaryView(out vsTextView);
					if (err != VSConstants.S_OK)
						throw new COMException("codeWindow.GetPrimaryView raised an exception", err);
				}
			}
			else
			{
				//	We can also GetProperty doc_view on windowFrame but it seems better like this
				vsTextBuffer = m_editorAdaptersFactoryService.GetBufferAdapter(doc.TextDocument.TextBuffer);

				err = m_textManager.GetActiveView(0, vsTextBuffer, out vsTextView);
				if (err != VSConstants.S_OK)
					throw new COMException("m_textManager.GetActiveView raised an exception", err);
			}

			IWpfTextView textView = m_editorAdaptersFactoryService.GetWpfTextView(vsTextView);
			err = m_textManager.NavigateToPosition(vsTextBuffer, ref logicalView, span.Start, span.Length);
			if (err != VSConstants.S_OK)
				throw new COMException("m_textManager.NavigateToPosition raised an exception", err);
			textView.Selection.Select(new SnapshotSpan(textView.TextSnapshot, span), true);
		}
		private bool GoToDefinition(SnapshotPoint point)
		{
			SnapshotSpan? extent = GetWordExtent(point);
			if (!extent.HasValue)
				return true;

			VHDLDocument thisDoc = m_vhdlDocTable.GetDocument(m_textView.TextBuffer);

			try
			{
				VHDLDeclaration decl = VHDLDeclarationUtilities.GetDeclaration(thisDoc, point) as VHDLDeclaration;

				if (decl == null)
					return false;

				int start = decl.NameContext.Start.StartIndex;
				int end = decl.NameContext.Stop.StopIndex + 1;
				NavigateToAsync(decl.Document, new Span(start, end - start));
			}
			catch (Exception e)
			{

			}
			return true;
		}

		private bool CommentBlock()
		{
			int start = m_textView.Selection.Start.Position.TranslateTo(m_textView.TextBuffer.CurrentSnapshot, PointTrackingMode.Positive);
			int end = m_textView.Selection.End.Position.TranslateTo(m_textView.TextBuffer.CurrentSnapshot, PointTrackingMode.Positive);
			int lineStart = m_textView.TextBuffer.CurrentSnapshot.GetLineNumberFromPosition(start);
			int lineEnd = m_textView.TextBuffer.CurrentSnapshot.GetLineNumberFromPosition(end);

			int minSpaceLength = int.MaxValue;
			char spaceChar = '\t';
			for (int i = lineStart; i <= lineEnd; ++i)
			{
				string text = m_textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(i).GetText();
				//	Skip if line is empty
				if (string.IsNullOrWhiteSpace(text))
					continue;

				string space = GetStartingWhitespace(text);
				int spaceLength = space.Length;
				if (minSpaceLength > spaceLength)
				{
					minSpaceLength = spaceLength;
					if (spaceLength > 0)
						spaceChar = space[0];
				}
			}
			ITextEdit edit = m_textView.TextBuffer.CreateEdit();
			for (int i = lineStart; i <= lineEnd; ++i)
			{
				string text = m_textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(i).GetText();
				if (string.IsNullOrWhiteSpace(text))
				{
					edit.Insert(m_textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(i).Start, new string(spaceChar, minSpaceLength) + "--");
				}
				else
					edit.Insert(m_textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(i).Start + minSpaceLength, "--");
			}
			edit.Apply();
			return true;
		}
		private bool UncommentBlock()
		{
			int start = m_textView.Selection.Start.Position.TranslateTo(m_textView.TextBuffer.CurrentSnapshot, PointTrackingMode.Positive);
			int end = m_textView.Selection.End.Position.TranslateTo(m_textView.TextBuffer.CurrentSnapshot, PointTrackingMode.Positive);
			int lineStart = m_textView.TextBuffer.CurrentSnapshot.GetLineNumberFromPosition(start);
			int lineEnd = m_textView.TextBuffer.CurrentSnapshot.GetLineNumberFromPosition(end);
			ITextEdit edit = m_textView.TextBuffer.CreateEdit();
			for (int i = lineStart; i <= lineEnd; ++i)
			{
				string text = m_textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(i).GetText();
				if (text.TrimStart().StartsWith("--"))
				{
					edit.Delete(m_textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(i).Start + text.IndexOf("--"), 2);
				}
			}
			edit.Apply();
			return true;
		}

		private bool CommitCompletion(char c)
		{
			if (m_completionBroker.IsCompletionActive(m_textView))
			{
				IAsyncCompletionSession session = m_completionBroker.GetSession(m_textView);
				if (session.IsDismissed)
					return false;
				session.Commit(c, CancellationToken.None);
				return true;
			}

			return false;
		}

		//	Add correct indentation at caret position
		private bool Indent()
		{
			VHDLDocument document = m_vhdlDocTable.GetDocument(m_textView.TextBuffer);
			ParseResult result = document.Parser.PResult;

			VHDLIndentVisitor visitor = new VHDLIndentVisitor(
				m_textView.Caret.Position.BufferPosition.TranslateTo(result.Snapshot, PointTrackingMode.Negative).Position,
				result.Snapshot);
			int indent = visitor.Visit(result.Tree);

			ITextEdit edit = m_textView.TextBuffer.CreateEdit();
			edit.Replace(m_textView.Selection.StreamSelectionSpan.SnapshotSpan.Span, Environment.NewLine + new string('\t', indent));
			edit.Apply();
			return true;
		}

		private bool FormatDocument()
		{
			ITextSnapshot snapshot = m_textView.TextSnapshot;

			//	Get an uptodate parse tree
			vhdlLexer lexer = new vhdlLexer(new Antlr4.Runtime.AntlrInputStream(snapshot.GetText()));

			Antlr4.Runtime.CommonTokenStream tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
			vhdlParser parser = new vhdlParser(tokenStream);
			vhdlParser.Design_fileContext context = parser.design_file();

			VHDLFormatVisitor visitor = new VHDLFormatVisitor(m_textView.TextSnapshot);
			visitor.Visit(context);
			visitor.Edit.Apply();
			return true;
		}

		private bool FormatSelection()
		{
			return true;
		}

		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			/*if (VsShellUtilities.IsInAutomationFunction(m_provider.ServiceProvider))
			{
				return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}*/
			
			if (pguidCmdGroup == VSConstants.VSStd2K)
			{
				switch (nCmdID)
				{
					case (uint)VSConstants.VSStd2KCmdID.FORMATDOCUMENT:
						if(FormatDocument())
							return VSConstants.S_OK;
						break;
					case (uint)VSConstants.VSStd2KCmdID.FORMATSELECTION:
						if (FormatSelection())
							return VSConstants.S_OK;
						break;
					case (uint)VSConstants.VSStd2KCmdID.COMMENT_BLOCK:
					case (uint)VSConstants.VSStd2KCmdID.COMMENTBLOCK:
						if (CommentBlock())
							return VSConstants.S_OK;
						break;
					case (uint)VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK:
					case (uint)VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK:
						if (UncommentBlock())
							return VSConstants.S_OK;
						break;
					case (uint)VSConstants.VSStd2KCmdID.RETURN:
						if (m_completionBroker.IsCompletionActive(m_textView))
						{
							CommitCompletion(default(char));
							return VSConstants.S_OK;
						}
						else
						{
							Indent();
							return VSConstants.S_OK;
						}
						break;
					case (uint)VSConstants.VSStd2KCmdID.TAB:
						if (CommitCompletion('\t'))
							return VSConstants.S_OK;
						break;
					case (uint)VSConstants.VSStd2KCmdID.TYPECHAR:
						char typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
						if (typedChar == '"')
						{
							ITextBuffer textBuffer = m_textView.TextBuffer;
							textBuffer.Insert(m_textView.Caret.Position.BufferPosition, "\"\"");
							SnapshotPoint point = m_textView.Caret.Position.BufferPosition;
							m_textView.Caret.MoveTo(point - 1);
							return VSConstants.S_OK;
						}
						else if (typedChar == '(')
						{
							if (m_signatureHelpSession == null)
							{
								ITextBuffer textBuffer = m_textView.TextBuffer;
								textBuffer.Insert(m_textView.Caret.Position.BufferPosition, "(");
								ITextSnapshot snapshot = textBuffer.CurrentSnapshot;
								ITrackingPoint point = snapshot.CreateTrackingPoint(m_textView.Caret.Position.BufferPosition.TranslateTo(snapshot, PointTrackingMode.Positive), PointTrackingMode.Positive);
								m_signatureHelpSession = m_signatureHelpBroker.CreateSignatureHelpSession(m_textView, point, true);
								m_signatureHelpSession.Dismissed += OnSignatureHelperSessionDismissed;
								m_signatureHelpSession.Start();
								return VSConstants.S_OK;
							}
						}
						else if(typedChar == ',')
						{
							ITextBuffer textBuffer = m_textView.TextBuffer;
							textBuffer.Insert(m_textView.Caret.Position.BufferPosition, ",");
							if (m_signatureHelpSession != null)
							{
								m_signatureHelpSession.Recalculate();
							}
							else
							{
								ITextSnapshot snapshot = textBuffer.CurrentSnapshot;
								ITrackingPoint point = snapshot.CreateTrackingPoint(m_textView.Caret.Position.BufferPosition.TranslateTo(snapshot, PointTrackingMode.Positive), PointTrackingMode.Positive);
								m_signatureHelpSession = m_signatureHelpBroker.CreateSignatureHelpSession(m_textView, point, true);
								m_signatureHelpSession.Dismissed += OnSignatureHelperSessionDismissed;
								m_signatureHelpSession.Start();
							}
							return VSConstants.S_OK;
						}
						else if(typedChar == ')')
						{
							if (m_signatureHelpSession != null && !m_signatureHelpSession.IsDismissed)
							{
								m_signatureHelpSession.Dismiss();
							}
						}
						else if (char.IsLetterOrDigit(typedChar) || typedChar == '_')
						{
							return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
						}
						else if (char.IsWhiteSpace(typedChar) || char.IsPunctuation(typedChar))
						{
							CommitCompletion(typedChar);
							return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
						}
						break;
					case (uint)VSConstants.VSStd2KCmdID.BACKSPACE:
						{
							// Backspace triggers a completion session by default. That's not what we want
							// but if I handle the delete myself, when using ctrl-z, the selection is lost
							//return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
							ITextSelection selection = m_textView.Selection;
							
							ITextEdit e = m_textView.TextBuffer.CreateEdit();
							if (!selection.IsEmpty)
							{
								foreach (SnapshotSpan span in selection.SelectedSpans)
									e.Delete(span);
								selection.Clear();
								e.Apply();
							}
							else if (selection.Start.Position > 0)
							{
								if (selection.Start.Position.GetContainingLine().Start == selection.Start.Position)
									e.Delete(new SnapshotSpan(selection.Start.Position - 2, 2));
								else
									e.Delete(new SnapshotSpan(selection.Start.Position - 1, 1));
								e.Apply();
							}
							else
								e.Cancel();

							if (m_signatureHelpSession != null)
							{
								m_signatureHelpSession.Recalculate();
							}
							return VSConstants.S_OK;
						}
					default:
						break;
				}
			}
			else if(pguidCmdGroup == typeof(VSConstants.VSStd97CmdID).GUID)
			{
				switch(nCmdID)
				{
					case (uint)VSConstants.VSStd97CmdID.GotoDefn:
						if (GoToDefinition(m_textView.Caret.Position.BufferPosition))
							return VSConstants.S_OK;
						break;
					case (uint)VSConstants.VSStd97CmdID.GotoDecl:
						if (GoToDefinition(m_textView.Caret.Position.BufferPosition))
							return VSConstants.S_OK;
						break;
				}
			}

			return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
		}

		private void OnSignatureHelperSessionDismissed(object sender, EventArgs e)
		{
			if (m_signatureHelpSession != null)
			{
				m_signatureHelpSession.Dismissed -= OnSignatureHelperSessionDismissed;
				m_signatureHelpSession = null;
			}
		}
	}
}
