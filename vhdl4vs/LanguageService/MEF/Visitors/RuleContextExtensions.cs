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
