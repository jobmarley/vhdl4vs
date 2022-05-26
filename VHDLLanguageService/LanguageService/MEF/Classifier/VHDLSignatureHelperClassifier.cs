using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Language.Intellisense;

namespace MyCompany.LanguageServices.VHDL
{
	// This is required because of the way Signature helper works.
	// Maybe we could achieve something with presenters, but this way seems easier.
	// Check DefaultSignatureHelpPresenter in Microsoft.VisualStudio.Platform.VSEditor.dll for more info
	internal class VHDLSignatureHelperClassifier
		: IClassifier
	{
		private ITextBuffer m_textBuffer = null;
		private IClassificationTypeRegistryService m_classificationService = null;
		private IStandardClassificationService m_standardClassificationService = null;
		public VHDLSignatureHelperClassifier(ITextBuffer textBuffer, IClassificationTypeRegistryService classificationService, IStandardClassificationService standardClassificationService)
		{
			m_classificationService = classificationService;
			m_standardClassificationService = standardClassificationService;
			m_textBuffer = textBuffer;
		}
		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			// We just get the classified spans from the signature that's simpler
			// Otherwise we would have to get the original ITextBuffer, and the trigger point
			// And get declarations etc... and even then, we wouldn't have good flexibility for colors in the comment part
			ISignatureHelpSession session = null;
			if (!m_textBuffer.Properties.TryGetProperty(typeof(ISignatureHelpSession), out session))
				return null;

			VHDLSignature signature = session?.SelectedSignature as VHDLSignature;
			if (signature == null)
				return null;

			List<ClassificationSpan> result = new List<ClassificationSpan>();
			foreach (VHDLClassifactionSpan s in signature.ClassifiedText.ClassificationSpans)
			{
				SnapshotSpan? classificationSpan = span.Intersection(s.Span);
				if (classificationSpan.HasValue)
				{
					IClassificationType type = m_classificationService.GetClassificationType(s.ClassificationType);
					if (type == null)
						continue;
					result.Add(new ClassificationSpan(classificationSpan.Value, type));
				}
			}
			
			return result;
		}
	}
}
