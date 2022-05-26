using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;

namespace MyCompany.LanguageServices.VHDL
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("VHDL QuickInfo Source")]
    //[Order(Before = "Default Quick Info Presenter")]
    [ContentType("VHDL")]
    internal class VHDLQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

        [Import]
        internal ITextDocumentFactoryService DocumentService { get; set; }

        [Import]
        internal IClassificationTypeRegistryService TypeRegistryService { get; set; }

        [Import]
        internal IClassificationFormatMapService ClassificationFormatMapService { get; set; }

        [Import]
        internal IStandardClassificationService StandardClassificationService { get; set; }

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        [Import]
        public IGlyphService GlyphService;

		[Import]
		internal VHDLDocumentTable VHDLDocTable { get; set; }

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new VHDLQuickInfoSource(textBuffer, TextBufferFactoryService, ContentTypeRegistryService, NavigatorService, GlyphService, DocumentService, TypeRegistryService, ClassificationFormatMapService, StandardClassificationService, VHDLDocTable);
        }
    }
}
