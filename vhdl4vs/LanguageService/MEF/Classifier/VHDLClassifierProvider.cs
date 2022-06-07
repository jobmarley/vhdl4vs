/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

//------------------------------------------------------------------------------
// <copyright file="MLClassifierProvider.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Language.StandardClassification;
using System;
using Microsoft.VisualStudio.Shell;

namespace vhdl4vs
{
    /// <summary>
    /// Classifier provider. It adds the classifier to the set of classifiers.
    /// </summary>
	[Name("VHDL classifier provider")]
    [Export(typeof(IClassifierProvider))]
    [ContentType("VHDL")]
    //[ContentType("VHDL Signature Help")]
    //[ContentType("any")]
    internal class VHDLClassifierProvider
        : IClassifierProvider
    {
        // Disable "Field is never assigned to..." compiler's warning. Justification: the field is assigned by MEF.
#pragma warning disable 649

        /// <summary>
        /// Classification registry to be used for getting a reference
        /// to the custom classification type later.
        /// </summary>
        [Import]
		internal IClassificationTypeRegistryService ClassificationRegistry { get; set; }

		[Import]
		internal IStandardClassificationService StandardClassificationService { get; set; }

		[Import]
        internal ITextDocumentFactoryService DocumentService { get; set; }

		[Import]
		internal SVsServiceProvider ServiceProvider { get; set; }

		[Import]
		internal VHDLDocumentTable ParserManagerService { get; set; }

#pragma warning restore 649

		#region IClassifierProvider

		/// <summary>
		/// Gets a classifier for the given text buffer.
		/// </summary>
		/// <param name="buffer">The <see cref="ITextBuffer"/> to classify.</param>
		/// <returns>A classifier for the text buffer, or null if the provider cannot do so in its current state.</returns>
		public IClassifier GetClassifier(ITextBuffer buffer)
        {
			Func<VHDLClassifier> createClassifier = delegate () { return new VHDLClassifier(buffer, this.ClassificationRegistry, this.StandardClassificationService, this.DocumentService, this.ServiceProvider, ParserManagerService); };
            return buffer.Properties.GetOrCreateSingletonProperty<VHDLClassifier>(createClassifier);
        }

        #endregion
    }
}
