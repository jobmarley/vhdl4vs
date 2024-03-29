﻿/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs.ExpressionVisitors
{
	class VHDLExpressionVisitor
		: vhdlBaseVisitor<VHDLExpression>
	{
		private Action<IVHDLToResolve, DeepAnalysisResult, Action<VHDLError>> m_resolveOverrider = null;
		private Action<VHDLError> m_errorListener = null;
		private AnalysisResult m_analysisResult = null;
		private IVHDLReference m_assignedToReference = null;

		private void AddToResolve(IVHDLToResolve toResolve)
		{
			if (m_resolveOverrider != null)
				m_analysisResult.AddToResolve(new VHDLFakeResolver(toResolve, m_resolveOverrider));
			else
				m_analysisResult.AddToResolve(toResolve);
		}
		public VHDLExpressionVisitor(AnalysisResult analysisResult, Action<VHDLError> errorListener,
			Action<IVHDLToResolve, DeepAnalysisResult, Action<VHDLError>> resolveOverrider = null,
			IVHDLReference assignedToReference = null)
		{
			m_analysisResult = analysisResult;
			m_errorListener = errorListener;
			m_resolveOverrider = resolveOverrider;
			m_assignedToReference = assignedToReference;
		}
		protected override VHDLExpression AggregateResult(VHDLExpression aggregate, VHDLExpression nextResult)
		{
			return null;
		}
		protected override bool ShouldVisitNextChild(IRuleNode node, VHDLExpression currentResult)
		{
			return false;
		}
		public override VHDLExpression VisitSimple_expression([NotNull] vhdlParser.Simple_expressionContext context)
		{
			VHDLExpression expr = null;
			if (context.MINUS() != null)
			{
				expr = new VHDLUnaryMinusExpression(m_analysisResult, Span.FromBounds(context.MINUS().Symbol.StartIndex, context.term()[0].Stop.StopIndex), VisitTerm(context.term()[0]));
			}
			else if (context.PLUS() != null)
			{
				expr = new VHDLUnaryPlusExpression(m_analysisResult, Span.FromBounds(context.PLUS().Symbol.StartIndex, context.term()[0].Stop.StopIndex), VisitTerm(context.term()[0]));
			}
			else
			{
				expr = VisitTerm(context.term()[0]);
			}

			if (context.adding_operator() != null)
			{
				foreach (var (addingContext, termContext) in context.adding_operator().Zip(context.term().Skip(1), (x, y) => Tuple.Create(x, y)))
				{
					VHDLExpression expr2 = VisitTerm(termContext);
					if (addingContext.PLUS() != null)
					{
						expr = new VHDLAddExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
					}
					else if (addingContext.MINUS() != null)
					{
						expr = new VHDLSubtractExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
					}
					else if (addingContext.AMPERSAND() != null)
					{
						expr = new VHDLConcatenateExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
					}
					else
						return null;
				}
			}

			return expr;
		}
		public override VHDLExpression VisitShift_expression([NotNull] vhdlParser.Shift_expressionContext context)
		{
			VHDLExpression expr = VisitSimple_expression(context.simple_expression()[0]);
			if (context.shift_operator() != null)
			{
				VHDLExpression expr2 = VisitSimple_expression(context.simple_expression()[1]);
				if (context.shift_operator().SLL() != null)
				{
					expr = new VHDLShiftLeftLogicalExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
				}
				else if (context.shift_operator().SRL() != null)
				{
					expr = new VHDLShiftRightLogicalExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
				}
				else if (context.shift_operator().SLA() != null)
				{
					expr = new VHDLShiftLeftArithmeticExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
				}
				else if (context.shift_operator().SRA() != null)
				{
					expr = new VHDLShiftRightArithmeticExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
				}
				else if (context.shift_operator().ROL() != null)
				{
					expr = new VHDLRotateLeftExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
				}
				else if (context.shift_operator().ROR() != null)
				{
					expr = new VHDLRotateRightExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
				}
				else
					return null;
			}
			return expr;
		}
		public override VHDLExpression VisitSimple_simultaneous_statement([NotNull] vhdlParser.Simple_simultaneous_statementContext context)
		{
			VHDLExpression expr1 = VisitSimple_expression(context.simple_expression()[0]);
			VHDLExpression expr2 = VisitSimple_expression(context.simple_expression()[1]);
			return new VHDLAssignExpression(m_analysisResult, expr1.Span.Union(expr2.Span), expr1, expr2);
		}
		public override VHDLExpression VisitRelation([NotNull] vhdlParser.RelationContext context)
		{
			VHDLExpression expr = VisitShift_expression(context.shift_expression()[0]);
			if (context.relational_operator() != null)
			{
				VHDLExpression expr2 = VisitShift_expression(context.shift_expression()[1]);
				if (context.relational_operator().NEQ() != null)
				{
					expr = new VHDLNotEqualExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
				}
				else if (context.relational_operator().LOWERTHAN() != null)
				{
					expr = new VHDLLowerExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
				}
				else if (context.relational_operator().LE() != null)
				{
					expr = new VHDLLowerOrEqualExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
				}
				else if (context.relational_operator().GREATERTHAN() != null)
				{
					expr = new VHDLGreaterExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
				}
				else if (context.relational_operator().GE() != null)
				{
					expr = new VHDLGreaterOrEqualExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
				}
				else if (context.relational_operator().EQ() != null)
				{
					expr = new VHDLIsEqualExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
				}
				else
					return null;
			}
			return expr;
		}
		public override VHDLExpression VisitExpression([NotNull] vhdlParser.ExpressionContext context)
		{
			VHDLExpression expr = VisitRelation(context.relation()[0]);
			if (context.logical_operator() != null)
			{
				foreach (var (operatorContext, relationContext) in context.logical_operator().Zip(context.relation().Skip(1), (x, y) => Tuple.Create(x, y)))
				{
					VHDLExpression expr2 = VisitRelation(relationContext);
					if (operatorContext.AND() != null)
					{
						expr = new VHDLAndExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
					}
					else if (operatorContext.OR() != null)
					{
						expr = new VHDLOrExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
					}
					else if (operatorContext.NAND() != null)
					{
						expr = new VHDLNandExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
					}
					else if (operatorContext.NOR() != null)
					{
						expr = new VHDLNorExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
					}
					else if (operatorContext.XOR() != null)
					{
						expr = new VHDLXorExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
					}
					else if (operatorContext.XNOR() != null)
					{
						expr = new VHDLXnorExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
					}
					else
						return null;
				}
			}
			return expr;
		}
		public override VHDLExpression VisitTerm([NotNull] vhdlParser.TermContext context)
		{
			VHDLExpression expr = null;
			if (context.factor()?.Length > 0)
			{
				expr = VisitFactor(context.factor()[0]);
			}
			if (context.multiplying_operator()?.Length > 0)
			{
				foreach (var (operatorContext, factorContext) in context.multiplying_operator().Zip(context.factor().Skip(1), (x, y) => Tuple.Create(x, y)))
				{
					VHDLExpression expr2 = VisitFactor(factorContext);
					if (operatorContext.MUL() != null)
					{
						expr = new VHDLMultiplyExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
					}
					else if (operatorContext.DIV() != null)
					{
						expr = new VHDLDivideExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
					}
					else if (operatorContext.MOD() != null)
					{
						expr = new VHDLModuloExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
					}
					else if (operatorContext.REM() != null)
					{
						expr = new VHDLRemainderExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2);
					}
					else
						return null;
				}
			}
			return expr;
		}
		public override VHDLExpression VisitFactor([NotNull] vhdlParser.FactorContext context)
		{
			VHDLExpression expr = VisitPrimary(context.primary()[0]);
			if (context.ABS() != null)
				return new VHDLAbsoluteExpression(m_analysisResult, context.GetSpan(), expr);
			else if (context.NOT() != null)
				return new VHDLNotExpression(m_analysisResult, context.GetSpan(), expr);

			if (context.DOUBLESTAR() != null)
			{
				VHDLExpression expr2 = VisitPrimary(context.primary()[1]);
				expr = new VHDLPowerExpression(m_analysisResult, context.GetSpan(), expr, expr2);
			}
			return expr;
		}
		public override VHDLExpression VisitPrimary([NotNull] vhdlParser.PrimaryContext context)
		{
			if (context.literal() != null)
			{
				return VisitLiteral(context.literal());
			}
			else if (context.qualified_expression() != null)
			{
				return null;
			}
			else if (context.expression() != null)
			{
				return VisitExpression(context.expression());
			}
			else if (context.allocator() != null)
			{
				return null;
			}
			else if (context.aggregate() != null)
			{
				return VisitAggregate(context.aggregate());
			}
			else if (context.name() != null)
			{
				return VisitName(context.name());
			}
			return null;
		}
		public override VHDLExpression VisitAggregate([NotNull] vhdlParser.AggregateContext context)
		{
			var tmpOverrider = m_resolveOverrider;
			// aggregate named elements must be resolved in the context of the assigned variable type
			m_resolveOverrider = (r, dar, errorListener) =>
			{
				VHDLNameExpression expr = r as VHDLNameExpression;
				if (expr == null)
					return;

				VHDLRecordType rt = (m_assignedToReference?.Declaration as VHDLAbstractVariableDeclaration)?.Type?.Dereference() as VHDLRecordType;
				if (rt == null)
				{
					// This is not a record, resolve the name normally
					r.Resolve(dar, errorListener);
					return;
				}
				expr.Declaration = rt.Fields.FirstOrDefault(x => string.Compare(x.Name, expr.Name) == 0);
				if (expr.Declaration == null)
				{
					// record but name was not found
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Name '{0}' cannot be found", expr.Name), expr.Span));
					return;
				}
				dar.SortedReferences.Add(expr.Span.Start,
					new VHDLNameReference(
						expr.Name,
						expr.Span,
						expr.Declaration));
			};
			List<VHDLExpression> elements = new List<VHDLExpression>();
			foreach (var elemContext in context.element_association())
			{
				elements.Add(VisitElement_association(elemContext));
			}
			m_resolveOverrider = tmpOverrider;
			return new VHDLAggregateExpression(m_analysisResult, context.GetSpan(), elements);
		}
		public override VHDLExpression VisitElement_association([NotNull] vhdlParser.Element_associationContext context)
		{
			// (name => expression, ...)
			// expression must be parsed after name
			// lets say we have something like "record <= (name1 => (name2 => value), ...)"
			// to resolve name1, we need to resolve record to get its type, and definition.
			// Only then can name1 be resolved to record.name1
			// Then we know the type of name1, and name2 can be resolved, etc...

			VHDLExpression e = null;
			if (context.choices() != null)
			{
				VHDLNameExpression nameExpr = null;
				List<VHDLExpression> choices = new List<VHDLExpression>();
				foreach (var choiceContext in context.choices().choice())
				{
					if (choiceContext.identifier() != null)
					{
						// element names need to be resolved in the context of the expected type (a record?)
						nameExpr = new VHDLNameExpression(m_analysisResult, choiceContext.identifier().GetSpan(), choiceContext.identifier().GetText());
						choices.Add(nameExpr);
						AddToResolve(nameExpr);
					}
					else if (choiceContext.discrete_range() != null)
					{
						choices.Add(VisitDiscrete_range(choiceContext.discrete_range()));
					}
					else if (choiceContext.simple_expression() != null)
					{
						choices.Add(VisitSimple_expression(choiceContext.simple_expression()));
					}
					else if (choiceContext.OTHERS() != null)
					{
						choices.Add(new VHDLOthersExpression(m_analysisResult, choiceContext.OTHERS().Symbol.GetSpan()));
					}
				}
				// We must create a new visitor because the context is different, and otherwise it will be overriden
				e = new VHDLExpressionVisitor(m_analysisResult, m_errorListener, null, nameExpr).Visit(context.expression());
				return new VHDLArgumentAssociationExpression(m_analysisResult, context.GetSpan(), choices, e);
			}

			e = new VHDLExpressionVisitor(m_analysisResult, m_errorListener).Visit(context.expression());
			return e;
		}
		
		public override VHDLExpression VisitName([NotNull] vhdlParser.NameContext context)
		{
			VHDLNameExpressionVisitor visitor = new VHDLNameExpressionVisitor(m_analysisResult, m_errorListener, m_resolveOverrider);
			return visitor.Visit(context);
		}
		public override VHDLExpression VisitSelected_name([NotNull] vhdlParser.Selected_nameContext context)
		{
			VHDLNameExpressionVisitor visitor = new VHDLNameExpressionVisitor(m_analysisResult, m_errorListener, m_resolveOverrider);
			return visitor.Visit(context);
		}
		public override VHDLExpression VisitActual_part([NotNull] vhdlParser.Actual_partContext context)
		{
			VHDLNameExpressionVisitor visitor = new VHDLNameExpressionVisitor(m_analysisResult, m_errorListener, m_resolveOverrider);
			return visitor.Visit(context);
		}

		public override VHDLExpression VisitFormal_part([NotNull] vhdlParser.Formal_partContext context)
		{
			VHDLNameExpressionVisitor visitor = new VHDLNameExpressionVisitor(m_analysisResult, m_errorListener, m_resolveOverrider);
			return visitor.Visit(context);
		}
		public override VHDLExpression VisitAssociation_element([NotNull] vhdlParser.Association_elementContext context)
		{
			VHDLNameExpressionVisitor visitor = new VHDLNameExpressionVisitor(m_analysisResult, m_errorListener, m_resolveOverrider);
			return visitor.Visit(context);
		}

		public VHDLExpression VisitSTRING_LITERAL(ITerminalNode node)
		{
			string s = node.GetText();
			if (s.StartsWith("b") || s.StartsWith("B"))
				return new VHDLBinaryStringLiteral(m_analysisResult, node.Symbol.GetSpan(), s.Substring(2, s.Length - 3));
			else if (s.StartsWith("o") || s.StartsWith("O"))
				return new VHDLOctalStringLiteral(m_analysisResult, node.Symbol.GetSpan(), s.Substring(2, s.Length - 3));
			else if (s.StartsWith("x") || s.StartsWith("X"))
				return new VHDLHexStringLiteral(m_analysisResult, node.Symbol.GetSpan(), s.Substring(2, s.Length - 3));
			else
			{
				return new VHDLStringLiteral(m_analysisResult, node.Symbol.GetSpan(), s.Substring(1, s.Length - 2));
			}
		}
		public override VHDLExpression VisitLiteral([NotNull] vhdlParser.LiteralContext context)
		{
			if (context.NULL_() != null)
			{
				return new VHDLNull(m_analysisResult, context.NULL_().Symbol.GetSpan());
			}
			else if (context.BIT_STRING_LITERAL() != null)
			{
				return VisitSTRING_LITERAL(context.BIT_STRING_LITERAL());
			}
			else if (context.STRING_LITERAL() != null)
			{
				return VisitSTRING_LITERAL(context.STRING_LITERAL());
			}
			else if (context.numeric_literal() != null)
			{
				return VisitNumeric_literal(context.numeric_literal());
			}
			else if (context.enumeration_literal() != null)
			{
				if (context.enumeration_literal().identifier() != null)
				{
					VHDLNameExpression nameExpression = new VHDLNameExpression(m_analysisResult, context.enumeration_literal().identifier().GetSpan(), context.enumeration_literal().identifier().GetText());
					AddToResolve(nameExpression);
					return nameExpression;
				}
				else if (context.enumeration_literal().CHARACTER_LITERAL() != null)
					return new VHDLCharacterLiteral(m_analysisResult, context.enumeration_literal().CHARACTER_LITERAL().Symbol.GetSpan(), context.enumeration_literal().CHARACTER_LITERAL().GetText()[1]);
			}
			return null;
		}
		public override VHDLExpression VisitNumeric_literal([NotNull] vhdlParser.Numeric_literalContext context)
		{
			if (context.abstract_literal() != null)
				return VisitAbstract_literal(context.abstract_literal());
			else if (context.physical_literal() != null)
				return VisitPhysical_literal(context.physical_literal());
			return null;
		}
		private double ParseReal(string s)
		{
			// Should check if converted back to string is equal to s, if not we lack precision
			// Or should check if result is +infinity/-infinity
			return double.Parse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
		}
		public override VHDLExpression VisitAbstract_literal([NotNull] vhdlParser.Abstract_literalContext context)
		{
			if (context.INTEGER() != null)
			{
				return new VHDLIntegerLiteral(m_analysisResult, context.INTEGER().Symbol.GetSpan(), long.Parse(context.INTEGER().GetText()), context.INTEGER().GetText());
			}
			else if (context.REAL_LITERAL() != null)
			{
				return new VHDLRealLiteral(m_analysisResult, context.REAL_LITERAL().Symbol.GetSpan(), ParseReal(context.REAL_LITERAL().GetText()), context.REAL_LITERAL().GetText());
			}
			else if (context.BASE_LITERAL() != null)
			{
				return null;
			}
			return base.VisitAbstract_literal(context);
		}
		public override VHDLExpression VisitPhysical_literal([NotNull] vhdlParser.Physical_literalContext context)
		{
			VHDLExpression literal = Visit(context.abstract_literal());
			return new VHDLPhysicalLiteral(m_analysisResult, context.GetSpan(), literal as VHDLLiteral, context.identifier().GetText());
		}

		public override VHDLExpression VisitDiscrete_range([NotNull] vhdlParser.Discrete_rangeContext context)
		{
			if (context.range_decl() != null)
				return VisitRange_decl(context.range_decl());
			else if (context.subtype_indication() != null)
				return VisitSubtype_indication(context.subtype_indication());
			return null;
		}
		public override VHDLExpression VisitSubtype_indication([NotNull] vhdlParser.Subtype_indicationContext context)
		{
			return null;
		}

		public override VHDLExpression VisitRange_decl([NotNull] vhdlParser.Range_declContext context)
		{
			if (context.explicit_range() != null)
				return VisitExplicit_range(context.explicit_range());
			else if (context.name() != null)
				return VisitName(context.name());
			return null;
		}
		public override VHDLExpression VisitExplicit_range([NotNull] vhdlParser.Explicit_rangeContext context)
		{
			VHDLExpressionVisitor visitor = new VHDLExpressionVisitor(m_analysisResult, m_errorListener);
			VHDLExpression expr = visitor.Visit(context.simple_expression()[0]);
			if (context.simple_expression().Count() > 1)
			{
				VHDLExpression expr2 = visitor.Visit(context.simple_expression()[1]);
				return new VHDLRangeExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2, context.direction()?.DOWNTO() != null ? VHDLRangeDirection.DownTo : VHDLRangeDirection.To);
			}

			return expr;
		}
	}

	class VHDLNameExpressionVisitor
		: vhdlBaseVisitor<VHDLExpression>
	{
		private Action<IVHDLToResolve, DeepAnalysisResult, Action<VHDLError>> m_resolveOverrider = null;
		private Action<VHDLError> m_errorListener = null;
		private AnalysisResult m_analysisResult = null;

		private VHDLExpression m_currentExpression = null;

		private void AddToResolve(IVHDLToResolve toResolve)
		{
			if (m_resolveOverrider != null)
				m_analysisResult.AddToResolve(new VHDLFakeResolver(toResolve, m_resolveOverrider));
			else
				m_analysisResult.AddToResolve(toResolve);
		}
		public VHDLNameExpressionVisitor(AnalysisResult analysisResult, Action<VHDLError> errorListener = null, Action<IVHDLToResolve, DeepAnalysisResult, Action<VHDLError>> resolveOverrider = null)
		{
			m_analysisResult = analysisResult;
			m_errorListener = errorListener;
			m_resolveOverrider = resolveOverrider;
		}
		protected override VHDLExpression AggregateResult(VHDLExpression aggregate, VHDLExpression nextResult)
		{
			return null;
		}
		protected override bool ShouldVisitNextChild(IRuleNode node, VHDLExpression currentResult)
		{
			return false;
		}
		public override VHDLExpression VisitName([NotNull] vhdlParser.NameContext context)
		{
			if (context.identifier() != null)
			{
				m_currentExpression = new VHDLNameExpression(m_analysisResult, context.identifier().GetSpan(), context.identifier().GetText());
				AddToResolve(m_currentExpression as VHDLNameExpression);
			}
			else if (context.STRING_LITERAL() != null)
			{
				m_currentExpression = new VHDLExpressionVisitor(m_analysisResult, m_errorListener).VisitSTRING_LITERAL(context.STRING_LITERAL());
			}

			if (context.name_part() != null)
			{
				foreach (var namePartContext in context.name_part())
				{
					m_currentExpression = VisitName_part(namePartContext);
				}
			}
			return m_currentExpression;
		}
		public override VHDLExpression VisitSuffix([NotNull] vhdlParser.SuffixContext context)
		{
			if (context.identifier() != null)
			{
				m_currentExpression = new VHDLMemberSelectExpression(m_analysisResult,
					m_currentExpression.Span.Union(context.identifier().GetSpan()),
					context.identifier().GetSpan(),
					m_currentExpression,
					context.identifier().GetText());
				AddToResolve(m_currentExpression as VHDLMemberSelectExpression);
				return m_currentExpression;
			}
			else if (context.ALL() != null)
			{
				m_currentExpression = new VHDLMemberSelectExpression(m_analysisResult,
					m_currentExpression.Span.Union(context.ALL().Symbol.GetSpan()),
					context.ALL().Symbol.GetSpan(),
					m_currentExpression,
					context.ALL().GetText());
				AddToResolve(m_currentExpression as VHDLMemberSelectExpression);
				return m_currentExpression;
			}
			else
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "strings and characters in names not supported", context.GetSpan()));
				return null;
			}
		}
		public override VHDLExpression VisitSelected_name([NotNull] vhdlParser.Selected_nameContext context)
		{
			if (m_currentExpression != null)
			{
				m_currentExpression = new VHDLMemberSelectExpression(m_analysisResult, m_currentExpression.Span.Union(context.identifier().GetSpan()), context.identifier().GetSpan(), m_currentExpression, context.identifier().GetText());
				AddToResolve(m_currentExpression as VHDLMemberSelectExpression);
			}
			else
			{
				m_currentExpression = new VHDLNameExpression(m_analysisResult, context.identifier().GetSpan(), context.identifier().GetText());
				AddToResolve(m_currentExpression as VHDLNameExpression);
			}
			if (context.suffix() != null)
			{
				foreach (var suffixContext in context.suffix())
					VisitSuffix(suffixContext);
			}
			return m_currentExpression;
		}

		public override VHDLExpression VisitAttribute_name_part([NotNull] vhdlParser.Attribute_name_partContext context)
		{
			m_currentExpression = new VHDLAttributeExpression(m_analysisResult, context.attribute_designator().GetSpan(), m_currentExpression, context.attribute_designator().GetText());
			AddToResolve(m_currentExpression as VHDLAttributeExpression);
			if (context.expression() != null)
			{
				VHDLExpression expr = new VHDLExpressionVisitor(m_analysisResult, m_errorListener).Visit(context.expression());
				m_currentExpression = new VHDLFunctionCallOrIndexExpression(m_analysisResult, m_currentExpression.Span.Union(context.GetSpan()), m_currentExpression, new VHDLExpression[] { expr });
			}

			return m_currentExpression;
		}
		public override VHDLExpression VisitName_part([NotNull] vhdlParser.Name_partContext context)
		{
			if (context.selected_name_part() != null)
				return VisitSelected_name_part(context.selected_name_part());
			if (context.attribute_name_part() != null)
			{
				return VisitAttribute_name_part(context.attribute_name_part());
			}
			else if (context.function_call_or_indexed_name_part() != null)
			{
				VHDLFunctionCallOrIndexExpression fce = new VHDLFunctionCallOrIndexExpression(m_analysisResult, m_currentExpression.Span.Union(context.function_call_or_indexed_name_part().GetSpan()), m_currentExpression, null);
				var tmpOverrider = m_resolveOverrider;
				m_resolveOverrider = (x, y, z) => fce.ResolveFunctionParameter(x, y, z);
				List<VHDLExpression> arguments = new List<VHDLExpression>();
				if (context.function_call_or_indexed_name_part().actual_parameter_part()?.association_list()?.association_element() != null)
				{
					foreach (var elementContext in context.function_call_or_indexed_name_part().actual_parameter_part()?.association_list()?.association_element())
					{
						arguments.Add(VisitAssociation_element(elementContext));
						if (arguments.Last() == null)
						{
							int zqdqd = 0;
						}
					}
				}
				m_resolveOverrider = tmpOverrider;
				fce.Arguments = arguments;
				m_currentExpression = fce;
				return fce;
			}
			else if (context.slice_name_part() != null)
			{
				return VisitSlice_name_part(context.slice_name_part());
			}

			return m_currentExpression;
		}
		public override VHDLExpression VisitSlice_name_part([NotNull] vhdlParser.Slice_name_partContext context)
		{
			List<VHDLExpression> arguments = new List<VHDLExpression>();
			arguments.Add(new VHDLExpressionVisitor(m_analysisResult, m_errorListener, m_resolveOverrider).VisitDiscrete_range(context.discrete_range()));
			m_currentExpression = new VHDLFunctionCallOrIndexExpression(m_analysisResult, m_currentExpression.Span.Union(context.GetSpan()), m_currentExpression, arguments);
			if (arguments.Last() == null)
			{
				int qqzqdqz = 0;
			}
			return m_currentExpression;
		}

		public override VHDLExpression VisitSelected_name_part([NotNull] vhdlParser.Selected_name_partContext context)
		{
			if (context.suffix() != null)
			{
				foreach (var suffixContext in context.suffix())
					VisitSuffix(suffixContext);
			}
			return m_currentExpression;
		}
		public override VHDLExpression VisitAssociation_element([NotNull] vhdlParser.Association_elementContext context)
		{
			VHDLExpression formalPartExpression = null;
			if (context.formal_part() != null)
			{
				formalPartExpression = VisitFormal_part(context.formal_part());
			}

			VHDLExpression valueExpression = new VHDLNameExpressionVisitor(m_analysisResult, m_errorListener).VisitActual_part(context.actual_part());
			if (formalPartExpression != null)
				return new VHDLArgumentAssociationExpression(m_analysisResult, context.GetSpan(), new[] { formalPartExpression }, valueExpression);
			else
				return valueExpression;
		}
		public override VHDLExpression VisitFormal_part([NotNull] vhdlParser.Formal_partContext context)
		{
			VHDLNameExpression expr = new VHDLNameExpression(m_analysisResult, context.identifier().GetSpan(), context.identifier().GetText());
			AddToResolve(expr);
			if (context.explicit_range() != null)
			{
				VHDLExpression rangeExpression = new VHDLNameExpressionVisitor(m_analysisResult, m_errorListener).VisitExplicit_range(context.explicit_range());
				return new VHDLFunctionCallOrIndexExpression(m_analysisResult, expr.Span.Union(rangeExpression.Span), expr, new VHDLExpression[] { rangeExpression } );
			}
			return expr;
		}

		public override VHDLExpression VisitExplicit_range([NotNull] vhdlParser.Explicit_rangeContext context)
		{
			VHDLExpressionVisitor visitor = new VHDLExpressionVisitor(m_analysisResult, m_errorListener);
			VHDLExpression expr = visitor.Visit(context.simple_expression()[0]);
			if (context.simple_expression().Count() > 1)
			{
				VHDLExpression expr2 = visitor.Visit(context.simple_expression()[1]);
				return new VHDLRangeExpression(m_analysisResult, expr.Span.Union(expr2.Span), expr, expr2, context.direction()?.DOWNTO() != null ? VHDLRangeDirection.DownTo : VHDLRangeDirection.To);
			}

			return expr;
		}

		public override VHDLExpression VisitActual_part([NotNull] vhdlParser.Actual_partContext context)
		{
			if (context.name() != null)
			{
				VHDLNameExpressionVisitor visitor = new VHDLNameExpressionVisitor(m_analysisResult, m_errorListener);
				VHDLExpression expr = visitor.Visit(context.name());
				VHDLExpression designatorExpr = VisitActual_designator(context.actual_designator());
				return new VHDLFunctionCallOrIndexExpression(m_analysisResult, expr.Span.Union(designatorExpr.Span), expr, new VHDLExpression[] { designatorExpr });
			}
			else
			{
				return VisitActual_designator(context.actual_designator());
			}
		}

		public override VHDLExpression VisitActual_designator([NotNull] vhdlParser.Actual_designatorContext context)
		{
			if (context.OPEN() != null)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "'open' not supported", context.GetSpan()));
				return null;
			}
			else
			{
				VHDLExpressionVisitor visitor = new VHDLExpressionVisitor(m_analysisResult, m_errorListener);
				return visitor.Visit(context.expression());
			}
		}
	}
}
