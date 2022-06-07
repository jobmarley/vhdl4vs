/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs
{
	internal class VHDLStatementVisitor
		: vhdlBaseVisitor<VHDLStatement>
	{
		private Action<VHDLError> m_errorListener = null;
		private AnalysisResult m_analysisResult = null;
		private VHDLDeclaration m_parent = null;
		public VHDLStatementVisitor(AnalysisResult analysisResult, VHDLDeclaration parent, Action<VHDLError> errorListener = null)
		{
			m_analysisResult = analysisResult;
			m_parent = parent;
			m_errorListener = errorListener;
		}
		protected override VHDLStatement AggregateResult(VHDLStatement aggregate, VHDLStatement nextResult)
		{
			return null;
		}
		protected override bool ShouldVisitNextChild(IRuleNode node, VHDLStatement currentResult)
		{
			return false;
		}
		public override VHDLStatement VisitSequential_statement([NotNull] vhdlParser.Sequential_statementContext context)
		{
			if (context.wait_statement() != null)
			{
				return Visit(context.wait_statement());
			}
			else if (context.assertion_statement() != null)
			{
				return Visit(context.assertion_statement());
			}
			else if (context.report_statement() != null)
			{
				return Visit(context.report_statement());
			}
			else if (context.signal_assignment_statement() != null)
			{
				return Visit(context.signal_assignment_statement());
			}
			else if (context.variable_assignment_statement() != null)
			{
				return Visit(context.variable_assignment_statement());
			}
			else if (context.if_statement() != null)
			{
				return Visit(context.if_statement());
			}
			else if (context.case_statement() != null)
			{
				return Visit(context.case_statement());
			}
			else if (context.loop_statement() != null)
			{
				return Visit(context.loop_statement());
			}
			else if (context.next_statement() != null)
			{
				return Visit(context.next_statement());
			}
			else if (context.exit_statement() != null)
			{
				return Visit(context.exit_statement());
			}
			else if (context.return_statement() != null)
			{
				return Visit(context.return_statement());
			}
			else if (context.NULL() != null)
			{

			}
			else if (context.break_statement() != null)
			{
				return Visit(context.break_statement());
			}
			else if (context.procedure_call_statement() != null)
			{
				return Visit(context.procedure_call_statement());
			}
			return null;
		}
		public override VHDLStatement VisitBreak_statement([NotNull] vhdlParser.Break_statementContext context)
		{
			VHDLBreakStatement statement = new VHDLBreakStatement(m_analysisResult, m_parent);
			m_analysisResult.AddStatement(context, statement);
			return statement;
		}
		public override VHDLStatement VisitReturn_statement([NotNull] vhdlParser.Return_statementContext context)
		{
			VHDLExpression expression = null;
			if (context.expression() != null)
				expression = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener).Visit(context.expression());

			VHDLStatement statement = new VHDLReturnStatement(m_analysisResult, m_parent, expression);
			m_analysisResult.AddStatement(context, statement);
			return statement;
		}
		public override VHDLStatement VisitExit_statement([NotNull] vhdlParser.Exit_statementContext context)
		{
			VHDLExitStatement statement = new VHDLExitStatement(m_analysisResult, m_parent);
			if (context.identifier() != null)
				statement.Name = context.identifier().GetText();
			if (context.condition()?.expression() != null)
				statement.Condition = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener).Visit(context.condition()?.expression());

			m_analysisResult.AddStatement(context, statement);
			return statement;
		}
		public override VHDLStatement VisitNext_statement([NotNull] vhdlParser.Next_statementContext context)
		{
			VHDLNextStatement statement = new VHDLNextStatement(m_analysisResult, m_parent);
			if (context.identifier() != null)
				statement.Name = context.identifier().GetText();
			if (context.condition()?.expression() != null)
				statement.Condition = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener).Visit(context.condition()?.expression());

			m_analysisResult.AddStatement(context, statement);
			return statement;
		}
		public override VHDLStatement VisitCase_statement([NotNull] vhdlParser.Case_statementContext context)
		{
			ExpressionVisitors.VHDLExpressionVisitor visitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);

			VHDLCaseStatement statement = new VHDLCaseStatement(m_analysisResult, m_parent);
			statement.Expression = visitor.Visit(context.expression());
			foreach (var alternativeContext in context.case_statement_alternative())
			{
				VHDLCaseAlternative alternative = new VHDLCaseAlternative();
				foreach (var choiceContext in alternativeContext.choices().choice())
				{
					try
					{
						if (choiceContext.identifier() != null)
							alternative.Conditions.Add(new VHDLNameExpression(m_analysisResult, choiceContext.identifier().GetSpan(), choiceContext.identifier().GetText()));
						else if (choiceContext.discrete_range() != null)
						{
							alternative.Conditions.Add(null);
							m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, "'discrete_range' in case not supported", choiceContext.discrete_range().GetSpan()));
						}
						else if (choiceContext.simple_expression() != null)
						{
							alternative.Conditions.Add(visitor.Visit(choiceContext.simple_expression()));
						}
						else if (choiceContext.OTHERS() != null)
							alternative.Conditions.Add(new VHDLOthersExpression(m_analysisResult, choiceContext.OTHERS().Symbol.GetSpan()));
					}
					catch (Exception e)
					{
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", choiceContext.GetSpan()));
					}
				}

				foreach (var statementContext in alternativeContext.sequence_of_statements().sequential_statement())
				{
					try
					{
						VHDLStatement s = Visit(statementContext);
						alternative.Statements.Add(s);
					}
					catch (Exception e)
					{
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", statementContext.GetSpan()));
					}
				}
				statement.Alternatives.Add(alternative);
			}

			m_analysisResult.AddStatement(context, statement);
			return statement;
		}
		public override VHDLStatement VisitProcedure_call_statement([NotNull] vhdlParser.Procedure_call_statementContext context)
		{
			return Visit(context.procedure_call());
		}
		public override VHDLStatement VisitProcedure_call([NotNull] vhdlParser.Procedure_callContext context)
		{
			ExpressionVisitors.VHDLNameExpressionVisitor visitor = new ExpressionVisitors.VHDLNameExpressionVisitor(m_analysisResult, m_errorListener);
			ExpressionVisitors.VHDLNameExpressionVisitor paramNameVisitor = new ExpressionVisitors.VHDLNameExpressionVisitor(m_analysisResult, m_errorListener, (x, y, z) => { });
			VHDLProcedureCallStatement statement = new VHDLProcedureCallStatement(m_analysisResult, m_parent);
			statement.NameExpression = visitor.Visit(context.selected_name());
			if (context.actual_parameter_part()?.association_list()?.association_element() != null)
			{
				foreach (var elementContext in context.actual_parameter_part()?.association_list()?.association_element())
				{
					VHDLExpression formalPartExpression = null;
					if (elementContext.formal_part() != null)
					{
						formalPartExpression = paramNameVisitor.VisitFormal_part(elementContext.formal_part());
					}

					VHDLExpression valueExpression = visitor.VisitActual_part(elementContext.actual_part());
					if (formalPartExpression != null)
						statement.Arguments.Add(new VHDLArgumentAssociationExpression(m_analysisResult, context.GetSpan(), new[] { formalPartExpression }, valueExpression));
					else
						statement.Arguments.Add(valueExpression);
				}
			}

			m_analysisResult.AddStatement(context, statement);
			return statement;
		}
		public override VHDLStatement VisitArchitecture_statement([NotNull] vhdlParser.Architecture_statementContext context)
		{
			if (context.block_statement() != null)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, "block statements not supported", context.GetSpan()));
				return null;
			}
			else if (context.process_statement() != null)
			{
				// Handled in VHDLDeclarationVisitor
				return null;
			}
			else if (context.concurrent_procedure_call_statement() != null)
			{
				return Visit(context.concurrent_procedure_call_statement().procedure_call());
			}
			else if (context.concurrent_assertion_statement() != null)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, "assertion statements not supported", context.GetSpan()));
				return null;
			}
			else if (context.concurrent_signal_assignment_statement() != null)
			{
				return Visit(context.concurrent_signal_assignment_statement());
			}
			else if (context.component_instantiation_statement() != null)
			{
				return Visit(context.component_instantiation_statement());
			}
			else if (context.generate_statement() != null)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, "generate statements not supported", context.GetSpan()));
				return null;
			}
			else if (context.concurrent_break_statement() != null)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, "concurrent break statements not supported", context.GetSpan()));
				return null;
			}
			else if (context.simultaneous_statement() != null)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, "simultaneous statements not supported", context.GetSpan()));
				return null;
			}

			return null;
		}
		public override VHDLStatement VisitComponent_instantiation_statement([NotNull] vhdlParser.Component_instantiation_statementContext context)
		{
			VHDLComponentInstanciationStatement statement = new VHDLComponentInstanciationStatement(m_analysisResult, m_parent);
			statement.Name = context.label_colon()?.identifier()?.GetText();
			if (context.instantiated_unit()?.ENTITY() != null)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, "entity component instantiation not supported", context.GetSpan()));
				return null;
			}
			if (context.instantiated_unit()?.CONFIGURATION() != null)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, "configuration component instantiation not supported", context.GetSpan()));
				return null;
			}

			ExpressionVisitors.VHDLExpressionVisitor visitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
			statement.ComponentNameExpression = visitor.Visit(context.instantiated_unit().name()) as VHDLReferenceExpression;
			foreach (var elementContext in context.port_map_aspect().association_list().association_element())
			{
				ExpressionVisitors.VHDLExpressionVisitor portVisitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener, (x, y, z) => statement.ResolvePortName(x, y, z));
				try
				{
					VHDLExpression v = visitor.Visit(elementContext.actual_part());
					VHDLExpression port = null;
					if (elementContext.formal_part() != null)
						port = portVisitor.Visit(elementContext.formal_part());

					if (port != null)
						statement.Parameters.Add(new VHDLArgumentAssociationExpression(m_analysisResult, elementContext.GetSpan(), new[] { port }, v));
					else
						statement.Parameters.Add(v);
				}
				catch (Exception ex)
				{
					// so the parameters stay ordered
					statement.Parameters.Add(null);
				}
			}
			m_analysisResult.AddStatement(context, statement);
			return statement;
		}
		public override VHDLStatement VisitConcurrent_signal_assignment_statement([NotNull] vhdlParser.Concurrent_signal_assignment_statementContext context)
		{
			if (context.conditional_signal_assignment() != null)
			{
				return VisitConditional_signal_assignment(context.conditional_signal_assignment());
			}
			else if (context.selected_signal_assignment() != null)
			{
				return null;
			}
			return null;
		}
		public override VHDLStatement VisitSignal_assignment_statement([NotNull] vhdlParser.Signal_assignment_statementContext context)
		{
			ExpressionVisitors.VHDLExpressionVisitor visitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);

			VHDLSignalAssignmentStatement statement = new VHDLSignalAssignmentStatement(m_analysisResult, m_parent);
			if (context.target().name() != null)
				statement.NameExpression = visitor.Visit(context.target().name());
			else
				statement.NameExpression = visitor.Visit(context.target().aggregate());

			if (context.conditional_waveforms().waveform().UNAFFECTED() != null)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "'unaffected' not supported", context.conditional_waveforms().waveform().UNAFFECTED().Symbol.GetSpan()));
				return null;
			}
			var conditionalWaveformContext = context.conditional_waveforms();
			while (conditionalWaveformContext != null)
			{
				VHDLExpression conditionExpression = null;
				if (conditionalWaveformContext.condition()?.expression() != null)
					conditionExpression = visitor.Visit(conditionalWaveformContext.condition().expression());

				if (conditionalWaveformContext.waveform().UNAFFECTED() != null)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "'unaffected' not supported", context.conditional_waveforms().waveform().UNAFFECTED().Symbol.GetSpan()));
					return null;
				}

				if (conditionalWaveformContext.waveform().waveform_element().Length > 1)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "multiple assignment values not supported", conditionalWaveformContext.waveform().GetSpan()));
					return null;
				}
				var waveformElementContext = conditionalWaveformContext.waveform().waveform_element()[0];
				if (waveformElementContext.AFTER() != null)
				{
				}
				VHDLExpression valueExpression = visitor.Visit(waveformElementContext.expression()[0]);
				statement.Values.Add(new VHDLConditionalExpression(conditionExpression, valueExpression));


				conditionalWaveformContext = conditionalWaveformContext.conditional_waveforms();
			}
			m_analysisResult.AddStatement(context, statement);
			return statement;
		}
		public override VHDLStatement VisitConditional_signal_assignment([NotNull] vhdlParser.Conditional_signal_assignmentContext context)
		{
			ExpressionVisitors.VHDLExpressionVisitor visitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);

			VHDLSignalAssignmentStatement statement = new VHDLSignalAssignmentStatement(m_analysisResult, m_parent);
			if (context.target().name() != null)
				statement.NameExpression = visitor.Visit(context.target().name());
			else
				statement.NameExpression = visitor.Visit(context.target().aggregate());

			if (context.conditional_waveforms().waveform().UNAFFECTED() != null)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "'unaffected' not supported", context.conditional_waveforms().waveform().UNAFFECTED().Symbol.GetSpan()));
				return null;
			}
			var conditionalWaveformContext = context.conditional_waveforms();
			while (conditionalWaveformContext != null)
			{
				VHDLExpression conditionExpression = null;
				if (conditionalWaveformContext.condition()?.expression() != null)
					conditionExpression = visitor.Visit(conditionalWaveformContext.condition().expression());

				if (conditionalWaveformContext.waveform().UNAFFECTED() != null)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "'unaffected' not supported", context.conditional_waveforms().waveform().UNAFFECTED().Symbol.GetSpan()));
					return null;
				}

				if (conditionalWaveformContext.waveform().waveform_element().Length > 1)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "multiple assignment values not supported", conditionalWaveformContext.waveform().GetSpan()));
					return null;
				}
				var waveformElementContext = conditionalWaveformContext.waveform().waveform_element()[0];
				if (waveformElementContext.AFTER() != null)
				{
				}
				VHDLExpression valueExpression = visitor.Visit(waveformElementContext.expression()[0]);
				statement.Values.Add(new VHDLConditionalExpression(conditionExpression, valueExpression));


				conditionalWaveformContext = conditionalWaveformContext.conditional_waveforms();
			}
			m_analysisResult.AddStatement(context, statement);
			return statement;
		}

		public override VHDLStatement VisitVariable_assignment_statement([NotNull] vhdlParser.Variable_assignment_statementContext context)
		{
			ExpressionVisitors.VHDLExpressionVisitor visitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);

			VHDLVariableAssignmentStatement statement = new VHDLVariableAssignmentStatement(m_analysisResult, m_parent);
			if (context.target().name() != null)
				statement.NameExpression = visitor.Visit(context.target().name());
			else
				statement.NameExpression = visitor.Visit(context.target().aggregate());

			var conditionalExpressionContext = context.conditional_expression();
			while (conditionalExpressionContext != null)
			{
				VHDLExpression conditionExpression = null;
				if (conditionalExpressionContext.condition()?.expression() != null)
					conditionExpression = visitor.Visit(conditionalExpressionContext.condition().expression());

				VHDLExpression valueExpression = visitor.Visit(conditionalExpressionContext.expression());
				statement.Values.Add(new VHDLConditionalExpression(conditionExpression, valueExpression));

				conditionalExpressionContext = conditionalExpressionContext.conditional_expression();
			}
			m_analysisResult.AddStatement(context, statement);
			return statement;
		}

		public override VHDLStatement VisitIf_statement([NotNull] vhdlParser.If_statementContext context)
		{
			ExpressionVisitors.VHDLExpressionVisitor visitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
			VHDLIfStatement statement = new VHDLIfStatement(m_analysisResult, m_parent);
			statement.Condition = visitor.Visit(context.condition()[0].expression());
			foreach (var statementContext in context.sequence_of_statements()[0].sequential_statement())
			{
				try
				{
					VHDLStatement s = Visit(statementContext);
					statement.Statements.Add(s);
				}
				catch (Exception e)
				{
					m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", statementContext.GetSpan()));
				}
			}

			foreach (var (conditionContext, sequenceStatementContext) in context.condition().Skip(1).Zip(context.sequence_of_statements().Skip(1), (x, y) => Tuple.Create(x, y)))
			{
				VHDLExpression condition = visitor.Visit(conditionContext.expression());
				List<VHDLStatement> elseIfStatements = new List<VHDLStatement>();
				foreach (var statementContext in sequenceStatementContext.sequential_statement())
				{
					try
					{
						VHDLStatement s = Visit(statementContext);
						elseIfStatements.Add(s);
					}
					catch (Exception e)
					{
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", statementContext.GetSpan()));
					}
				}
				statement.ElseIfStatements.Add(Tuple.Create(condition, elseIfStatements));
			}

			if (context.sequence_of_statements().Length > context.condition().Length)
			{
				foreach (var statementContext in context.sequence_of_statements().Last().sequential_statement())
				{
					try
					{
						VHDLStatement s = Visit(statementContext);
						statement.ElseStatements.Add(s);
					}
					catch (Exception e)
					{
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", statementContext.GetSpan()));
					}
				}
			}
			m_analysisResult.AddStatement(context, statement);
			return statement;
		}

		public override VHDLStatement VisitLoop_statement([NotNull] vhdlParser.Loop_statementContext context)
		{
			if (context.iteration_scheme() == null)
			{
				m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "loops without for or while not supported", context.GetSpan()));
				return null;
			}
			if (context.iteration_scheme().WHILE() != null)
			{
				VHDLWhileStatement statement = new VHDLWhileStatement(m_analysisResult, m_parent);
				ExpressionVisitors.VHDLExpressionVisitor visitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
				statement.Condition = visitor.Visit(context.iteration_scheme().condition().expression());

				foreach (var statementContext in context.sequence_of_statements().sequential_statement())
				{
					try
					{
						VHDLStatement s = Visit(statementContext);
						statement.Statements.Add(s);
					}
					catch (Exception e)
					{
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", statementContext.GetSpan()));
					}
				}
				m_analysisResult.AddStatement(context, statement);
				return statement;
			}
			else if (context.iteration_scheme().FOR() != null)
			{
				VHDLForStatement statement = new VHDLForStatement(m_analysisResult, m_parent);
				ExpressionVisitors.VHDLExpressionVisitor visitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
				statement.Variable = context.iteration_scheme().parameter_specification().identifier().GetText();
				statement.Range = visitor.Visit(context.iteration_scheme().parameter_specification().discrete_range()) as VHDLRangeExpression;
				
				foreach (var statementContext in context.sequence_of_statements().sequential_statement())
				{
					try
					{
						VHDLStatement s = Visit(statementContext);
						statement.Statements.Add(s);
					}
					catch (Exception e)
					{
						m_errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", statementContext.GetSpan()));
					}
				}
				m_analysisResult.AddStatement(context, statement);
				return statement;
			}

			return null;
		}
		public override VHDLStatement VisitWait_statement([NotNull] vhdlParser.Wait_statementContext context)
		{
			VHDLWaitStatement statement = new VHDLWaitStatement(m_analysisResult, m_parent);
			ExpressionVisitors.VHDLExpressionVisitor visitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
			if (context.condition_clause()?.condition()?.expression() != null)
				statement.UntilCondition = visitor.Visit(context.condition_clause().condition().expression());
			if (context.timeout_clause()?.expression() != null)
				statement.TimeoutExpression = visitor.Visit(context.timeout_clause()?.expression());

			m_analysisResult.AddStatement(context, statement);
			return statement;
		}
	}
}
