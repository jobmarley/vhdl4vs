﻿/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Antlr4.Runtime;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs
{
	internal class VHDLReverseLexer
	{
		private ITextSnapshot m_snapshot = null;
		public ITextSnapshot Snapshot => m_snapshot;
		private int m_lineIndex = -1;
		private IList<IToken> m_lineTokens = null;
		private int m_tokenIndex = -1;
		private int m_index = -1;

		public VHDLReverseLexer(ITextSnapshot snapshot)
		{
			m_snapshot = snapshot;
		}

		public void SetIndex(int index)
		{
			m_index = index;

			ITextSnapshotLine line = m_snapshot.GetLineFromPosition(index);
			m_lineIndex = line.LineNumber;
			vhdlLexer lexer = new vhdlLexer(new AntlrInputStream(line.GetText()));
			m_lineTokens = lexer.GetAllTokens();

			int column = index - line.Start;
			m_tokenIndex = -1;
			for (int i = 0; i < m_lineTokens.Count(); ++i)
			{
				if (m_lineTokens[i].Column < column)
					m_tokenIndex = i;
				else
					break;
			}
		}

		private bool LexPreviousLine()
		{
			if (m_lineIndex == 0)
				return false;

			ITextSnapshotLine line = m_snapshot.GetLineFromLineNumber(--m_lineIndex);
			vhdlLexer lexer = new vhdlLexer(new AntlrInputStream(line.GetText()));
			m_lineTokens = lexer.GetAllTokens();

			m_tokenIndex = m_lineTokens.Count - 1;
			return true;
		}
		public IToken GetPreviousToken()
		{
			if (m_tokenIndex == -1)
			{
				while(m_tokenIndex == -1)
					if (!LexPreviousLine())
						return null;
			}

			IToken token = m_lineTokens[m_tokenIndex--];
			m_index = token.StartIndex + m_snapshot.GetLineFromLineNumber(m_lineIndex).Start.Position;
			return token;
		}

		public IToken PeekPreviousToken()
		{
			if (m_tokenIndex == -1)
			{
				while (m_tokenIndex == -1)
					if (!LexPreviousLine())
						return null;
			}

			IToken token = m_lineTokens[m_tokenIndex];
			return token;
		}
		public int GetIndex()
		{
			return m_index;
		}
	}
}
