/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs
{
	class DeepAnalysisResult
	{
		public AnalysisResult AnalysisResult { get; set; } = null;
		public vhdlParser.Design_fileContext Tree { get; set; } = null;
		public List<VHDLError> Errors { get; set; } = new List<VHDLError>();
		public ITextSnapshot Snapshot { get; set; } = null;
		public UInt64 Version { get; set; } = 0;
		public SortedList<int, VHDLNameReference> SortedReferences { get; set; } = new SortedList<int, VHDLNameReference>();
	}
	class DeepAnalysisResultEventArgs
		: EventArgs
	{
		public DeepAnalysisResultEventArgs(IVHDLParser parser, DeepAnalysisResult result)
		{
			Parser = parser;
			Result = result;
		}
		public IVHDLParser Parser { get; private set; }
		public DeepAnalysisResult Result { get; private set; }
	}
	// Used for stuff that need to be resolved at a later stage
	interface IVHDLToResolve
	{
		void Resolve(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener);
	}

	class VHDLFakeResolver
		: IVHDLToResolve
	{
		public VHDLFakeResolver(IVHDLToResolve overriden, Action<IVHDLToResolve, DeepAnalysisResult, Action<VHDLError>> resolver)
		{
			Overriden = overriden;
			Resolver = resolver;
		}
		public VHDLFakeResolver(VHDLNameExpression e, VHDLDeclaration d)
		{
			Overriden = e;
			Resolver = (r, dar, errList) =>
			{
				dar.SortedReferences.Add(e.Span.Start, new VHDLNameReference(e.Name, e.Span, d));
			};
		}
		public IVHDLToResolve Overriden { get; set; } = null;
		public Action<IVHDLToResolve, DeepAnalysisResult, Action<VHDLError>> Resolver { get; set; } = null;

		public void Resolve(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
			Resolver(Overriden, deepAnalysisResult, errorListener);
		}
	}
	class VHDLNameReference
	{
		public VHDLNameReference(string name, Span span, VHDLDeclaration declaration)
		{
			Name = name;
			Span = span;
			Declaration = declaration;
		}
		public string Name { get; set; } = null;
		public Span Span { get; set; }
		public VHDLDeclaration Declaration { get; set; } = null;
	}
	class AnalysisResult
	{
		public VHDLDocument Document { get { return ParseResult.Document; } }
		public ParseResult ParseResult { get; set; } = null;
		public vhdlParser.Design_fileContext Tree { get; set; } = null;
		public ITextSnapshot Snapshot { get; set; } = null;
		public UInt64 Version { get; set; } = 0;
		/// <summary>
		/// Dictionnary of all absolute names in the file (eg. mypackage.myarchitecture.mysignal)
		/// We add @attr to be able to differentiate things with the same name, like overloaded functions
		/// </summary>
		public IDictionary<string, VHDLDeclaration> Declarations { get; set; } = null;
		public IDictionary<Antlr4.Runtime.RuleContext, VHDLDeclaration> DeclarationsByContext { get; set; } = null;
		public SortedList<int, VHDLDeclaration> SortedScopedDeclarations { get; set; } = null;
		public List<IVHDLToResolve> ToResolve { get; set; } = new List<IVHDLToResolve>();
		public Dictionary<Antlr4.Runtime.ParserRuleContext, VHDLStatement> StatementsByContext { get; set; } = new Dictionary<Antlr4.Runtime.ParserRuleContext, VHDLStatement>();
		public List<VHDLError> Errors { get; set; } = new List<VHDLError>();
		// This is usefull for deep analysis, because BOOLEAN need to be declared
		public VHDLType BooleanType { get; set; } = null;
		// Same for wait statements and such
		public VHDLType TimeType { get; set; } = null;
		// For report statements
		public VHDLType SeverityLevelType { get; set; } = null;


		public void AddStatement(Antlr4.Runtime.ParserRuleContext context, VHDLStatement statement)
		{
			//if (StatementsByContext.ContainsKey(context))
			//	System.Diagnostics.Debugger.Break();
			StatementsByContext.Add(context, statement);
		}
		public void AddToResolve(IVHDLToResolve r)
		{
			//if (r is VHDLExpression e && ToResolve.Any(x => (x as VHDLExpression).Span.Start == e.Span.Start))
			//	System.Diagnostics.Debugger.Break();
			ToResolve.Add(r);
		}

	}
	class AnalysisResultEventArgs
		: EventArgs
	{
		public AnalysisResultEventArgs(IVHDLParser parser, AnalysisResult result)
		{
			Parser = parser;
			Result = result;
		}
		public IVHDLParser Parser { get; private set; }
		public AnalysisResult Result { get; private set; }
	}

	class VHDLError
	{
		public VHDLError(int errorCode, string errorType, string message, Span span)
		{
			ErrorType = errorType;
			ErrorCode = errorCode;
			Message = message;
			Span = span;
		}
		// This is ErrorTag => errorType
		public string ErrorType { get; set; }
		public int ErrorCode { get; set; }
		public string Message { get; set; }
		public Span Span { get; set; }
	}

	class ParseResult
	{
		public VHDLDocument Document { get; set; } = null;
		public vhdlParser.Design_fileContext Tree { get; set; }
		public IEnumerable<VHDLError> Errors { get; set; }
		public ITextSnapshot Snapshot { get; set; }
		public Antlr4.Runtime.CommonTokenStream TokenStream { get; set; }
		public IEnumerable<Span> Comments { get; set; }
		public UInt64 Version { get; set; }
	}
	class ParseResultEventArgs
		: EventArgs
	{
		public ParseResultEventArgs(IVHDLParser parser, ParseResult result)
		{
			Parser = parser;
			Result = result;
		}
		public IVHDLParser Parser { get; private set; }
		public ParseResult Result { get; }
	}
	interface IVHDLParser
		: IDisposable
	{
		event EventHandler<ParseResultEventArgs> ParseComplete;
		event EventHandler<AnalysisResultEventArgs> AnalysisComplete;
		event EventHandler<DeepAnalysisResultEventArgs> DeepAnalysisComplete;

		ParseResult PResult { get; }
		AnalysisResult AResult { get; }
		DeepAnalysisResult DAResult { get; }

		VHDLDocument Document { get; }
	}
}
