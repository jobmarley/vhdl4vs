using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Antlr4.Runtime.Misc;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using System.Collections.Immutable;
using Antlr4.Runtime;

namespace MyCompany.LanguageServices.VHDL
{
	class VHDLCompletionSource
		: IAsyncCompletionSource
	{
		private ITextView m_textView;
		private IGlyphService m_glyphService;
		private ITextDocumentFactoryService m_documentService;
		private SVsServiceProvider m_serviceProvider = null;
		private IViewTagAggregatorFactoryService m_viewTagAggregatorFactoryService;
		private IStandardClassificationService m_standardClassificationService;
		private ITextStructureNavigatorSelectorService m_textNavigatorService;
		private VHDLDocumentTable m_vhdlDocTable = null;

		public VHDLCompletionSource(
			ITextView textView,
			ITextStructureNavigatorSelectorService textNavigatorService,
			IGlyphService glyphService,
			ITextDocumentFactoryService documentService,
			SVsServiceProvider serviceProvider,
			IViewTagAggregatorFactoryService viewTagAggregatorFactoryService,
			IStandardClassificationService standardClassificationService,
			IClassificationTypeRegistryService classificationTypeRegistryService,
			IClassificationFormatMapService classificationFormatMapService,
			VHDLDocumentTable vhdlDocumentTable)
		{
			m_textView = textView;
			m_textNavigatorService = textNavigatorService;
			m_glyphService = glyphService;
			m_documentService = documentService;
			m_serviceProvider = serviceProvider;
			m_viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			m_standardClassificationService = standardClassificationService;
			m_vhdlDocTable = vhdlDocumentTable;

			VHDLQuickInfoHelper.Initialize(classificationFormatMapService, classificationTypeRegistryService, glyphService);
		}

		void GetDefaultItems(Dictionary<string, CompletionItem> items)
		{
			foreach(string keyword in VHDLLanguageUtils.Keywords)
			{
				CompletionItem item = new CompletionItem(keyword, this, VHDLQuickInfoHelper.KeywordImageElement);
				item.Properties["iskeyword"] = true;
				items[keyword] = item;
			}
		}
		void GetLibraries(VHDLDocument doc, Dictionary<string, CompletionItem> items)
		{
			foreach (string s in doc.Project?.GetAllLibraries()?.Select(lib => lib.Name).Prepend("work") ?? Array.Empty<string>())
			{
				if (items.ContainsKey(s))
					continue;

				CompletionItem item = new CompletionItem(s, this, VHDLQuickInfoHelper.PackageImageElement);
				items[s] = item;
			}
		}
		void AddDeclarationCompletion(VHDLDeclaration decl, Dictionary<string, CompletionItem> items)
		{
			try
			{
				if (decl?.UndecoratedName != null && !items.ContainsKey(decl.UndecoratedName))
				{
					CompletionItem ci = decl.BuildCompletion(this);
					if (ci != null)
						items[decl.UndecoratedName] = ci;
				}
			}
			catch (Exception ex)
			{
			}
		}
		void BuildCompletionList(VHDLDocument document, VHDLDeclaration localScope, Dictionary<string, CompletionItem> items)
		{
			Func<VHDLDeclaration, bool> filter = decl =>
			{
				return !(decl is VHDLPackageBodyDeclaration
				|| decl is VHDLFunctionBodyDeclaration
				|| decl is VHDLProcedureBodyDeclaration
				|| (decl is VHDLFunctionDeclaration && ((VHDLFunctionDeclaration)decl).UndecoratedName.StartsWith("\"")));
			};
			foreach (VHDLDeclaration decl in VHDLDeclarationUtilities.FindAll(localScope, filter))
			{
				if (!items.ContainsKey(decl.UndecoratedName))
					AddDeclarationCompletion(decl, items);
			}
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

		void BuildMemberCompletionList(VHDLDeclaration previousDeclaration, Dictionary<string, CompletionItem> items)
		{
			if (previousDeclaration is VHDLAbstractVariableDeclaration avd)
			{
				VHDLType t = avd.Type;
				t = avd.Type.Dereference();

				if (t is VHDLRecordType recordType)
				{
					foreach (var field in recordType.Fields)
					{
						items[field.Name] = new CompletionItem(field.Name, this, VHDLQuickInfoHelper.VariableImageElement);
						items[field.Name].Properties["record_field"] = field;
					}
				}

				return;
			}
			Func<VHDLDeclaration, bool> filter = decl =>
			{
				return !(decl is VHDLPackageBodyDeclaration
				|| decl is VHDLFunctionBodyDeclaration
				|| decl is VHDLProcedureBodyDeclaration
				|| (decl is VHDLFunctionDeclaration && ((VHDLFunctionDeclaration)decl).UndecoratedName.StartsWith("\"")));
			};
			foreach (VHDLDeclaration decl in previousDeclaration.Children.Where(filter))
			{
				if (!items.ContainsKey(decl.UndecoratedName))
					items[decl.UndecoratedName] = decl.BuildCompletion(this);
			}
		}
		bool IsWritingName(Antlr4.Runtime.IToken previousToken)
		{
			if (previousToken == null)
				return false;
			return previousToken.Text.ToLower() == "entity" ||
				   previousToken.Text.ToLower() == "architecture" ||
				   previousToken.Text.ToLower() == "package" ||
				   previousToken.Text.ToLower() == "signal" ||
				   previousToken.Text.ToLower() == "variable" ||
				   previousToken.Text.ToLower() == "process" ||
				   previousToken.Text.ToLower() == "function" ||
				   previousToken.Text.ToLower() == "procedure";
		}
		bool IsInString(Antlr4.Runtime.IToken previousToken, int position)
		{
			if (previousToken == null)
				return false;

			if ((vhdlLexer.DefaultVocabulary.GetSymbolicName(previousToken.Type) == "STRING_LITERAL" ||
				vhdlLexer.DefaultVocabulary.GetSymbolicName(previousToken.Type) == "CHARACTER_LITERAL" ||
				vhdlLexer.DefaultVocabulary.GetSymbolicName(previousToken.Type) == "BIT_STRING_LITERAL") &&
				position > previousToken.StartIndex && position <= previousToken.StopIndex)
			{
				return true;
			}

			return false;
		}

		bool IsInComment(Antlr4.Runtime.IToken previousToken)
		{
			if (previousToken == null)
				return false;
			return vhdlLexer.DefaultVocabulary.GetSymbolicName(previousToken.Type) == "COMMENT";
		}

		public async Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
		{
			ITextBuffer textBuffer = session.TextView.TextBuffer;
			if (textBuffer == null)
				return null;

			VHDLDocument doc = m_vhdlDocTable.GetDocument(textBuffer);
			if (doc == null)
				return null;

			// Parse current line 
			ITextSnapshot snapshot = triggerLocation.Snapshot;
			VHDLReverseLexer lexer = new VHDLReverseLexer(snapshot);
			lexer.SetIndex(triggerLocation);
			IToken currentToken = lexer.GetPreviousToken();
			IToken previousToken = lexer.PeekPreviousToken();

			Dictionary<string, CompletionItem> items = new Dictionary<string, CompletionItem>(StringComparer.OrdinalIgnoreCase);

			char c = (triggerLocation - 1).GetChar();
			if (c == '.')
			{
				// We are selecting a member
				VHDLReverseExpressionParser parser = new VHDLReverseExpressionParser(doc, lexer);
				VHDLDeclaration decl = parser.ParseName();
				if (decl == null)
					return null;

				BuildMemberCompletionList(decl, items);
			}
			else if ((char.IsLetter(c) || c == '_') && currentToken.Length() == 1)
			{
				if (previousToken != null && string.Compare(previousToken.Text, "use", true) == 0)
				{
					// use <libraryname>
					GetLibraries(doc, items);
				}
				else
				{
					GetDefaultItems(items);

					// simple name, more complicated
					if (IsWritingName(previousToken) ||
						IsInString(currentToken, triggerLocation - triggerLocation.GetContainingLine().Start) ||
						IsInComment(currentToken))
					{
						return null;
					}
					VHDLDeclaration parentDeclaration = VHDLDeclarationUtilities.GetEnclosingDeclaration(doc?.Parser?.AResult, triggerLocation);
					if (parentDeclaration != null)
						BuildCompletionList(doc, parentDeclaration, items);
				}
			}
			else
				return null;


			return new CompletionContext(ImmutableArray.CreateRange(items.Values));
		}

		public async Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
		{
			VHDLDeclaration decl = null;
			if (item.Properties.TryGetProperty("declaration", out decl))
			{
				return await decl.BuildQuickInfoAsync();
			}
			bool iskeyword = false;
			if (item.Properties.TryGetProperty("iskeyword", out iskeyword))
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				System.Windows.Controls.TextBlock textBlock = new System.Windows.Controls.TextBlock();
				textBlock.TextWrapping = System.Windows.TextWrapping.NoWrap;

				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(item.DisplayText + " Keyword"));
				return textBlock;
			}
			return null;
		}

		public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
		{
			try
			{
				SnapshotPoint start = triggerLocation - 1;
				SnapshotPoint end = triggerLocation;
				if (start.GetChar() == '.')
					start = start + 1;
				SnapshotSpan span = new SnapshotSpan(start, end);
				return new CompletionStartData(CompletionParticipation.ProvidesItems, span);
			}
			catch (Exception ex)
			{
			}
			return CompletionStartData.DoesNotParticipateInCompletion;
		}
	}
}
