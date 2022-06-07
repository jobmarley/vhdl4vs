/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

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

namespace vhdl4vs
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
