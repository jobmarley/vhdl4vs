using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using System.Threading;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;

namespace vhdl4vs
{
    class VHDLOutliningVisitor
        : vhdlBaseVisitor<bool>
    {
        private ITextSnapshot m_snapshot = null;
        public VHDLOutliningVisitor(ITextSnapshot snapshot)
        {
            m_snapshot = snapshot;
        }

        void AddRegion(Antlr4.Runtime.ParserRuleContext context, Antlr4.Runtime.IToken start, Antlr4.Runtime.IToken end)
        {
            if (start != null && end != null)
            {
                VHDLRegion region = new VHDLRegion();
                region.OutlineSpan = new SnapshotSpan(m_snapshot, start.StopIndex + 1, end.StopIndex - start.StopIndex);
                region.Tip = m_snapshot.GetText(context.Start.StartIndex, context.Stop.StopIndex - context.Start.StartIndex);
                m_regions.Add(region);
            }
        }
        void AddGuideline(Antlr4.Runtime.IToken start, Antlr4.Runtime.IToken end)
        {
            if (start != null && end != null)
            {
                VHDLGuideline guideline = new VHDLGuideline();
                guideline.Points = new List<SnapshotPoint>()
                {
                    new SnapshotPoint(m_snapshot, start.StartIndex),
                    new SnapshotPoint(m_snapshot, end.StartIndex),
                };
                m_guidelines.Add(guideline);
            }
        }
        public override bool VisitLoop_statement([NotNull] vhdlParser.Loop_statementContext context)
        {
            try
            { 
                Antlr4.Runtime.IToken start = null;
                Antlr4.Runtime.IToken end = null;
                Antlr4.Runtime.IToken guidelineStart = null;
                Antlr4.Runtime.IToken guidelineEnd = null;

                guidelineStart = context.label_colon()?.Start;
                if (guidelineStart == null)
                    guidelineStart = context.iteration_scheme()?.Start;
                guidelineEnd = context.END()?.Symbol;

                start = context.iteration_scheme()?.condition()?.Stop;
                if (start == null)
                    start = context.iteration_scheme()?.parameter_specification()?.Stop;
                end = context.SEMI()?.Symbol;


                AddRegion(context, start, end);
                AddGuideline(guidelineStart, guidelineEnd);
                return base.VisitLoop_statement(context);
            }
            catch (Exception)
			{
			}
            return false;
        }
        public override bool VisitCase_statement([NotNull] vhdlParser.Case_statementContext context)
        {
            try
            { 
                Antlr4.Runtime.IToken start = null;
                Antlr4.Runtime.IToken end = null;
                Antlr4.Runtime.IToken guidelineStart = null;
                Antlr4.Runtime.IToken guidelineEnd = null;

                guidelineStart = context.label_colon()?.Start;
                if(guidelineStart == null && context.CASE()?.Length > 0)
                    guidelineStart = context.CASE()?[0]?.Symbol;
                guidelineEnd = context.END()?.Symbol;

                start = context.expression()?.Stop;
                end = context.SEMI()?.Symbol;


                AddRegion(context, start, end);
                AddGuideline(guidelineStart, guidelineEnd);
                return base.VisitCase_statement(context);
            }
            catch (Exception)
			{
			}
            return false;
        }
        public override bool VisitEntity_declaration([NotNull] vhdlParser.Entity_declarationContext context)
        {
            try
            { 
                Antlr4.Runtime.IToken start = null;
                Antlr4.Runtime.IToken end = null;
                Antlr4.Runtime.IToken guidelineStart = null;
                Antlr4.Runtime.IToken guidelineEnd = null;

                if (context.ENTITY()?.Length > 0)
                    guidelineStart = context.ENTITY()?[0]?.Symbol;
                guidelineEnd = context.END()?.Symbol;

                if (context.identifier()?.Length > 0)
                    start = context.identifier()?[0]?.Stop;
                end = context.SEMI()?.Symbol;


                AddRegion(context, start, end);
                AddGuideline(guidelineStart, guidelineEnd);
                return base.VisitEntity_declaration(context);
            }
            catch (Exception)
			{
            }
            return false;
        }

        public override bool VisitArchitecture_body([NotNull] vhdlParser.Architecture_bodyContext context)
        {
            try
            { 
                Antlr4.Runtime.IToken start = null;
                Antlr4.Runtime.IToken end = null;
                Antlr4.Runtime.IToken guidelineStart = null;
                Antlr4.Runtime.IToken guidelineEnd = null;

                if (context.ARCHITECTURE()?.Length > 0)
                    guidelineStart = context.ARCHITECTURE()?[0]?.Symbol;
                guidelineEnd = context.END()?.Symbol;

                if (context.identifier()?.Length > 0)
                    start = context.identifier()?[0]?.Stop;
                end = context.SEMI()?.Symbol;

                AddRegion(context, start, end);
                AddGuideline(guidelineStart, guidelineEnd);
                return base.VisitArchitecture_body(context);
            }
            catch (Exception)
			{
            }
            return false;
        }

        public override bool VisitProcess_statement([NotNull] vhdlParser.Process_statementContext context)
        {
            try
            {
                Antlr4.Runtime.IToken start = null;
                Antlr4.Runtime.IToken end = null;
                Antlr4.Runtime.IToken guidelineStart = null;
                Antlr4.Runtime.IToken guidelineEnd = null;

                if (context.PROCESS()?.Length > 0)
                    start = context.PROCESS()?[0]?.Symbol;
                end = context.SEMI()?.Symbol;

                guidelineStart = context.BEGIN()?.Symbol;
                guidelineEnd = context.END()?.Symbol;

                AddRegion(context, start, end);
                AddGuideline(guidelineStart, guidelineEnd);
                return base.VisitProcess_statement(context);
            }
            catch (Exception)
			{
            }
            return false;
        }

        public override bool VisitPackage_body([NotNull] vhdlParser.Package_bodyContext context)
        {
            try
            { 
                Antlr4.Runtime.IToken start = null;
                Antlr4.Runtime.IToken end = null;
                Antlr4.Runtime.IToken guidelineStart = null;
                Antlr4.Runtime.IToken guidelineEnd = null;

                if (context.identifier()?.Length > 0)
                    start = context.identifier()?[0]?.Stop;
                end = context.SEMI()?.Symbol;

                if (context.PACKAGE()?.Length > 0)
                    guidelineStart = context.PACKAGE()?[0]?.Symbol;
                guidelineEnd = context.END()?.Symbol;

                AddRegion(context, start, end);
                AddGuideline(guidelineStart, guidelineEnd);
                return base.VisitPackage_body(context);
            }
            catch (Exception)
			{
            }
            return false;
        }

        public override bool VisitPackage_declaration([NotNull] vhdlParser.Package_declarationContext context)
        {
            try
            { 
                Antlr4.Runtime.IToken start = null;
                Antlr4.Runtime.IToken end = null;
                Antlr4.Runtime.IToken guidelineStart = null;
                Antlr4.Runtime.IToken guidelineEnd = null;

                if (context.identifier()?.Length > 0)
                    start = context.identifier()?[0]?.Stop;
                end = context.SEMI()?.Symbol;

                if (context.PACKAGE()?.Length > 0)
                    guidelineStart = context.PACKAGE()?[0]?.Symbol;
                guidelineEnd = context.END()?.Symbol;

                AddRegion(context, start, end);
                AddGuideline(guidelineStart, guidelineEnd);
                return base.VisitPackage_declaration(context);
            }
            catch (Exception)
			{
            }
            return false;
        }

        public override bool VisitSubprogram_body([NotNull] vhdlParser.Subprogram_bodyContext context)
        {
            try
            { 
                Antlr4.Runtime.IToken start = null;
                Antlr4.Runtime.IToken end = null;
                Antlr4.Runtime.IToken guidelineStart = null;
                Antlr4.Runtime.IToken guidelineEnd = null;

                guidelineStart = context.Start;
                guidelineEnd = context.END()?.Symbol;
            
                start = context.subprogram_specification()?.function_specification()?.designator()?.Stop;
			    if (start != null)
                    start = context.subprogram_specification()?.procedure_specification()?.designator()?.Stop;
                end = context.SEMI()?.Symbol;

                AddRegion(context, start, end);
                AddGuideline(guidelineStart, guidelineEnd);
                return base.VisitSubprogram_body(context);
            }
            catch (Exception)
			{
            }
            return false;
        }

        public override bool VisitIf_statement([NotNull] vhdlParser.If_statementContext context)
        {
            try
            { 
                Antlr4.Runtime.IToken start = null;
                Antlr4.Runtime.IToken end = null;
                Antlr4.Runtime.IToken guidelineStart = null;
                Antlr4.Runtime.IToken guidelineEnd = null;

                if (context.IF()?.Length > 0)
                    guidelineStart = context.IF()?[0]?.Symbol;
                guidelineEnd = context.END()?.Symbol;

                if (context.condition()?.Length > 0)
                    start = context.condition()[0]?.Stop;

                if (context.ELSIF()?.Length > 0 || context.ELSE() != null)
                    end = context.sequence_of_statements()?[0]?.Stop;
                else
                    end = context.SEMI()?.Symbol;

                AddRegion(context, start, end);
                AddGuideline(guidelineStart, guidelineEnd);

                for (int i = 0; i < context.ELSIF()?.Length; ++i)
                {
                    start = null;
                    end = null;

                    if (context.condition()?.Length > i + 1)
                        start = context.condition()?[i + 1]?.Stop;

                    if (context.ELSIF()?.Length > i + 1 || context.ELSE() != null)
                        end = context.sequence_of_statements()?[i + 1]?.Stop;
                    else
                        end = context.SEMI()?.Symbol;

                    AddRegion(context, start, end);
                }

                if(context.ELSE() != null)
                {
                    start = context.ELSE()?.Symbol;
                    end = context.SEMI()?.Symbol;

                    AddRegion(context, start, end);
                }
                return base.VisitIf_statement(context);
            }
            catch (Exception)
			{
			}
            return false;
        }

        private List<VHDLRegion> m_regions = new List<VHDLRegion>();
        public IList<VHDLRegion> Regions
        {
            get
            {
                return m_regions;
            }
        }
        private List<VHDLGuideline> m_guidelines = new List<VHDLGuideline>();
        public IList<VHDLGuideline> Guidelines
        {
            get
            {
                return m_guidelines;
            }
        }
    }
    class VHDLGuideline
    {
        public IList<SnapshotPoint> Points { get; set; }
    }
    class VHDLRegion
    {
        /*public int StartLine { get; set; }
        public int StartCol { get; set; }
        public int EndLine { get; set; }
        public int EndCol { get; set; }*/
		public SnapshotSpan OutlineSpan { get; set; }
        public string Tip { get; set; }
    }

    internal sealed class VHDLOutliningTagger
        : ITagger<IOutliningRegionTag>
    {
        //WeakReference<ITextBuffer> m_buffer = null;
        ///WeakReference<ITextSnapshot> m_snapshot = null;
        IEnumerable<VHDLRegion> m_regions = new List<VHDLRegion>();

		VHDLDocument m_document = null;
		ParseResult m_parseResult = null;

        ITextDocumentFactoryService m_documentService = null;
		VHDLDocumentTable m_vhdlDocTable = null;



		public VHDLOutliningTagger(ITextBuffer buffer, ITextDocumentFactoryService documentService, VHDLDocumentTable vhdlDocTable)
        {
            m_documentService = documentService;
			m_vhdlDocTable = vhdlDocTable;

			m_document = m_vhdlDocTable.GetOrAddDocument(buffer);
			m_document.Parser.ParseComplete += ParseComplete;
			m_vhdlDocTable.DocumentClosed += OnDocumentClosed;
			
			if (m_document.TextBuffer != buffer)
				throw new Exception();

			ParseResult result = m_document.Parser.PResult;
			if (result != null)
			{
				m_parseResult = result;
				ReParse(result);
			}
        }
		private void OnDocumentClosed(object sender, VHDLDocumentEventArgs e)
		{
			e.Document.Parser.ParseComplete -= ParseComplete;
			m_vhdlDocTable.DocumentClosed -= OnDocumentClosed;
		}

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            IEnumerable<VHDLRegion> currentRegions = m_regions;
            ITextSnapshot currentSnapshot = null;
			if (spans.Count > 0)
				currentSnapshot = spans[0].Snapshot;
			//m_snapshot.TryGetTarget(out currentSnapshot);

            foreach (var region in currentRegions)
            {
				if (spans.IntersectsWith(region.OutlineSpan.TranslateTo(currentSnapshot, SpanTrackingMode.EdgePositive)))
                {
                    //the region starts at the beginning of the "[", and goes until the *end* of the line that contains the "]".
                    yield return new TagSpan<IOutliningRegionTag>(region.OutlineSpan,
                        new OutliningRegionTag(false, false, "...", region.Tip));
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        void ParseComplete(object sender, ParseResultEventArgs e)
        {
			// If this isn't the most up-to-date version of the buffer, then ignore it for now (we'll eventually get another change event).
			/*if (e.After != buffer.CurrentSnapshot)
				return;*/
			m_parseResult = e.Result;
			ReParse(e.Result);
        }

        VHDLRegion RegionFromComment(Span s, ITextSnapshot snapshot)
        {
            SnapshotSpan span = new SnapshotSpan(snapshot, s);
            VHDLRegion r = new VHDLRegion();
            r.OutlineSpan = span;
            string text = span.GetText();
            if (text.Length < 15)
                r.Tip = text;
            else
                r.Tip = text.Substring(12) + "...";
            return r;
        }
		void ReParse(ParseResult newResult)
        {
			if (newResult == null || newResult.Snapshot == null)
                return;

            VHDLOutliningVisitor visitor = new VHDLOutliningVisitor(newResult.Snapshot);
            visitor.Visit(newResult.Tree);
			IEnumerable<VHDLRegion> newRegions = visitor.Regions.Concat(newResult.Comments.Select(x => RegionFromComment(x, newResult.Snapshot)));

            //determine the changed span, and send a changed event with the new spans
            List<Span> oldSpans = new List<Span>(m_regions.Select(r => r.OutlineSpan.TranslateTo(newResult.Snapshot, SpanTrackingMode.EdgeExclusive).Span));
			List<Span> newSpans = new List<Span>(newRegions.Select(r => r.OutlineSpan.Span));

			NormalizedSpanCollection oldSpanCollection = new NormalizedSpanCollection(oldSpans);
            NormalizedSpanCollection newSpanCollection = new NormalizedSpanCollection(newSpans);

            //the changed regions are regions that appear in one set or the other, but not both.
            NormalizedSpanCollection removed = NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

            int changeStart = int.MaxValue;
            int changeEnd = -1;

            if (removed.Count > 0)
            {
                changeStart = removed[0].Start;
                changeEnd = removed[removed.Count - 1].End;
            }

            if (newSpans.Count > 0)
            {
                changeStart = Math.Min(changeStart, newSpans[0].Start);
                changeEnd = Math.Max(changeEnd, newSpans[newSpans.Count - 1].End);
            }

			m_regions = newRegions;

            if (changeStart <= changeEnd)
            {
                if (this.TagsChanged != null)
                    this.TagsChanged(this, new SnapshotSpanEventArgs(
                        new SnapshotSpan(newResult.Snapshot, Span.FromBounds(changeStart, changeEnd))));
            }
        }

        /*static SnapshotSpan AsSnapshotSpan(VHDLRegion region, ITextSnapshot snapshot)
        {
            var startLine = snapshot.GetLineFromLineNumber(region.StartLine);
            var endLine = (region.StartLine == region.EndLine) ? startLine
                 : snapshot.GetLineFromLineNumber(region.EndLine);
            return new SnapshotSpan(startLine.Start + region.StartCol, endLine.End);
        }*/
    }
}
