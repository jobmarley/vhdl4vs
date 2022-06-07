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
