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
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;

namespace vhdl4vs
{
	[Export(typeof(IAsyncCompletionSourceProvider))]
	[ContentType("VHDL")]
	[Name("VHDL Token Completion")]
	internal class VHDLCompletionSourceProvider : IAsyncCompletionSourceProvider
	{
		[Import]
		internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

		[Import]
		internal IGlyphService GlyphService { get; set; }

		[Import]
		internal ITextDocumentFactoryService DocumentService { get; set; }

		[Import]
		internal SVsServiceProvider ServiceProvider { get; set; }

		[Import]
		internal IViewTagAggregatorFactoryService ViewTagAggregatorFactoryService { get; set; }

		[Import]
		internal IStandardClassificationService StandardClassificationService { get; set; }

		[Import]
		internal VHDLDocumentTable VHDLDocTable { get; set; }

		[Import]
		internal IClassificationTypeRegistryService ClassificationTypeRegistryService { get; set; }

		[Import]
		internal IClassificationFormatMapService ClassificationFormatMapService { get; set; }

		public IAsyncCompletionSource GetOrCreate(ITextView textView)
		{
			return new VHDLCompletionSource(
				textView,
				NavigatorService,
				GlyphService,
				DocumentService,
				ServiceProvider,
				ViewTagAggregatorFactoryService,
				StandardClassificationService,
				ClassificationTypeRegistryService,
				ClassificationFormatMapService,
				VHDLDocTable);
		}
	}
}
