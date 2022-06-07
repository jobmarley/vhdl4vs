/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

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

namespace vhdl4vs
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
