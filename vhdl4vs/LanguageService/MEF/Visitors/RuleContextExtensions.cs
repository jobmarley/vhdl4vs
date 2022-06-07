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
using Antlr4.Runtime.Tree;

using Span = Microsoft.VisualStudio.Text.Span;

namespace vhdl4vs
{
	internal static class RuleContextExtensions
	{
		/// <summary>
		/// Returns the ancestor object as the given type, or null.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="context"></param>
		/// <param name="level">Recursion level. 1 = Parent, 2 = Parent.Parent, etc.</param>
		/// <returns></returns>
		public static T GetAncestorAs<T>(this RuleContext context, int level)
			where T : RuleContext
		{
			for (int i = 0; i < level; ++i)
			{
				if (context.Parent != null)
					context = context.Parent;
				else
					return null;
			}

			return context as T;
		}

		
	}

	internal static class TokenExtensions
	{

		/// <summary>
		/// Length of the token.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public static int Length(this IToken token)
		{
			return token.StopIndex - token.StartIndex + 1;
		}
		public static Span GetSpan(this IToken token)
		{
			return new Span(token.StartIndex, token.Length());
		}
	}
	internal static class ParserRuleExtensions
	{
		public static bool Contains(this ParserRuleContext context, int point)
		{
			if (point >= context.Start.StartIndex && point <= context.Stop.StopIndex)
				return true;
			else
				return false;
		}

		/// <summary>
		/// Length of the rule context in characters.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public static int Length(this ParserRuleContext context)
		{
			return context.Stop.StopIndex - context.Start.StartIndex + 1;
		}

		public static Span GetSpan(this ParserRuleContext context)
		{
			return new Span(context.Start.StartIndex, context.Length());
		}
	}
}
