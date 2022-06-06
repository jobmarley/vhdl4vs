using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs
{
    /// <summary>
    /// Classifier provider. It adds the classifier to the set of classifiers.
    /// </summary>
    [Name("VHDL classifier provider")]
    [Export(typeof(IClassifierProvider))]
    [ContentType("VHDL Signature Help")]
    //[ContentType("any")]
    internal class VHDLSignatureHelperClassifierProvider
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

#pragma warning restore 649

        #region IClassifierProvider

        /// <summary>
        /// Gets a classifier for the given text buffer.
        /// </summary>
        /// <param name="buffer">The <see cref="ITextBuffer"/> to classify.</param>
        /// <returns>A classifier for the text buffer, or null if the provider cannot do so in its current state.</returns>
        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            Func<VHDLSignatureHelperClassifier> createClassifier = delegate () { return new VHDLSignatureHelperClassifier(buffer, ClassificationRegistry, StandardClassificationService); };
            return buffer.Properties.GetOrCreateSingletonProperty(createClassifier);
        }

        #endregion
    }
}
