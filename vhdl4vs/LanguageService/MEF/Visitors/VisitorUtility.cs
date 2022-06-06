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
