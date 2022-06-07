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
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Windows.Media;

namespace vhdl4vs
{
	[Export(typeof(EditorFormatDefinition))]
	[Name("MarkerFormatDefinition/VHDLHighlightWordFormatDefinition")]
	[UserVisible(true)]
	class VHDLHighlightWordFormatDefinition
		: MarkerFormatDefinition
	{
		public VHDLHighlightWordFormatDefinition()
		{
			BackgroundColor = Color.FromRgb(226, 230, 214);
			ForegroundColor = Color.FromRgb(226, 230, 214);
			DisplayName = "VHDL Highlight Word";
			ZOrder = 5;
		}
	}
	[Export(typeof(EditorFormatDefinition))]
	[Name("MarkerFormatDefinition/VHDLHighlightDeclarationWordFormatDefinition")]
	[UserVisible(true)]
	class VHDLHighlightDeclarationWordFormatDefinition
		: MarkerFormatDefinition
	{
		public VHDLHighlightDeclarationWordFormatDefinition()
		{
			BackgroundColor = Color.FromRgb(226, 230, 214);
			ForegroundColor = Color.FromArgb(23, 0, 0, 0);
			DisplayName = "VHDL Highlight Declaration Word";
			ZOrder = 5;
		}
	}
	class VHDLHighlightWordTag
		: TextMarkerTag
	{
		public VHDLHighlightWordTag()
			: base("MarkerFormatDefinition/VHDLHighlightWordFormatDefinition")
		{
		}
		protected VHDLHighlightWordTag(string type)
			: base(type)
		{
		}
	}
	class VHDLHighlightDeclarationWordTag
		: VHDLHighlightWordTag
	{
		public VHDLHighlightDeclarationWordTag()
			: base("MarkerFormatDefinition/VHDLHighlightDeclarationWordFormatDefinition")
		{

		}
	}
	class VHDLHighlightWordTagger
		: ITagger<VHDLHighlightWordTag>
	{
		ITextView View { get; set; }
		ITextBuffer SourceBuffer { get; set; }
		ITextSearchService TextSearchService { get; set; }
		ITextStructureNavigator TextStructureNavigator { get; set; }
		//NormalizedSnapshotSpanCollection WordSpans { get; set; }
		//SnapshotSpan? CurrentWord { get; set; }
		//SnapshotPoint RequestedPoint { get; set; }
		object updateLock = new object();
		VHDLDocumentTable DocumentTable { get; set; }
		VHDLDocument Document { get; set; }

		public VHDLHighlightWordTagger(ITextView view, ITextBuffer sourceBuffer, VHDLDocumentTable documentTable, ITextSearchService textSearchService, ITextStructureNavigator textStructureNavigator)
		{
			this.View = view;
			this.SourceBuffer = sourceBuffer;
			DocumentTable = documentTable;
			Document = DocumentTable.GetOrAddDocument(sourceBuffer);
			this.TextSearchService = textSearchService;
			this.TextStructureNavigator = textStructureNavigator;
			//this.WordSpans = new NormalizedSnapshotSpanCollection();
			//this.CurrentWord = null;
			this.View.Caret.PositionChanged += CaretPositionChanged;
			this.View.LayoutChanged += ViewLayoutChanged;
			Document.Parser.AnalysisComplete += OnAnalysisComplete;

		}

		void OnAnalysisComplete(object sender, AnalysisResultEventArgs e)
		{
			Request req = CurrentRequest;
			if (req != null && !req.Done)
			{
				UpdateWordAdornments();
			}

		}
		void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			// If a new snapshot wasn't generated, then skip this layout 
			if (e.NewSnapshot != e.OldSnapshot)
			{
				UpdateAtCaretPosition(View.Caret.Position);
			}
		}

		void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
		{
			UpdateAtCaretPosition(e.NewPosition);
		}

		class Request
		{
			public SnapshotPoint Point { get; set; }
			public SnapshotSpan Word { get; set; }
			public bool Done { get; set; }
			public SnapshotSpan? Declaration { get; set; }
			public NormalizedSnapshotSpanCollection References { get; set; }
		}

		private Request CurrentRequest = null;
		async void UpdateAtCaretPosition(CaretPosition caretPosition)
		{
			/*SnapshotPoint? point = caretPosition.Point.GetPoint(SourceBuffer, caretPosition.Affinity);

			if (!point.HasValue)
				return;

			// If the new caret position is still within the current word (and on the same snapshot), we don't need to check it 
			if (CurrentWord.HasValue
				&& CurrentWord.Value.Snapshot == View.TextSnapshot
				&& point.Value >= CurrentWord.Value.Start
				&& point.Value <= CurrentWord.Value.End)
			{
				return;
			}

			RequestedPoint = point.Value;
			UpdateWordAdornments();*/

			SnapshotPoint? point = caretPosition.Point.GetPoint(SourceBuffer, caretPosition.Affinity);

			if (!point.HasValue)
			{
				CurrentRequest = null;
				return;
			}

			// If the new caret position is still within the current word (and on the same snapshot), we don't need to check it 
			Request req = CurrentRequest;
			if (req != null &&
				req.Word.Snapshot == View.TextSnapshot
				&& point >= req.Word.Start
				&& point <= req.Word.End)
			{
				return;
			}

			Request newReq = new Request();
			newReq.Declaration = null;
			newReq.References = null;
			newReq.Point = point.Value;
			newReq.Word = TextStructureNavigator.GetExtentOfWord(point.Value).Span;
			newReq.Done = false;
			CurrentRequest = newReq;
			await Task.Run(UpdateWordAdornments);
		}

		void UpdateWordAdornments()
		{
			Request req = CurrentRequest;
			if (req == null || req.Done)
				return;

			//SnapshotPoint currentRequest = req.Point;
			List<SnapshotSpan> wordSpans = new List<SnapshotSpan>();

			//	- Find declaration
			//	- Analyse current file and find all references to declaration

			EventHandler<SnapshotSpanEventArgs> tempEvent = null;

			AnalysisResult aresult = Document.Parser.AResult;
			if (aresult.Snapshot != req.Point.Snapshot)
			{
				req.References = new NormalizedSnapshotSpanCollection();
				tempEvent = TagsChanged;
				tempEvent?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(SourceBuffer.CurrentSnapshot, 0, SourceBuffer.CurrentSnapshot.Length)));
				return;
			}

			//int position = req.Point.TranslateTo(aresult.Snapshot, PointTrackingMode.Positive);
			VHDLNameVisitor nameVisitor = new VHDLNameVisitor(req.Point.Position);
			Antlr4.Runtime.ParserRuleContext nameContext = nameVisitor.Visit(aresult.Tree);
			if(nameContext == null)
			{
				req.Done = true;
				tempEvent = TagsChanged;
				tempEvent?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(SourceBuffer.CurrentSnapshot, 0, SourceBuffer.CurrentSnapshot.Length)));
				return;
			}

			VHDLNameResolveVisitor resolveVisitor = new VHDLNameResolveVisitor(aresult, nameContext, req.Point.Position);
			VHDLDeclaration decl = resolveVisitor.Visit(nameContext);
			if (decl == null)
			{
				req.References = new NormalizedSnapshotSpanCollection();
				req.Done = true;
				//SynchronousUpdate(req.Point, new NormalizedSnapshotSpanCollection(), null);
				tempEvent = TagsChanged;
				tempEvent?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(SourceBuffer.CurrentSnapshot, 0, SourceBuffer.CurrentSnapshot.Length)));
				return;
			}
			/*AnalysisResult aresult = Document.Parser.AResult;
			aresult.*/


			//Find all words in the buffer like the one the caret is on
			//TextExtent word = TextStructureNavigator.GetExtentOfWord(req.Point);
			//bool foundWord = true;
			//If we've selected something not worth highlighting, we might have missed a "word" by a little bit
			/*if (!WordExtentIsValid(currentRequest, word))
			{
				//Before we retry, make sure it is worthwhile 
				if (word.Span.Start != currentRequest
					 || currentRequest == currentRequest.GetContainingLine().Start
					 || char.IsWhiteSpace((currentRequest - 1).GetChar()))
				{
					foundWord = false;
				}
				else
				{
					// Try again, one character previous.  
					//If the caret is at the end of a word, pick up the word.
					word = TextStructureNavigator.GetExtentOfWord(currentRequest - 1);

					//If the word still isn't valid, we're done 
					if (!WordExtentIsValid(currentRequest, word))
						foundWord = false;
				}
			}*/

			/*if (!foundWord)
			{
				//If we couldn't find a word, clear out the existing markers
				SynchronousUpdate(currentRequest, new NormalizedSnapshotSpanCollection(), null);
				return;
			}*/


			SnapshotSpan currentWord = req.Word;
			//If this is the current word, and the caret moved within a word, we're done. 
			//if (CurrentWord.HasValue && currentWord == CurrentWord)
			//	return;

			VHDLReferenceVisitor referenceVisitor = new VHDLReferenceVisitor(decl, aresult);
			referenceVisitor.Visit(aresult.Tree);
			Span nameSpan = new Span(decl.NameContext.Start.StartIndex, decl.NameContext.Stop.StopIndex - decl.NameContext.Start.StartIndex + 1);
			req.Declaration = new SnapshotSpan(aresult.Snapshot, nameSpan);
			req.References = new NormalizedSnapshotSpanCollection(aresult.Snapshot, referenceVisitor.References.Where((s) => s != nameSpan).Select((s) => new Span(s.Start, s.Length + 1)).ToList());
			req.Done = true;

			tempEvent = TagsChanged;
			tempEvent?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(SourceBuffer.CurrentSnapshot, 0, SourceBuffer.CurrentSnapshot.Length)));

			//wordSpans.AddRange(referenceVisitor.References.Select((s) => new SnapshotSpan(aresult.Snapshot)
			//Find the new spans
			//FindData findData = new FindData(currentWord.GetText(), currentWord.Snapshot);
			//findData.FindOptions = FindOptions.WholeWord | FindOptions.MatchCase;

			//wordSpans.Add(currentWord);
			//wordSpans.AddRange(TextSearchService.FindAll(findData));

			//If another change hasn't happened, do a real update 
			//if (req.Point == RequestedPoint)
			//	SynchronousUpdate(req.Point, new NormalizedSnapshotSpanCollection(wordSpans), currentWord);
		}
		static bool WordExtentIsValid(SnapshotPoint currentRequest, TextExtent word)
		{
			return word.IsSignificant
				&& currentRequest.Snapshot.GetText(word.Span).Any(c => char.IsLetter(c));
		}
		void SynchronousUpdate(SnapshotPoint currentRequest, NormalizedSnapshotSpanCollection newSpans, SnapshotSpan? newCurrentWord)
		{
			/*lock (updateLock)
			{
				if (currentRequest != RequestedPoint)
					return;

				WordSpans = newSpans;
				CurrentWord = newCurrentWord;
				*/
				var tempEvent = TagsChanged;
				tempEvent?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(SourceBuffer.CurrentSnapshot, 0, SourceBuffer.CurrentSnapshot.Length)));
			//}
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public IEnumerable<ITagSpan<VHDLHighlightWordTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			Request req = CurrentRequest;
			if (req == null || spans.Count == 0)
				yield break;

			SnapshotSpan? declSpan = req.Declaration;
			if (declSpan != null && spans.Count > 0 && declSpan.Value.Snapshot != spans[0].Snapshot)
				declSpan = declSpan.Value.TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeExclusive);

			// return declaration span with a more highlighted tag
			if(declSpan != null && spans.OverlapsWith(declSpan.Value))
				yield return new TagSpan<VHDLHighlightDeclarationWordTag>(declSpan.Value, new VHDLHighlightDeclarationWordTag());

			if (req.References == null || req.References.Count == 0)
				yield break;

			NormalizedSnapshotSpanCollection wordSpans = req.References;

			// If the requested snapshot isn't the same as the one our words are on, translate our spans to the expected snapshot 
			if (spans.Count > 0 && req.References.Count > 0 &&
				spans[0].Snapshot != wordSpans[0].Snapshot)
			{
				wordSpans = new NormalizedSnapshotSpanCollection(
					wordSpans.Select(span => span.TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeExclusive)));
			}

			foreach (SnapshotSpan span in NormalizedSnapshotSpanCollection.Overlap(wordSpans, spans))
				yield return new TagSpan<VHDLHighlightWordTag>(span, new VHDLHighlightWordTag());

			/*if (CurrentWord == null)
				yield break;

			// Hold on to a "snapshot" of the word spans and current word, so that we maintain the same
			// collection throughout
			SnapshotSpan currentWord = CurrentWord.Value;
			NormalizedSnapshotSpanCollection wordSpans = WordSpans;

			if (spans.Count == 0 || wordSpans.Count == 0)
				yield break;

			// If the requested snapshot isn't the same as the one our words are on, translate our spans to the expected snapshot 
			if (spans[0].Snapshot != wordSpans[0].Snapshot)
			{
				wordSpans = new NormalizedSnapshotSpanCollection(
					wordSpans.Select(span => span.TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeExclusive)));

				currentWord = currentWord.TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeExclusive);
			}

			// First, yield back the word the cursor is under (if it overlaps) 
			// Note that we'll yield back the same word again in the wordspans collection; 
			// the duplication here is expected. 
			if (spans.OverlapsWith(new NormalizedSnapshotSpanCollection(currentWord)))
				yield return new TagSpan<VHDLHighlightWordTag>(currentWord, new VHDLHighlightWordTag());

			// Second, yield all the other words in the file 
			foreach (SnapshotSpan span in NormalizedSnapshotSpanCollection.Overlap(spans, wordSpans))
			{
				yield return new TagSpan<VHDLHighlightWordTag>(span, new VHDLHighlightWordTag());
			}*/
		}
	}
}
