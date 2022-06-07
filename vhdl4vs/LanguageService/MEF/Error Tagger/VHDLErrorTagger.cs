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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace vhdl4vs
{
    class VHDLErrorTagger
         : ITagger<IErrorTag>
    {		
		VHDLDocumentTable m_vhdlDocTable = null;
		VHDLDocument m_vhdlDoc = null;
        
		public VHDLErrorTagger(ITextBuffer buffer, VHDLDocumentTable vhdlDocTable)
        {
			m_vhdlDocTable = vhdlDocTable;

            m_vhdlDocTable.DocumentClosed += OnDocumentClosed;
            m_vhdlDoc = m_vhdlDocTable.GetOrAddDocument(buffer);
            m_vhdlDoc.Parser.DeepAnalysisComplete += DeepAnalysisComplete;

            DeepAnalysisResult result = m_vhdlDoc.Parser.DAResult;
            if (result != null)
            {
                ReParse(result, result.Errors.Concat(result.AnalysisResult.Errors).Concat(result.AnalysisResult.ParseResult.Errors));
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private DeepAnalysisResult m_daResult = null;
        private IEnumerable<VHDLError> m_errors = null;

        void OnDocumentClosed(object sender, VHDLDocumentEventArgs e)
        {
            if(e.Document == m_vhdlDoc)
            {
                m_vhdlDoc.Parser.DeepAnalysisComplete -= DeepAnalysisComplete;
            }
        }
        void DeepAnalysisComplete(object sender, DeepAnalysisResultEventArgs e)
        {
            if (e.Result.Snapshot == null)
                return;

            DeepAnalysisResult result = e.Result;
            ReParse(e.Result, result.Errors.Concat(result.AnalysisResult.Errors).Concat(result.AnalysisResult.ParseResult.Errors));
        }

        void ReParse(DeepAnalysisResult daResult, IEnumerable<VHDLError> errors)
        {
			//determine the changed span, and send a changed event with the new spans

            //  Get old errors translated to the new snapshot
			IEnumerable<Span> oldSpans;
			if (m_errors == null)
				oldSpans = Enumerable.Empty<Span>();
			else
				oldSpans = m_errors.Select(e => new SnapshotSpan(m_daResult.Snapshot, e.Span)
					.TranslateTo(daResult.Snapshot, SpanTrackingMode.EdgeExclusive)
					.Span).OrderBy(x => x.Start);

            IEnumerable<Span> newSpans = errors.Select(e => e.Span).OrderBy(x => x.Start);

            //  This needs to be sorted or some errors dont show
            NormalizedSpanCollection newSpanCollection = new NormalizedSpanCollection(newSpans);
			NormalizedSpanCollection oldSpanCollection = new NormalizedSpanCollection(oldSpans);

            //System.Diagnostics.Debug.WriteLine(string.Format("ErrorTagger reparse {}, {} errors", m_vhdlDoc.Filepath, newSpanCollection.Count));

            //the changed regions are regions that appear in one set or the other, but not both.
            NormalizedSpanCollection removed = NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

            Span? change = null;

            if (removed.Count > 0)
                change = removed.First().Union(removed.Last());

            if (newSpans.Any())
            {
                if (change != null)
                    change = change.Value.Union(newSpans.First()).Union(newSpans.Last());
                else
                    change = newSpans.First().Union(newSpans.Last());
            }

			m_daResult = daResult;
            m_errors = errors;

            if (change.HasValue)
            {
            	TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(daResult.Snapshot, change.Value)));
            }
        }
        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            DeepAnalysisResult daResult = m_daResult;
            IEnumerable<VHDLError> errors = m_errors;
			if (m_daResult == null)
				yield break;

			ITextSnapshot currentSnapshot = null;
			if (spans.Count > 0)
				currentSnapshot = spans.First().Snapshot;

            foreach (VHDLError error in errors)
            {
				SnapshotSpan errorSpan = new SnapshotSpan(m_daResult.Snapshot, error.Span).TranslateTo(currentSnapshot, SpanTrackingMode.EdgePositive);
				if (spans.IntersectsWith(errorSpan))
				{
					yield return new TagSpan<IErrorTag>(
						errorSpan,
						new ErrorTag(error.ErrorType, error.Message));
				}
            }
        }
    }
}