using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompany.LanguageServices.VHDL.ExpressionVisitors
{
	class VHDLFakeResolver
		: IVHDLToResolve
	{
		public VHDLFakeResolver(IVHDLToResolve overriden, Action<IVHDLToResolve, DeepAnalysisResult, Action<VHDLError>> resolver)
		{
			Overriden = overriden;
			Resolver = resolver;
		}
		public IVHDLToResolve Overriden { get; set; } = null;
		public Action<IVHDLToResolve, DeepAnalysisResult, Action<VHDLError>> Resolver { get; set; } = null;

		public void Resolve(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
			Resolver(Overriden, deepAnalysisResult, errorListener);
		}
	}
	class VHDLExpressionVisitor
		: vhdlBaseVisitor<VHDLExpression>
	{
		private Action<IVHDLToResolve, DeepAnalysisResult, Action<VHDLError>> m_resolveOverrider = null;
		private Action<VHDLError> m_errorListener = null;
		private AnalysisResult m_analysisResult = null;
		public VHDLExpressionVisitor(AnalysisResult analysisResult, Action<VHDLError> errorListener, Action<IVHDLToResolve, DeepAnalysisResult, Action<VHDLError>> resolveOverrider = null)
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
		public override VHDLExpression VisitSimple_expression([NotNull] vhdlParser.Simple_expressionContext context)
		{
			VHDLExpression expr = null;
			if (context.MINUS() != null)
			{
				expr = new VHDLUnaryMinusExpression(m_analysisResult, Span.FromBounds(context.MINUS().Symbol.StartIndex, context.term()[0].Stop.StopIndex), VisitTerm(context.term()[0]));
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
				return null;
			}
			else if (context.name() != null)
			{
				return VisitName(context.name());
			}
			return null;
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
		public override VHDLExpression VisitLiteral([NotNull] vhdlParser.LiteralContext context)
		{
			if (context.NULL() != null)
			{
				return new VHDLNull(m_analysisResult, context.NULL().Symbol.GetSpan());
			}
			else if(context.BIT_STRING_LITERAL() != null)
			{
				string s = context.BIT_STRING_LITERAL().GetText();
				if (s.StartsWith("b") || s.StartsWith("B"))
					return new VHDLBinaryStringLiteral(m_analysisResult, context.BIT_STRING_LITERAL().Symbol.GetSpan(), s.Substring(2, s.Length - 3));
				else if (s.StartsWith("o") || s.StartsWith("O"))
					return new VHDLOctalStringLiteral(m_analysisResult, context.BIT_STRING_LITERAL().Symbol.GetSpan(), s.Substring(2, s.Length - 3));
				else if (s.StartsWith("x") || s.StartsWith("X"))
					return new VHDLHexStringLiteral(m_analysisResult, context.BIT_STRING_LITERAL().Symbol.GetSpan(), s.Substring(2, s.Length - 3));
			}
			else if (context.STRING_LITERAL() != null)
			{
				string s = context.STRING_LITERAL().GetText();
				return new VHDLStringLiteral(m_analysisResult, context.STRING_LITERAL().Symbol.GetSpan(), s.Substring(1, s.Length - 2));
			}
			else if (context.CHARACTER_LITERAL() != null)
			{
				return new VHDLCharacterLiteral(m_analysisResult, context.CHARACTER_LITERAL().Symbol.GetSpan(), context.CHARACTER_LITERAL().GetText()[1]);
			}
			else if (context.numeric_literal() != null)
			{
				return VisitNumeric_literal(context.numeric_literal());
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
			if (context.selected_name() != null)
			{
				return VisitSelected_name(context.selected_name());
			}
			else if (context.name_part() != null)
			{
				foreach (var namePartContext in context.name_part())
				{
					m_currentExpression = VisitName_part(namePartContext);
				}

				return m_currentExpression;
			}
			return null;
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
				{
					if (suffixContext.identifier() != null)
					{
						m_currentExpression = new VHDLMemberSelectExpression(m_analysisResult, 
							m_currentExpression.Span.Union(suffixContext.identifier().GetSpan()),
							suffixContext.identifier().GetSpan(),
							m_currentExpression,
							suffixContext.identifier().GetText());
						AddToResolve(m_currentExpression as VHDLMemberSelectExpression);
					}
					else if (suffixContext.ALL() != null)
					{
						m_currentExpression = new VHDLMemberSelectExpression(m_analysisResult,
							m_currentExpression.Span.Union(suffixContext.ALL().Symbol.GetSpan()),
							suffixContext.ALL().Symbol.GetSpan(),
							m_currentExpression,
							suffixContext.ALL().GetText());
						AddToResolve(m_currentExpression as VHDLMemberSelectExpression);
					}
					else
					{
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "strings and characters in names not supported", suffixContext.GetSpan()));
						return null;
					}
				}
			}
			return m_currentExpression;
		}
		public override VHDLExpression VisitName_part([NotNull] vhdlParser.Name_partContext context)
		{
			VHDLExpression nameExpression = VisitSelected_name(context.selected_name());

			if (context.name_attribute_part() != null)
			{
				/*if (context.name_attribute_part().expression() != null && context.name_attribute_part().expression().Length > 0)
				{
					VHDLExpression expr = null;
					try
					{
						expr = new VHDLExpressionVisitor(m_analysisResult, m_toResolve, m_errorListener).Visit(context.name_attribute_part().expression()[0]);
					}
					catch (Exception e)
					{
					}
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "attributes with expressions not supported", context.name_attribute_part().GetSpan()));
					return null;
				}*/
				return new VHDLAttributeExpression(m_analysisResult, context.name_attribute_part().GetSpan(), nameExpression, context.name_attribute_part().attribute_designator().GetText());
			}
			else if (context.name_function_call_or_indexed_part() != null)
			{
				List<VHDLExpression> arguments = new List<VHDLExpression>();
				if (context.name_function_call_or_indexed_part().actual_parameter_part()?.association_list()?.association_element() != null)
				{
					foreach (var elementContext in context.name_function_call_or_indexed_part().actual_parameter_part()?.association_list()?.association_element())
					{
						arguments.Add(VisitAssociation_element(elementContext));
					}
				}

				return new VHDLFunctionCallOrIndexExpression(m_analysisResult, nameExpression.Span.Union(context.name_function_call_or_indexed_part().GetSpan()), nameExpression, arguments);
			}
			else if (context.name_slice_part() != null)
			{
				foreach (var slicePartContext in context.name_slice_part())
				{
					List<VHDLExpression> arguments = new List<VHDLExpression>();
					foreach (var rangeContext in slicePartContext.explicit_range())
					{
						arguments.Add(VisitExplicit_range(rangeContext));
					}
					nameExpression = new VHDLFunctionCallOrIndexExpression(m_analysisResult, nameExpression.Span.Union(slicePartContext.GetSpan()), nameExpression, arguments);
				}
				return nameExpression;
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

			VHDLExpression valueExpression = VisitActual_part(context.actual_part());
			if (formalPartExpression != null)
				return new VHDLArgumentAssociationExpression(m_analysisResult, context.GetSpan(), formalPartExpression, valueExpression);
			else
				return valueExpression;
		}
		public override VHDLExpression VisitFormal_part([NotNull] vhdlParser.Formal_partContext context)
		{
			VHDLNameExpression expr = new VHDLNameExpression(m_analysisResult, context.identifier().GetSpan(), context.identifier().GetText());
			AddToResolve(expr);
			if (context.explicit_range() != null)
			{
				VHDLExpression rangeExpression = VisitExplicit_range(context.explicit_range());
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
