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

namespace vhdl4vs
{
	public class VHDLLanguageUtils
	{

		static private HashSet<string> keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
		static public HashSet<string> Keywords => keywords;
	}
}
