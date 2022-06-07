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
using Antlr4.Runtime;

namespace vhdl4vs
{
	class VisitorUtility
	{
		//	Return the comment that is before the given token
		public static string GetCommentForToken(IToken token, CommonTokenStream tokenStream)
		{
			StringBuilder content = new StringBuilder();

			IList<Antlr4.Runtime.IToken> commentTokens = tokenStream.GetHiddenTokensToLeft(token.TokenIndex);
			if (commentTokens == null)
				return "";

			foreach (Antlr4.Runtime.IToken t in commentTokens)
			{
				if (content.Length == 0)
					content.Append(t.Text.Substring(2).Trim());
				else
					content.Append(Environment.NewLine + t.Text.Substring(2).Trim());
			}

			return content.ToString();
		}
	}
}
