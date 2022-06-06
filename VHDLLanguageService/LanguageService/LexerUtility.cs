using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs
{
	class LexerUtility
	{
		static public Antlr4.Runtime.IToken GetTokenAtPosition(IList<Antlr4.Runtime.IToken> tokens, int pos)
		{
			foreach(Antlr4.Runtime.IToken token in tokens)
			{
				if (pos > token.StopIndex)
					break;
				if (pos >= token.StartIndex && pos <= token.StopIndex)
					return token;
			}

			return null;
		}


		static private HashSet<string> s_keywords = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase)
		{
			"abs",
			"access",
			"after",
			"alias",
			"all",
			"and",
			"architecture",
			"array",
			"assert",
			"attribute",
			"begin",
			"block",
			"body",
			"buffer",
			"bus",
			"case",
			"component",
			"configuration",
			"constant",
			"disconnect",
			"downto",
			"else",
			"elsif",
			"end",
			"entity",
			"exit",
			"file",
			"for",
			"function",
			"generate",
			"generic",
			"group",
			"guarded",
			"if",
			"impure",
			"in",
			"inertial",
			"inout",
			"is",
			"label",
			"library",
			"linkage",
			"literal",
			"loop",
			"map",
			"mod",
			"nand",
			"new",
			"next",
			"nor",
			"not",
			"null",
			"of",
			"on",
			"open",
			"or",
			"others",
			"out",
			"package",
			"port",
			"postponed",
			"procedure",
			"process",
			"pure",
			"range",
			"record",
			"register",
			"reject",
			"rem",
			"report",
			"return",
			"rol",
			"ror",
			"select",
			"severity",
			"signal",
			"shared",
			"sla",
			"sll",
			"sra",
			"srl",
			"subtype",
			"then",
			"to",
			"transport",
			"type",
			"unaffected",
			"units",
			"until",
			"use",
			"variable",
			"wait",
			"when",
			"while",
			"with",
			"xnor",
			"xor",
		};
		static public HashSet<string> Keywords { get { return s_keywords; } }
	}
}
