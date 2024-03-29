﻿/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace vhdl4vs
{
	class VHDLQuickFormatVisitor
		: vhdlBaseVisitor<bool>
	{
		public VHDLQuickFormatVisitor(TextBlock textBlock)
		{
			FormatResult = textBlock;
		}

		private string m_text = "";
		public string Text => m_text;
		private string m_lastText = null;
		public TextBlock FormatResult
		{
			get; set;
		}

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
		public override bool VisitTerminal(ITerminalNode node)
		{
			string textToInsert = node.GetText();

			char? lastChar = null;
			if(!string.IsNullOrEmpty(m_lastText)) lastChar = m_lastText.Last();

			char? nextChar = null;
			if (!string.IsNullOrEmpty(textToInsert)) nextChar = textToInsert.First();

			if (((IsAlphaNum_(lastChar) || lastChar == '\'' || lastChar == '"') && (IsAlphaNum_(nextChar) || nextChar == '\'' || nextChar == '"'))
				|| (IsAlphaNum_(lastChar) && nextChar == ':')
				|| (lastChar == ':' && IsAlphaNum_(nextChar))
				|| (lastChar == ',')
				|| (lastChar == ';')
				|| (lastChar == ')' && IsAlphaNum_(nextChar))
				|| (IsAlphaNum_(lastChar) && nextChar == '-'))
				textToInsert = textToInsert.Insert(0, " ");
			if (vhdlLexer.DefaultVocabulary.GetSymbolicName(node.Symbol.Type) == "STRING_LITERAL" || vhdlLexer.DefaultVocabulary.GetSymbolicName(node.Symbol.Type) == "CHARACTER_LITERAL")
			{
				m_text += textToInsert;
				FormatResult?.Inlines.Add(VHDLQuickInfoHelper.str(textToInsert));
			}
			else if (VHDLLanguageUtils.Keywords.Contains(node.GetText()))
			{
				m_text += textToInsert;
				FormatResult?.Inlines.Add(VHDLQuickInfoHelper.keyword(textToInsert.ToLower()));
			}
			else
			{
				m_text += textToInsert;
				FormatResult?.Inlines.Add(VHDLQuickInfoHelper.text(textToInsert));
			}

			m_lastText = node.GetText();

			return base.VisitTerminal(node);
		}
	};
}
