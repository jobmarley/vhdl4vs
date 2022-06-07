/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace vhdl4vs
{

	// Usefull when need to parsing stuff in realtime, when the background parsing is not done, or is too long
	// (eg. signatureHelper or completion)
	internal class VHDLReverseExpressionParser
	{
		private VHDLReverseLexer m_lexer = null;
		private VHDLDocument m_document = null;
		public VHDLReverseExpressionParser(VHDLDocument document, VHDLReverseLexer lexer)
		{
			m_document = document;
			m_lexer = lexer;
		}

		// Very simple, just parse stuff like name1.name2.name3
		public VHDLDeclaration ParseName()
		{
			IToken token = m_lexer.GetPreviousToken();
			if (token == null)
				return null;

			if (vhdlLexer.DefaultVocabulary.GetSymbolicName(token.Type) != "BASIC_IDENTIFIER")
				return null;

			List<IToken> names = new List<IToken>();
			names.Add(token);

			IToken beforeToken = null;

			while (token != null)
			{
				token = m_lexer.GetPreviousToken();
				beforeToken = token;
				if (token == null || token.Text != ".")
					break;

				token = m_lexer.GetPreviousToken();
				if (vhdlLexer.DefaultVocabulary.GetSymbolicName(token.Type) != "BASIC_IDENTIFIER")
					return null;

				names.Insert(0, token);
			}

			VHDLDeclaration decl = null;
			if (beforeToken != null && string.Compare(beforeToken.Text, "use") == 0)
			{
				// search in library declarations, a bit weird
				// could add that in FindName as well... not sure
				decl = m_document.Project?.GetLibrary(names.First().Text);
			}
			else
			{
				decl = VHDLDeclarationUtilities.GetEnclosingDeclaration(m_document?.Parser?.AResult, new SnapshotPoint(m_lexer.Snapshot, m_lexer.GetIndex()));
				try
				{
					decl = VHDLDeclarationUtilities.FindName(decl, names.First().Text);
					if (decl == null)
						return null;
				}
				catch (Exception ex)
				{
					return null;
				}
			}

			foreach(IToken name in names.Skip(1))
			{
				decl = VHDLDeclarationUtilities.GetMemberDeclaration(decl, name.Text);
				if (decl == null)
					return null;
			}

			return decl;
		}
	}
}
