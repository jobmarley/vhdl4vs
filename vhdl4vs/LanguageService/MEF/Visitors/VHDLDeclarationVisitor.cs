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
using Microsoft.VisualStudio.Text;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime;
using Microsoft.VisualStudio.Text.Adornments;

namespace vhdl4vs
{
	class VHDLDeclarationVisitor
	   : vhdlBaseVisitor<bool>
	{
		// Kind of a duplicate but whatever
		private Stack<VHDLModifiableDeclaration> m_declarationStack = new Stack<VHDLModifiableDeclaration>();
		public Dictionary<RuleContext, VHDLDeclaration> DeclarationsByContext { get; set; }
		public SortedList<int, VHDLDeclaration> SortedScopedDeclarations { get; set; }

		private AnalysisResult m_analysisResult = null;
		private Action<VHDLError> m_errorListener = null;
		private HashSet<string> m_uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		public VHDLDeclarationVisitor(AnalysisResult analysisResult, Action<VHDLError> errorListener = null)
		{
			m_analysisResult = analysisResult;

			DeclarationsByContext = new Dictionary<RuleContext, VHDLDeclaration>();
			SortedScopedDeclarations = new SortedList<int, VHDLDeclaration>();

			VHDLFileDeclaration fileDecl = new VHDLFileDeclaration(analysisResult);
			SortedScopedDeclarations.Add(-1, fileDecl);
			m_declarationStack.Push(fileDecl);

			m_errorListener = errorListener;
		}

		private void PushScope(VHDLModifiableDeclaration decl)
		{
			DeclarationsByContext.Add(decl.Context, decl);
			m_declarationStack.Peek().Children.Add(decl);
			m_declarationStack.Push(decl);
			SortedScopedDeclarations[decl.Span.Start] = decl;
		}
		private void PopScope()
		{
			int end = m_declarationStack.Peek().Span.End;
			m_declarationStack.Pop();
			SortedScopedDeclarations[end] = m_declarationStack.Count > 0 ? m_declarationStack.Peek() : null;
		}

		private string GenerateUniqueName(string name)
		{
			string treePathBase = m_declarationStack.Peek().TreePath + "." + name;
			int i = 1;
			while(true)
			{
				string treePath = treePathBase + "@" + i.ToString();
				if (!m_uniqueNames.Contains(treePath))
				{
					m_uniqueNames.Add(treePath);
					return name + "@" + i.ToString();
				}
				++i;
			}
		}

		private VHDLSignalMode VisitSignalMode(vhdlParser.Signal_modeContext context)
		{
			if (context?.IN() != null)
				return VHDLSignalMode.In;
			else if (context?.OUT() != null)
				return VHDLSignalMode.Out;
			else if (context?.INOUT() != null)
				return VHDLSignalMode.Inout;
			else if (context?.BUFFER() != null)
				return VHDLSignalMode.Buffer;
			else if (context?.LINKAGE() != null)
				return VHDLSignalMode.Linkage;
			else
				return VHDLSignalMode.In;
		}
		private List<VHDLUseClause> m_usedLibraries = new List<VHDLUseClause>();
		public override bool VisitUse_clause([NotNull] vhdlParser.Use_clauseContext context)
		{
			if (context.selected_name() != null)
			{
				foreach (var name in context.selected_name())
				{
					// dont add resolve cause names library are a special case, handled in VHDLUseClause
					ExpressionVisitors.VHDLNameExpressionVisitor visitor = new ExpressionVisitors.VHDLNameExpressionVisitor(m_analysisResult, m_errorListener,
						(x, y, z) => { });
					VHDLUseClause clause = new VHDLUseClause(m_analysisResult, null);
					clause.Name = visitor.Visit(name);
					m_usedLibraries.Add(clause);
				}
			}
			return true;
		}

		public override bool VisitType_declaration([NotNull] vhdlParser.Type_declarationContext context)
		{
			string typeName = context.identifier().GetText();

			VHDLTypeDeclaration decl = new VHDLTypeDeclaration(m_analysisResult, context, context.identifier(), typeName, m_declarationStack.FirstOrDefault());
			
			PushScope(decl);

			try // This should not cause an error
			{
				TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
				decl.Type = visitor.Visit(context.type_definition());
				decl.Type.Declaration = decl;
			}
			catch (VHDLCodeException e)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
			}
			catch (Exception e)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.type_definition()?.GetSpan() ?? context.GetSpan()));
			}

			if (decl.Type is VHDLRecordType recordType)
			{
				recordType.Declaration = decl;
				try
				{
					Visit(context.type_definition().composite_type_definition().record_type_definition());
				}
				catch (Exception e)
				{
				}
			}
			// for enumeration we need to gather all declaration so they can be found as constants
			if (decl.Type is VHDLEnumerationType et)
			{
				foreach(var v in et.Values.OfType<VHDLNameEnumerationValue>())
				{
					DeclarationsByContext.Add(v.Declaration.Context, v.Declaration);
					m_declarationStack.Peek().Children.Add(v.Declaration);
				}
			}
			PopScope();
			return true;
		}

        public override bool VisitSubtype_declaration([NotNull] vhdlParser.Subtype_declarationContext context)
        {
			string typeName = context.identifier().GetText();

			VHDLSubTypeDeclaration decl = new VHDLSubTypeDeclaration(m_analysisResult, context, context.identifier(), typeName, m_declarationStack.FirstOrDefault());
			DeclarationsByContext.Add(context, decl);
			m_declarationStack.Peek().Children.Add(decl);

			try // This should not cause an error
			{ 
				TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
				decl.Type = visitor.Visit(context.subtype_indication());
				decl.Type.Declaration = decl;
			}
			catch (VHDLCodeException e)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
			}
			catch (Exception e)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.subtype_indication()?.GetSpan() ?? context.GetSpan()));
			}

			return true;
        }
        /*
		 * interface_constant_declaration
  |  interface_signal_declaration
  | interface_variable_declaration
  | interface_file_declaration
  | interface_terminal_declaration
  | interface_quantity_declaration
  */
        public override bool VisitInterface_file_declaration([NotNull] vhdlParser.Interface_file_declarationContext context)
		{
			int start = m_declarationStack.Peek().Span.Start;
			int stop = m_declarationStack.Peek().Span.End;

			foreach (var identifier_context in context.identifier_list().identifier())
			{
				string name = identifier_context.GetText();

			}
			return true;
		}
		public override bool VisitInterface_terminal_declaration([NotNull] vhdlParser.Interface_terminal_declarationContext context)
		{
			int start = m_declarationStack.Peek().Span.Start;
			int stop = m_declarationStack.Peek().Span.End;

			foreach (var identifier_context in context.identifier_list().identifier())
			{
				string name = identifier_context.GetText();

			}
			return true;
		}
		public override bool VisitInterface_quantity_declaration([NotNull] vhdlParser.Interface_quantity_declarationContext context)
		{
			int start = m_declarationStack.Peek().Span.Start;
			int stop = m_declarationStack.Peek().Span.End;

			foreach (var identifier_context in context.identifier_list().identifier())
			{
				string name = identifier_context.GetText();

			}
			return true;
		}
		public override bool VisitInterface_variable_declaration([NotNull] vhdlParser.Interface_variable_declarationContext context)
		{
			int start = m_declarationStack.Peek().Span.Start;
			int stop = m_declarationStack.Peek().Span.End;

			VHDLExpression expr = null;
			if (context.expression() != null)
			{
				try // This should not cause an error
				{
					ExpressionVisitors.VHDLExpressionVisitor exprVisitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
					expr = exprVisitor.Visit(context.expression());
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.expression().GetSpan()));
				}
			}
			if (context.identifier_list()?.identifier() != null)
			{
				VHDLType type = null;
				try // This should not cause an error
				{
					TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
					type = visitor.Visit(context.subtype_indication());
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.subtype_indication()?.GetSpan() ?? context.GetSpan()));
				}

				foreach (var identifier_context in context.identifier_list().identifier())
				{
					string name = identifier_context.GetText();

					VHDLVariableDeclaration decl = new VHDLVariableDeclaration(m_analysisResult, context, identifier_context, name, m_declarationStack.FirstOrDefault());
					DeclarationsByContext.Add(identifier_context, decl);
					(m_declarationStack.First() as VHDLSubprogramDeclaration)?.Parameters.Add(decl);
					m_declarationStack.Peek().Children.Add(decl);
					decl.Type = type;

					decl.InitializationExpression = expr;
					decl.Mode = VisitSignalMode(context.signal_mode());
				}
			}
			return true;
		}
		public override bool VisitInterface_constant_declaration([NotNull] vhdlParser.Interface_constant_declarationContext context)
		{
			int start = m_declarationStack.Peek().Span.Start;
			int stop = m_declarationStack.Peek().Span.End;

			VHDLExpression expr = null;
			if (context.expression() != null)
			{
				try // This should not cause an error
				{ 
					ExpressionVisitors.VHDLExpressionVisitor exprVisitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
					expr = exprVisitor.Visit(context.expression());
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.expression().GetSpan()));
				}
			}
			if (context.identifier_list()?.identifier() != null)
			{
				VHDLType type = null;
				try // This should not cause an error
				{
					TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
					type = visitor.Visit(context.subtype_indication());

				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.GetSpan()));
				}

				foreach (var identifier_context in context.identifier_list().identifier())
				{
					string name = identifier_context.GetText();

					VHDLConstantDeclaration decl = new VHDLConstantDeclaration(m_analysisResult, context, identifier_context, name, m_declarationStack.FirstOrDefault());
					DeclarationsByContext.Add(identifier_context, decl);
					(m_declarationStack.First() as VHDLSubprogramDeclaration)?.Parameters.Add(decl);
					m_declarationStack.Peek().Children.Add(decl);

					decl.Type = type;

					decl.InitializationExpression = expr;
				}
			}
			return true;
		}
		public override bool VisitInterface_signal_declaration([NotNull] vhdlParser.Interface_signal_declarationContext context)
		{
			int start = m_declarationStack.Peek().Span.Start;
			int stop = m_declarationStack.Peek().Span.End;

			VHDLExpression expr = null;
			if (context.expression() != null)
			{
				try // This should not cause an error
				{ 
					ExpressionVisitors.VHDLExpressionVisitor exprVisitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
					expr = exprVisitor.Visit(context.expression());
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.expression().GetSpan()));
				}
			}
			if (context.identifier_list()?.identifier() != null)
			{
				VHDLType type = null;
				try // This should not cause an error
				{
					TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
					type = visitor.Visit(context.subtype_indication());
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.GetSpan()));
				}

				foreach (var identifier_context in context.identifier_list().identifier())
				{
					string name = identifier_context.GetText();

					VHDLSignalDeclaration decl = new VHDLSignalDeclaration(m_analysisResult, context, identifier_context, name, m_declarationStack.FirstOrDefault());
					DeclarationsByContext.Add(identifier_context, decl);
					(m_declarationStack.First() as VHDLSubprogramDeclaration)?.Parameters.Add(decl);
					m_declarationStack.Peek().Children.Add(decl);
					decl.Type = type;
					
					decl.InitializationExpression = expr;
					decl.Mode = VisitSignalMode(context.signal_mode());
				}
			}
			return true;
		}
		public override bool VisitConstant_declaration([NotNull] vhdlParser.Constant_declarationContext context)
		{
			int start = m_declarationStack.Peek().Span.Start;
			int stop = m_declarationStack.Peek().Span.End;

			VHDLExpression expr = null;
			if (context.expression() != null)
			{
				try // This should not cause an error
				{ 
					ExpressionVisitors.VHDLExpressionVisitor exprVisitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
					expr = exprVisitor.Visit(context.expression());
				}
				catch (Exception e)
				{
				}
			}
			if (context.identifier_list()?.identifier() != null)
			{
				VHDLType type = null;
				try // This should not cause an error
				{
					TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
					type = visitor.Visit(context.subtype_indication());
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.subtype_indication()?.GetSpan() ?? context.GetSpan()));
				}

				foreach (var identifier_context in context.identifier_list().identifier())
				{
					string name = identifier_context.GetText();

					VHDLConstantDeclaration decl = new VHDLConstantDeclaration(m_analysisResult, context, identifier_context, name, m_declarationStack.FirstOrDefault());
					DeclarationsByContext.Add(identifier_context, decl);
					m_declarationStack.Peek().Children.Add(decl);
					decl.Type = type;

					decl.InitializationExpression = expr;
				}
			}
			return true;
		}

		public override bool VisitSignal_declaration([NotNull] vhdlParser.Signal_declarationContext context)
		{
			int start = m_declarationStack.Peek().Span.Start;
			int stop = m_declarationStack.Peek().Span.End;

			VHDLExpression expr = null;
			if (context.expression() != null)
			{
				try // This should not cause an error
				{ 
					ExpressionVisitors.VHDLExpressionVisitor exprVisitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
					expr = exprVisitor.Visit(context.expression());
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.GetSpan()));
				}
			}
			if (context.identifier_list()?.identifier() != null)
			{
				VHDLType type = null;
				try // This should not cause an error
				{
					TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
					type = visitor.Visit(context.subtype_indication());
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.subtype_indication()?.GetSpan() ?? context.GetSpan()));
				}

				foreach (var identifier_context in context.identifier_list().identifier())
				{
					string name = identifier_context.GetText();

					VHDLSignalDeclaration decl = new VHDLSignalDeclaration(m_analysisResult, context, identifier_context, name, m_declarationStack.FirstOrDefault());
					DeclarationsByContext.Add(identifier_context, decl);
					m_declarationStack.Peek().Children.Add(decl);
					decl.Type = type;

					decl.InitializationExpression = expr;
				}
			}

			return true;
		}
		public override bool VisitVariable_declaration([NotNull] vhdlParser.Variable_declarationContext context)
		{
			int start = m_declarationStack.Peek().Span.Start;
			int stop = m_declarationStack.Peek().Span.End;

			VHDLExpression expr = null;
			if (context.expression() != null)
			{
				try // This should not cause an error
				{ 
					ExpressionVisitors.VHDLExpressionVisitor exprVisitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
					expr = exprVisitor.Visit(context.expression());
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.expression().GetSpan()));
				}
			}
			if (context.identifier_list()?.identifier() != null)
			{
				VHDLType type = null;
				try // This should not cause an error
				{
					TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
					type = visitor.Visit(context.subtype_indication());
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.subtype_indication()?.GetSpan() ?? context.GetSpan()));
				}

				foreach (var identifier_context in context.identifier_list().identifier())
				{
					string name = identifier_context.GetText();

					VHDLVariableDeclaration decl = new VHDLVariableDeclaration(m_analysisResult, context, identifier_context, name, m_declarationStack.FirstOrDefault());
					DeclarationsByContext.Add(identifier_context, decl);
					m_declarationStack.Peek().Children.Add(decl);
					decl.Type = type;
					
					decl.InitializationExpression = expr;
				}
			}

			return true;
		}
		public override bool VisitInterface_port_declaration([NotNull] vhdlParser.Interface_port_declarationContext context)
		{
			VHDLExpression expr = null;
			if (context.expression() != null)
			{
				try // This should not cause an error
				{ 
					ExpressionVisitors.VHDLExpressionVisitor exprVisitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
					expr = exprVisitor.Visit(context.expression());
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.expression().GetSpan()));
				}
			}
			if (context.identifier_list()?.identifier() != null)
			{
				VHDLType type = null;
				try // This should not cause an error
				{
					TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
					type = visitor.Visit(context.subtype_indication());
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", context.subtype_indication()?.GetSpan() ?? context.GetSpan()));
				}

				foreach (var identifier in context.identifier_list().identifier())
				{
					try
					{
						string port_name = identifier.GetText();

						VHDLPortDeclaration decl = new VHDLPortDeclaration(m_analysisResult, context, identifier, port_name, m_declarationStack.FirstOrDefault());
						DeclarationsByContext.Add(identifier, decl);
						m_declarationStack.Peek().Children.Add(decl);
						decl.Type = type;
						decl.InitializationExpression = expr;

						decl.Mode = VisitSignalMode(context.signal_mode());

					}
					catch (Exception e)
					{

					}
				}
			}

			return true;
		}

		public override bool VisitSubprogram_declaration([NotNull] vhdlParser.Subprogram_declarationContext context)
		{
			if (context.subprogram_specification()?.procedure_specification() != null)
			{
				if (context.subprogram_specification()?.procedure_specification()?.designator() == null)
					throw new Exception("VHDLTypeListVisitor procedure_specification.designator is null");

				string name = context.subprogram_specification()?.procedure_specification()?.designator()?.GetText();
				name = GenerateUniqueName(name);
				VHDLProcedureDeclaration decl = new VHDLProcedureDeclaration(
					m_analysisResult,
					context.subprogram_specification()?.procedure_specification(),
					context.subprogram_specification()?.procedure_specification()?.designator(),
					name,
					m_declarationStack.Peek());

				PushScope(decl);
			}
			else if (context.subprogram_specification()?.function_specification() != null)
			{
				if (context.subprogram_specification()?.function_specification()?.designator() == null)
					throw new Exception("VHDLTypeListVisitor function_specification.designator is null");

				string name = context.subprogram_specification()?.function_specification()?.designator()?.GetText();
				name = GenerateUniqueName(name);
				VHDLFunctionDeclaration decl = new VHDLFunctionDeclaration(
					m_analysisResult,
					context.subprogram_specification()?.function_specification(),
					context.subprogram_specification()?.function_specification()?.designator(),
					name,
					m_declarationStack.FirstOrDefault());

				try // This should not cause an error
				{ 
					TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
					decl.ReturnType = visitor.Visit(context.subprogram_specification()?.function_specification()?.subtype_indication());
					if (decl.ReturnType is VHDLIndexConstrainedType)
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Function return type cannot be index constrained", context.subprogram_specification().function_specification().subtype_indication().GetSpan()));
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error",
						context.subprogram_specification()?.function_specification()?.subtype_indication()?.GetSpan() ?? context.GetSpan()));
				}

				PushScope(decl);
			}
			else
				throw new Exception();

			bool b = base.VisitSubprogram_declaration(context);

			PopScope();
			return b;
		}
		public override bool VisitSubprogram_body([NotNull] vhdlParser.Subprogram_bodyContext context)
		{
			int start = context.Start.StartIndex;
			int stop = context.Stop.StopIndex;

			if (context.subprogram_specification()?.procedure_specification() != null)
			{
				if (context.subprogram_specification()?.procedure_specification()?.designator() == null)
					throw new Exception("VHDLTypeListVisitor procedure_specification.designator is null");

				string name = context.subprogram_specification()?.procedure_specification()?.designator()?.GetText();
				name = GenerateUniqueName(name);
				VHDLProcedureBodyDeclaration decl = new VHDLProcedureBodyDeclaration(
					m_analysisResult,
					context,
					context.subprogram_specification()?.procedure_specification()?.designator(),
					name,
					m_declarationStack.FirstOrDefault());
				PushScope(decl);

				VisitSubprogram_specification(context.subprogram_specification());
				VisitSubprogram_declarative_part(context.subprogram_declarative_part());

				if (context.subprogram_statement_part()?.sequential_statement() != null)
				{
					foreach (var statementContext in context.subprogram_statement_part().sequential_statement())
					{
						try
						{
							VHDLStatementVisitor visitor = new VHDLStatementVisitor(m_analysisResult, decl, m_errorListener);
							VHDLStatement statement = visitor.Visit(statementContext);
							if (statement != null)
								decl.Statements.Add(statement);
						}
						catch (VHDLCodeException e)
						{
							m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
						}
						catch (Exception e)
						{
							m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", statementContext.GetSpan()));
						}

						// we run through everything to get stuff like for loops
						Visit(statementContext);
					}
				}
			}
			else if (context.subprogram_specification()?.function_specification() != null)
			{
				if (context.subprogram_specification()?.function_specification()?.designator() == null)
					throw new Exception("VHDLTypeListVisitor function_specification.designator is null");

				string name = context.subprogram_specification()?.function_specification()?.designator()?.GetText();
				name = GenerateUniqueName(name);
				VHDLFunctionBodyDeclaration decl = new VHDLFunctionBodyDeclaration(
					m_analysisResult,
					context,
					context.subprogram_specification()?.function_specification()?.designator(),
					name,
					m_declarationStack.Peek());

				try // This should not cause an error
				{
					TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
					decl.ReturnType = visitor.Visit(context.subprogram_specification()?.function_specification()?.subtype_indication());
					if (decl.ReturnType is VHDLIndexConstrainedType)
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Function return type cannot be index constrained", context.subprogram_specification().function_specification().subtype_indication().GetSpan()));
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error",
						context.subprogram_specification()?.function_specification()?.subtype_indication()?.GetSpan() ?? context.GetSpan()));
				}
				PushScope(decl);

				VisitSubprogram_specification(context.subprogram_specification());
				VisitSubprogram_declarative_part(context.subprogram_declarative_part());

				if (context.subprogram_statement_part().sequential_statement() != null)
				{
					foreach (var statementContext in context.subprogram_statement_part()?.sequential_statement())
					{
						try
						{
							VHDLStatementVisitor visitor = new VHDLStatementVisitor(m_analysisResult, decl, m_errorListener);
							VHDLStatement statement = visitor.Visit(statementContext);
							if (statement != null)
								decl.Statements.Add(statement);
						}
						catch (VHDLCodeException e)
						{
							m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
						}
						catch (Exception e)
						{
							m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", statementContext.GetSpan()));
						}

						// we run through everything to get stuff like for loops
						Visit(statementContext);
					}
				}
			}
			else
				throw new Exception();

			PopScope();
			return true;
		}
		public override bool VisitPackage_declaration([NotNull] vhdlParser.Package_declarationContext context)
		{
			int start = context.Start.StartIndex;
			int stop = context.Stop.StopIndex;

			if (context.identifier()?[0] == null)
				throw new Exception("VHDLTypeListVisitor package identifier is null");
			string name = context.identifier()?[0]?.GetText() + "@declaration";
			VHDLPackageDeclaration decl = new VHDLPackageDeclaration(m_analysisResult, context, context.identifier()?[0], name, m_usedLibraries, m_declarationStack.FirstOrDefault());
			
			m_usedLibraries = new List<VHDLUseClause>();
			PushScope(decl);

			bool b = base.VisitPackage_declaration(context);

			PopScope();
			return b;
		}
		public override bool VisitPackage_body([NotNull] vhdlParser.Package_bodyContext context)
		{
			int start = context.Start.StartIndex;
			int stop = context.Stop.StopIndex;

			if (context.identifier()?[0] == null)
				throw new Exception("VHDLTypeListVisitor package identifier is null");
			string name = context.identifier()?[0]?.GetText() + "@body";
			VHDLPackageBodyDeclaration decl = new VHDLPackageBodyDeclaration(m_analysisResult, context, context.identifier()?[0], name, m_usedLibraries, m_declarationStack.FirstOrDefault());
			
			m_usedLibraries = new List<VHDLUseClause>();
			PushScope(decl);

			bool b = base.VisitPackage_body(context);

			PopScope();
			return b;
		}
		public override bool VisitArchitecture_body([NotNull] vhdlParser.Architecture_bodyContext context)
		{
			int start = context.Start.StartIndex;
			int stop = context.Stop.StopIndex;

			string entity_name = context.identifier()[1].GetText();
			string architecture_name = context.identifier()[0].GetText();

			VHDLArchitectureDeclaration decl = new VHDLArchitectureDeclaration(m_analysisResult, context, context.identifier()[0], architecture_name, m_usedLibraries, m_declarationStack.FirstOrDefault());
			
			m_usedLibraries = new List<VHDLUseClause>();
			PushScope(decl);

			m_analysisResult.AddToResolve(decl);

			try
			{
				VisitArchitecture_declarative_part(context.architecture_declarative_part());
			}
			catch (Exception ex)
			{
			}

			if (context.architecture_statement_part()?.architecture_statement() != null)
			{
				foreach (var statementContext in context.architecture_statement_part().architecture_statement())
				{
					try
					{
						if (statementContext.process_statement() == null)
						{
							VHDLStatementVisitor visitor = new VHDLStatementVisitor(m_analysisResult, decl, m_errorListener);
							VHDLStatement statement = visitor.Visit(statementContext);
							if (statement != null)
								decl.Statements.Add(statement);
						}
					}
					catch (VHDLCodeException e)
					{
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
					}
					catch (Exception e)
					{
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", statementContext.GetSpan()));
					}

					// we run through everything to get stuff like for loops
					Visit(statementContext);
				}
			}

			PopScope();
			return true;
		}
		public override bool VisitProcess_statement([NotNull] vhdlParser.Process_statementContext context)
		{
			string name = context.label_colon()?.identifier()?.GetText() ?? "@process_" + context.Start.StartIndex.ToString();
			VHDLProcessDeclaration decl = new VHDLProcessDeclaration(m_analysisResult,
				context, context.label_colon()?.identifier(),
				name,
				m_declarationStack.FirstOrDefault());
			PushScope(decl);

			if (context.ALL() != null)
				decl.SensitivityList.Add(new VHDLAllExpression(m_analysisResult, context.ALL().Symbol.GetSpan()));

			foreach (var nameContext in context.sensitivity_list()?.name() ?? Array.Empty<vhdlParser.NameContext>())
			{
				try
				{ 
					ExpressionVisitors.VHDLExpressionVisitor visitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
					decl.SensitivityList.Add(visitor.Visit(nameContext));
				}
				catch (VHDLCodeException e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", nameContext.GetSpan()));
				}
			}

			try
			{
				VisitProcess_declarative_part(context.process_declarative_part());
			}
			catch (Exception ex)
			{
			}

			if (context.process_statement_part()?.sequential_statement() != null)
			{
				foreach (var statementContext in context.process_statement_part()?.sequential_statement())
				{
					try
					{
						VHDLStatementVisitor visitor = new VHDLStatementVisitor(m_analysisResult, decl, m_errorListener);
						VHDLStatement statement = visitor.Visit(statementContext);
						if (statement != null)
							decl.Statements.Add(statement);
					}
					catch (VHDLCodeException e)
					{
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
					}
					catch (Exception e)
					{
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", statementContext.GetSpan()));
					}

					// we run through everything to get stuff like for loops
					Visit(statementContext);
				}
			}

			PopScope();
			return true;
		}
		public override bool VisitComponent_instantiation_statement([NotNull] vhdlParser.Component_instantiation_statementContext context)
		{
			return false;
		}
		public override bool VisitComposite_type_definition([NotNull] vhdlParser.Composite_type_definitionContext context)
		{
			return false;
		}

		public override bool VisitComponent_declaration([NotNull] vhdlParser.Component_declarationContext context)
		{
			VHDLComponentDeclaration decl = new VHDLComponentDeclaration(m_analysisResult, context, m_declarationStack.FirstOrDefault(), context.identifier()[0].GetText());

			PushScope(decl);

			base.VisitComponent_declaration(context);

			PopScope();
			return true;
		}
		public override bool VisitComponent_configuration([NotNull] vhdlParser.Component_configurationContext context)
		{
			return false;
		}
		public override bool VisitEntity_declaration([NotNull] vhdlParser.Entity_declarationContext context)
		{
			int start = context.Start.StartIndex;
			int stop = context.Stop.StopIndex;

			string name = context.identifier()[0].GetText();

			VHDLEntityDeclaration decl = new VHDLEntityDeclaration(m_analysisResult, context, context.identifier()[0], name, m_usedLibraries, m_declarationStack.FirstOrDefault());
			
			m_usedLibraries = new List<VHDLUseClause>();
			PushScope(decl);

			try
			{
				VisitEntity_header(context.entity_header());
			}
			catch (Exception ex)
			{
			}
			try
			{
				VisitEntity_declarative_part(context.entity_declarative_part());
			}
			catch (Exception ex)
			{
			}

			if (context.entity_statement_part()?.entity_statement() != null)
			{
				foreach (var statementContext in context.entity_statement_part().entity_statement())
				{
					try
					{
						if (statementContext.process_statement() == null)
						{
							VHDLStatementVisitor visitor = new VHDLStatementVisitor(m_analysisResult, decl, m_errorListener);
							VHDLStatement statement = visitor.Visit(statementContext);
							if (statement != null)
								decl.Statements.Add(statement);
						}
					}
					catch (VHDLCodeException e)
					{
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", e.Span));
					}
					catch (Exception e)
					{
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", statementContext.GetSpan()));
					}

					// we run through everything to get stuff like for loops
					Visit(statementContext);
				}
			}

			PopScope();
			return true;
		}

		public override bool VisitAlias_declaration([NotNull] vhdlParser.Alias_declarationContext context)
		{
			VHDLAliasDeclaration decl = new VHDLAliasDeclaration(m_analysisResult,
				context,
				context.alias_designator(),
				context.alias_designator().GetText(),
				m_declarationStack.FirstOrDefault());
			if (context.alias_indication()?.subtype_indication() != null)
			{
				TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
				decl.Type = visitor.Visit(context.alias_indication().subtype_indication());
			}
			else if (context.alias_indication()?.subnature_indication() != null)
			{
				TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
				decl.Type = visitor.Visit(context.alias_indication().subnature_indication());
			}

			ExpressionVisitors.VHDLNameExpressionVisitor expressionVisitor = new ExpressionVisitors.VHDLNameExpressionVisitor(m_analysisResult, m_errorListener);
			decl.InitializationExpression = expressionVisitor.Visit(context.name());

			DeclarationsByContext.Add(context, decl);
			m_declarationStack.Peek().Children.Add(decl);
			return true;
		}

		public override bool VisitAttribute_declaration([NotNull] vhdlParser.Attribute_declarationContext context)
		{
			VHDLAttributeDeclaration decl = new VHDLAttributeDeclaration(m_analysisResult,
				context,
				context.label_colon().identifier(),
				context.label_colon().identifier().GetText(),
				m_declarationStack.FirstOrDefault());
			DeclarationsByContext.Add(context, decl);
			m_declarationStack.Peek().Children.Add(decl);
			return true;
		}

		public override bool VisitLoop_statement([NotNull] vhdlParser.Loop_statementContext context)
		{
			if (context.iteration_scheme().FOR() != null)
			{
				VHDLLoopDeclaration decl = new VHDLLoopDeclaration(m_analysisResult, context, m_declarationStack.FirstOrDefault());

				ParserRuleContext variableContext = context.iteration_scheme().parameter_specification().identifier();
				VHDLConstantDeclaration variableDecl = new VHDLConstantDeclaration(m_analysisResult,
					variableContext,
					variableContext,
					variableContext.GetText(),
					decl);

				var discreteRangeContext = context.iteration_scheme().parameter_specification().discrete_range();
				TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);

				// Its inacurate. I think you can have "for i in std_ulogic" maybe?
				// anyway the type of that variable depend on the expression, and as such, cannot be deduced at this stage
				// It needs to be evaluated during the analysis
				variableDecl.Type = VHDLBuiltinTypeInteger.Instance;// visitor.Visit(discreteRangeContext);

				DeclarationsByContext.Add(variableContext, variableDecl);
				m_declarationStack.Peek().Children.Add(variableDecl);

				
				PushScope(decl);

				base.VisitLoop_statement(context);

				PopScope();
				return true;
			}

			return base.VisitLoop_statement(context);
		}

		public override bool VisitElement_declaration([NotNull] vhdlParser.Element_declarationContext context)
		{
			TypeVisitors.VHDLTypeResolverVisitor visitor = new TypeVisitors.VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
			VHDLType type = visitor.Visit(context.element_subtype_definition());
			foreach (var identifierContext in context.identifier_list().identifier())
			{
				try
				{
					VHDLRecordElementDeclaration decl = new VHDLRecordElementDeclaration(m_analysisResult, context, m_declarationStack.FirstOrDefault(), identifierContext.GetText());
					decl.Type = type;
					DeclarationsByContext.Add(context, decl);
					if (m_declarationStack.Count > 0)
						m_declarationStack.Peek().Children.Add(decl);
				}
				catch (Exception ex)
				{
				}
			}
			return true;
		}

	}
}
