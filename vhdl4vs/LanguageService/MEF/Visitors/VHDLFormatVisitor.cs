using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace vhdl4vs
{
	class VHDLFormatVisitor
		: vhdlBaseVisitor<bool>
	{
		private int m_indent = 0;
		private ITextSnapshot m_snapshot = null;
		private ITextEdit m_edit = null;
		public VHDLFormatVisitor(ITextSnapshot snapshot)
		{
			m_snapshot = snapshot;
			m_edit = m_snapshot.TextBuffer.CreateEdit(); 
		}

		public ITextEdit Edit { get { return m_edit; } }
		public ITextSnapshot Snapshot { get { return m_snapshot; } }
		protected override bool DefaultResult
		{
			get { return true; }
		}
		protected override bool AggregateResult(bool aggregate, bool nextResult)
		{
			return aggregate && nextResult;
		}

		protected override bool ShouldVisitNextChild(IRuleNode node, bool currentResult)
		{
			return true;
		}

		private bool IsAlphaNum_(char? c)
		{
			if (c == null)
				return false;
			return char.IsLetterOrDigit(c.Value) || c == '_';
		}

		// True if start of line contains non-whitespace characters
		KeyValuePair<bool, Span> GetWhitespaceBefore(int index)
		{
			string text = m_snapshot.GetText();
			
			int iEnd = index;

			int iLineStart = m_snapshot.GetLineFromPosition(index).Extent.Start;
			while (index > iLineStart
				&& char.IsWhiteSpace(text[index - 1]))
				--index;

			return new KeyValuePair<bool, Span>(index == iLineStart, new Span(index, iEnd - index));
		}

		void FixIndent(Antlr4.Runtime.IToken token, int indent)
		{
			if (token == null)
				return;

			KeyValuePair<bool, Span> span = GetWhitespaceBefore(token.StartIndex);

			string indentText = new string('\t', indent);
			bool alreadyHasNewline = span.Key;
			//if (!span.Value.IsEmpty)
			//{
				if (!alreadyHasNewline)
					m_edit.Replace(span.Value, Environment.NewLine + indentText);
				else if (m_snapshot.GetText(span.Value) != indentText)
					m_edit.Replace(span.Value, indentText);
			/*}
			else
			{
				if (!alreadyHasNewline)
					m_edit.Insert(token.StartIndex, Environment.NewLine + indentText);
				else
					m_edit.Insert(token.StartIndex, indentText);
			}*/
		}
		void FixIndent(Antlr4.Runtime.Tree.ITerminalNode node, int indent)
		{
			FixIndent(node?.Symbol, indent);
		}

		//	Ensure the given syntax element is on a new line with the given indent
		//	Create an edit if necessary
		void FixIndent(Antlr4.Runtime.ParserRuleContext context, int indent)
		{
			FixIndent(context?.Start, indent);
		}
		public override bool VisitBlock_declarative_item([NotNull] vhdlParser.Block_declarative_itemContext context)
		{
			FixIndent(context, m_indent + 1);

			return base.VisitBlock_declarative_item(context);
		}
		public override bool VisitArchitecture_body([NotNull] vhdlParser.Architecture_bodyContext context)
		{
			FixIndent(context.BEGIN(), m_indent);
			FixIndent(context.END(), m_indent);
			return base.VisitArchitecture_body(context);
		}
		/*public override bool VisitTerminal(ITerminalNode node)
		{
			string textToInsert = node.GetText();

			char? lastChar = null;
			if (!string.IsNullOrEmpty(m_lastText)) lastChar = m_lastText.Last();

			char? nextChar = null;
			if (!string.IsNullOrEmpty(textToInsert)) nextChar = textToInsert.First();

			if ((IsAlphaNum_(lastChar) && IsAlphaNum_(nextChar))
				|| (IsAlphaNum_(lastChar) && nextChar == ':')
				|| (lastChar == ':' && IsAlphaNum_(nextChar))
				|| (lastChar == ',')
				|| (lastChar == ';')
				|| (IsAlphaNum_(lastChar) && nextChar == '-'))
				textToInsert = textToInsert.Insert(0, " ");

			if (VHDLClassifier.IsKeyword(node.GetText()))
				FormatResult.Inlines.Add(VHDLQuickInfoHelper.keyword(textToInsert));
			else
				FormatResult.Inlines.Add(VHDLQuickInfoHelper.text(textToInsert));

			m_lastText = node.GetText();

			return base.VisitTerminal(node);
		}*/
	};
}
