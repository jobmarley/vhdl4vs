using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows.Controls;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Reflection;
using Microsoft.VisualStudio.Text.Formatting;
using System.Windows.Media;
using System.Windows;
using System.Windows.Documents;
using Microsoft.VisualStudio.Text.Classification;
using System.Threading;
using Microsoft.VisualStudio.Language.StandardClassification;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;


namespace MyCompany.LanguageServices.VHDL
{
	//  Extension to apply text format to DependencyObject
	internal static class DependencyObjectExtensions
	{
		public static void SetTextProperties(this DependencyObject dependencyObject, TextFormattingRunProperties textProperties)
		{
			dependencyObject.SetValue(TextElement.FontFamilyProperty, (object)textProperties.Typeface.FontFamily);
			dependencyObject.SetValue(TextElement.FontSizeProperty, (object)textProperties.FontRenderingEmSize);
			dependencyObject.SetValue(TextElement.FontStyleProperty, (object)(textProperties.Italic ? FontStyles.Italic : FontStyles.Normal));
			dependencyObject.SetValue(TextElement.FontWeightProperty, (object)(textProperties.Bold ? FontWeights.Bold : FontWeights.Normal));
			dependencyObject.SetValue(TextElement.BackgroundProperty, (object)textProperties.BackgroundBrush);
			dependencyObject.SetValue(TextElement.ForegroundProperty, (object)textProperties.ForegroundBrush);
		}

		public static void SetDefaultTextProperties(this DependencyObject dependencyObject, IClassificationFormatMap formatMap)
		{
			dependencyObject.SetTextProperties(formatMap.DefaultTextProperties);
		}
	}

	internal class VHDLQuickInfoSource
		: IAsyncQuickInfoSource
	{
		private WeakReference<ITextBuffer> m_subjectBuffer;
		private VHDLDocumentTable m_vhdlDocTable = null;

		public VHDLQuickInfoSource(
			ITextBuffer subjectBuffer,
			ITextBufferFactoryService textBufferFactoryService,
			IContentTypeRegistryService contentTypeRegistryService,
			ITextStructureNavigatorSelectorService navigatorService,
			IGlyphService glyphService,
			ITextDocumentFactoryService documentService,
			IClassificationTypeRegistryService classificationTypeRegistryService,
			IClassificationFormatMapService classificationFormatMapService,
			IStandardClassificationService standardClassificationService,
			VHDLDocumentTable vhdlDocTable)
		{
			m_subjectBuffer = new WeakReference<ITextBuffer>(subjectBuffer);
			m_vhdlDocTable = vhdlDocTable;

			VHDLQuickInfoHelper.Initialize(classificationFormatMapService, classificationTypeRegistryService, glyphService);
		}

		SnapshotSpan? GetWordExtent(SnapshotPoint point)
		{
			ITextSnapshotLine line = point.Snapshot.GetLineFromPosition(point.Position);
			int col = point.Position - line.Start.Position;
			vhdlLexer lexer = new vhdlLexer(new Antlr4.Runtime.AntlrInputStream(line.GetText()));

			IList<Antlr4.Runtime.IToken> tokens = lexer.GetAllTokens();
			foreach(Antlr4.Runtime.IToken token in tokens)
			{
				if (col >= token.Column && col <= token.Column + (token.StopIndex - token.StartIndex))
					return new SnapshotSpan(point.Snapshot, token.StartIndex + line.Start, token.StopIndex - token.StartIndex + 1);
			}

			return null;
		}

		private async Task<object> SearchQuickInfoAsync(string text, SnapshotPoint triggerPoint)
		{
			ITextBuffer textBuffer = null;
			if (!m_subjectBuffer.TryGetTarget(out textBuffer))
				return null;

			VHDLDocument thisDoc = m_vhdlDocTable.GetDocument(textBuffer);

			try
			{
				VHDLDeclaration decl = VHDLDeclarationUtilities.GetDeclaration(thisDoc, triggerPoint);
				if (decl != null)
					return await decl.BuildQuickInfoAsync();
			}
			catch (Exception ex)
			{

			}
			return null;
		}
		public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
		{
			ITrackingSpan applicableToSpan = null;
			ITextBuffer textBuffer = null;

			m_subjectBuffer.TryGetTarget(out textBuffer);
			// Map the trigger point down to our buffer.
			SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(textBuffer.CurrentSnapshot);
			if (!subjectTriggerPoint.HasValue)
			{
				return null;
			}

			ITextSnapshot currentSnapshot = subjectTriggerPoint.Value.Snapshot;
			SnapshotSpan querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);

			//look for occurrences of our QuickInfo words in the span
			/*ITextStructureNavigator navigator = m_navigatorService.GetTextStructureNavigator(m_subjectBuffer);
			TextExtent extent = navigator.GetExtentOfWord(subjectTriggerPoint.Value);*/

			SnapshotSpan? extent = GetWordExtent(subjectTriggerPoint.Value);
			//string searchText = extent.Span.GetText();

			object qiContent = null;
			if (extent.HasValue)
			{
				applicableToSpan = currentSnapshot.CreateTrackingSpan
				(
					//extent.Span.Start, extent.Span.Length, SpanTrackingMode.EdgeInclusive
					extent.Value.Span, SpanTrackingMode.EdgeInclusive
				);

				string searchText = extent.Value.GetText();

				qiContent = await SearchQuickInfoAsync(searchText, subjectTriggerPoint.Value);
			}
			else
			{
				//  No word under carret
				applicableToSpan = null;
			}

			if (qiContent == null)
				return null;
			else
				return new QuickInfoItem(applicableToSpan, qiContent);
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
