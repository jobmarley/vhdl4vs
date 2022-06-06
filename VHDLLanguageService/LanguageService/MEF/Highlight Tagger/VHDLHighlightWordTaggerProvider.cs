using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace vhdl4vs
{
	[Export(typeof(IViewTaggerProvider))]
	[ContentType("text")]
	[TagType(typeof(TextMarkerTag))]
	class VHDLHighlightWordTaggerProvider
		: IViewTaggerProvider
	{
		[Import]
		internal ITextSearchService TextSearchService { get; set; }

		[Import]
		internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }
		[Import]
		internal VHDLDocumentTable VHDLDocTable { get; set; }
		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
		{
			//provide highlighting only on the top buffer 
			if (textView.TextBuffer != buffer)
				return null;

			ITextStructureNavigator textStructureNavigator =
				TextStructureNavigatorSelector.GetTextStructureNavigator(buffer);

			return new VHDLHighlightWordTagger(textView, buffer, VHDLDocTable, TextSearchService, textStructureNavigator) as ITagger<T>;
		}
	}
}
