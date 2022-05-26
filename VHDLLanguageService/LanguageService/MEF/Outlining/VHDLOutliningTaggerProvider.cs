using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;

namespace MyCompany.LanguageServices.VHDL
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType("VHDL")]
    internal sealed class VHDLOutliningTaggerProvider
        : ITaggerProvider
    {
        [Import]
        internal ITextDocumentFactoryService DocumentService { get; set; }

		[Import]
		internal VHDLDocumentTable VHDLDocTable{ get; set; }

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            //create a single tagger for each buffer.
            Func<ITagger<T>> sc = delegate () { return new VHDLOutliningTagger(buffer, DocumentService, VHDLDocTable) as ITagger<T>; };
            return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(sc);
        }
    }
}
