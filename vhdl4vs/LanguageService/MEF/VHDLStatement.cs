/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs
{
	class VHDLStatementUtilities
	{
		public static bool CheckExpressionType(VHDLExpression expression, VHDLType expectedType, Action<VHDLError> errorListener, EvaluationContext evaluationContext = null)
		{
			if (expression == null)
				return false;

			if (evaluationContext == null)
				evaluationContext = new EvaluationContext();

			try
			{
				VHDLEvaluatedExpression eval = expression.Evaluate(evaluationContext, expectedType);
				if (eval?.Type == null)
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Type cannot be evaluated", expression.Span));
					return false;
				}

				else if (VHDLType.AreCompatible(expectedType, eval.Type, null, eval?.Result) == VHDLCompatibilityResult.No)
				{
					errorListener?.Invoke(new VHDLError(0,
								PredefinedErrorTypeNames.SyntaxError,
								string.Format("Cannot implicitly convert type '{0}' to '{1}'",
									eval.Type?.GetClassifiedText()?.Text ?? "<error type>",
									expectedType?.GetClassifiedText()?.Text ?? "<error type>"),
								expression.Span));
					return false;
				}
			}
			catch (VHDLCodeException e)
			{
				errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, e.Message, e.Span));
				return false;
			}
			catch (Exception e)
			{
				VHDLLogger.LogException(e);
				errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", expression.Span));
				return false;
			}
			return true;
		}
	}
	internal class VHDLStatement
	{
		public VHDLStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
		{
			AnalysisResult = analysisResult;
			Parent = parent;
		}
		public AnalysisResult AnalysisResult { get; set; } = null;
		public VHDLDeclaration Parent { get; set; } = null;

		// Check correctness and return errors
		public virtual void Check(Action<VHDLError> errorListener)
		{
		}

		public virtual IEnumerable<object> Children { get { yield break; } }
	}

	struct VHDLConditionalExpression
	{
		public VHDLConditionalExpression(VHDLExpression condition, VHDLExpression value)
		{
			ConditionExpression = condition;
			ValueExpression = value;
		}
		public VHDLExpression ConditionExpression { get; set; }
		public VHDLExpression ValueExpression { get; set; }

		public IEnumerable<object> Children { get { if (ConditionExpression != null) yield return ConditionExpression; if (ValueExpression != null) yield return ValueExpression; } }
	}
	class VHDLSignalAssignmentStatement
		: VHDLStatement
	{
		public VHDLSignalAssignmentStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
		public VHDLExpression NameExpression { get; set; } = null;
		public List<VHDLConditionalExpression> Values { get; set; } = new List<VHDLConditionalExpression>();

		public override void Check(Action<VHDLError> errorListener)
		{
			if (NameExpression is VHDLReferenceExpression r)
			{
				if (r.Declaration is VHDLVariableDeclaration)
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Cannot use concurrent assignment on a variable, use ':=' instead", Values.Select(x => x.ValueExpression?.Span).Prepend(NameExpression?.Span).Aggregate((x, y) => x?.Union(y) ?? y).Value));
				else if (r?.Declaration is VHDLConstantDeclaration)
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Constants cannot be assigned", Values.Select(x => x.ValueExpression?.Span).Prepend(NameExpression?.Span).Aggregate((x, y) => x?.Union(y) ?? y).Value));
			}

			foreach (VHDLExpression cond in Values.Select(x => x.ConditionExpression).Where(x => x != null))
			{
				VHDLStatementUtilities.CheckExpressionType(cond, AnalysisResult.BooleanType, errorListener);
			}

			VHDLType type = null;
			if (NameExpression != null)
			{
				try
				{
					type = NameExpression.Evaluate(new EvaluationContext())?.Type;
					if (type == null)
						errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Type cannot be evaluated", NameExpression.Span));
				}
				catch (VHDLCodeException e)
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, e.Message, e.Span));
				}
				catch (Exception e)
				{
					VHDLLogger.LogException(e);
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", NameExpression.Span));
				}
			}
			if (type != null)
			{
				foreach (VHDLExpression expression in Values.Select(x => x.ValueExpression))
				{
					VHDLStatementUtilities.CheckExpressionType(expression, type, errorListener);
				}
			}
		}
		public override IEnumerable<object> Children { get { return Values.SelectMany(x => x.Children).Prepend(NameExpression).Where(x => x != null); } }
	}

	class VHDLVariableAssignmentStatement
		: VHDLStatement
	{
		public VHDLVariableAssignmentStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
		public VHDLVariableAssignmentStatement(AnalysisResult analysisResult, VHDLDeclaration parent, VHDLExpression nameExpression)
			: base(analysisResult, parent)
		{
			NameExpression = nameExpression;
		}
		public VHDLExpression NameExpression { get; set; } = null;
		public List<VHDLConditionalExpression> Values { get; set; } = new List<VHDLConditionalExpression>();

		public override void Check(Action<VHDLError> errorListener)
		{
			if (NameExpression is VHDLReferenceExpression r)
			{
				if (r.Declaration is VHDLSignalDeclaration)
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Cannot use variable assignment on a signal, use '<=' instead", Values.Select(x => x.ValueExpression?.Span).Prepend(NameExpression?.Span).Aggregate((x, y) => x?.Union(y) ?? y).Value));
				else if (r?.Declaration is VHDLConstantDeclaration)
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Constants cannot be assigned", Values.Select(x => x.ValueExpression?.Span).Prepend(NameExpression?.Span).Aggregate((x, y) => x?.Union(y) ?? y).Value));
			}

			foreach (VHDLExpression cond in Values.Select(x => x.ConditionExpression).Where(x => x != null))
			{
				VHDLStatementUtilities.CheckExpressionType(cond, AnalysisResult.BooleanType, errorListener);
			}

			VHDLType type = null;
			if (NameExpression != null)
			{
				try
				{
					type = NameExpression.Evaluate(new EvaluationContext())?.Type;
					if (type == null)
						errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Type cannot be evaluated", NameExpression.Span));
				}
				catch (VHDLCodeException e)
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, e.Message, e.Span));
				}
				catch (Exception e)
				{
					VHDLLogger.LogException(e);
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", NameExpression.Span));
				}
			}
			if (type != null)
			{
				foreach (VHDLExpression expression in Values.Select(x => x.ValueExpression))
				{
					VHDLStatementUtilities.CheckExpressionType(expression, type, errorListener);
				}
			}
		}
		public override IEnumerable<object> Children { get { return Values.SelectMany(x => x.Children).Prepend(NameExpression).Where(x => x != null); } }
	}
	class VHDLIfStatement
		: VHDLStatement
	{
		public VHDLIfStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
		public VHDLExpression Condition { get; set; } = null;
		public List<VHDLStatement> Statements { get; set; } = new List<VHDLStatement>();
		public List<Tuple<VHDLExpression, List<VHDLStatement>>> ElseIfStatements { get; set; } = new List<Tuple<VHDLExpression, List<VHDLStatement>>>();
		public List<VHDLStatement> ElseStatements { get; set; } = new List<VHDLStatement>();

		public override void Check(Action<VHDLError> errorListener)
		{
			foreach (VHDLExpression cond in ElseIfStatements.Select(x => x.Item1).Prepend(Condition))
			{
				VHDLStatementUtilities.CheckExpressionType(cond, AnalysisResult.BooleanType, errorListener);
			}

			/*foreach (VHDLStatement statement in ElseIfStatements.Select(x => x.Item2).Prepend(Statements).Append(ElseStatements).SelectMany(x => x))
			{
				try
				{
					statement.Check(errorListener);
				}
				catch (Exception e)
				{
				}
			}*/
		}
		public override IEnumerable<object> Children
		{
			get
			{
				return Statements.Concat(ElseStatements).Concat(ElseIfStatements.SelectMany(x => x.Item2.Prepend<object>(x.Item1))).Prepend(Condition).Where(x => x != null);
			}
		}
	}

	class VHDLWhileStatement
		: VHDLStatement
	{
		public VHDLWhileStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
		public VHDLExpression Condition { get; set; } = null;
		public List<VHDLStatement> Statements { get; set; } = new List<VHDLStatement>();

		public override void Check(Action<VHDLError> errorListener)
		{
			VHDLStatementUtilities.CheckExpressionType(Condition, AnalysisResult.BooleanType, errorListener);
			foreach (VHDLStatement statement in Statements)
			{
				try
				{
					statement.Check(errorListener);
				}
				catch (Exception e)
				{
					VHDLLogger.LogException(e);
				}
			}
		}
		public override IEnumerable<object> Children { get { return Statements.Prepend<object>(Condition).Where(x => x != null); } }
	}
	class VHDLForStatement
		: VHDLStatement
	{
		public VHDLForStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
		public string Variable { get; set; } = null;
		public VHDLRangeExpression Range { get; set; } = null;

		public List<VHDLStatement> Statements { get; set; } = new List<VHDLStatement>();

		public override void Check(Action<VHDLError> errorListener)
		{
			/*foreach (VHDLStatement statement in Statements)
			{
				try
				{
					statement.Check(errorListener);
				}
				catch (Exception e)
				{
				}
			}*/
		}
		public override IEnumerable<object> Children { get { return Statements.Where(x => x != null); } }
	}
	class VHDLReturnStatement
		: VHDLStatement
	{
		public VHDLReturnStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
		public VHDLReturnStatement(AnalysisResult analysisResult, VHDLDeclaration parent, VHDLExpression expression)
			: base(analysisResult, parent)
		{
			Expression = expression;
		}
		public VHDLExpression Expression { get; set; } = null;
		public override IEnumerable<object> Children { get { if (Expression != null) yield return Expression; } }
	}
	class VHDLBreakStatement
		: VHDLStatement
	{
		public VHDLBreakStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
	}
	class VHDLExitStatement
		: VHDLStatement
	{
		public VHDLExitStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
		public VHDLExitStatement(AnalysisResult analysisResult, VHDLDeclaration parent, string name, VHDLExpression condition)
			: base(analysisResult, parent)
		{
			Condition = condition;
		}
		// Optional name of the loop to exit (usefull in nested loops)
		public string Name { get; set; } = null;
		// If not null exit when Condition is true
		public VHDLExpression Condition { get; set; } = null;
		public override IEnumerable<object> Children { get { if (Condition != null) yield return Condition; } }
	}
	class VHDLNextStatement
		: VHDLStatement
	{
		public VHDLNextStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
		public VHDLNextStatement(AnalysisResult analysisResult, VHDLDeclaration parent, string name, VHDLExpression condition)
			: base(analysisResult, parent)
		{
			Condition = condition;
		}
		// Optional name of the loop to exit (usefull in nested loops)
		public string Name { get; set; } = null;
		// If not null exit when Condition is true
		public VHDLExpression Condition { get; set; } = null;
		public override IEnumerable<object> Children { get { if (Condition != null) yield return Condition; } }
	}
	class VHDLCaseAlternative
	{
		public List<VHDLExpression> Conditions { get; set; } = new List<VHDLExpression>();
		public List<VHDLStatement> Statements { get; set; } = new List<VHDLStatement>();
		public IEnumerable<object> Children { get { return Statements.Prepend<object>(Conditions).Where(x => x != null); } }
	}
	class VHDLCaseStatement
		: VHDLStatement
	{
		public VHDLCaseStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
		public VHDLExpression Expression { get; set; } = null;
		public List<VHDLCaseAlternative> Alternatives { get; set; } = new List<VHDLCaseAlternative>();
		public override IEnumerable<object> Children { get { return Alternatives.SelectMany(x => x.Children).Prepend(Expression).Where(x => x != null); } }
	}

	class VHDLProcedureCallStatement
		: VHDLStatement
	{
		public VHDLProcedureCallStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
		public VHDLExpression NameExpression { get; set; } = null;
		public List<VHDLExpression> Arguments { get; set; } = new List<VHDLExpression>();
		public override IEnumerable<object> Children { get { return Arguments.Prepend(NameExpression).Where(x => x != null); } }
	}

	class VHDLComponentInstanciationStatement
		: VHDLStatement
	{
		public VHDLComponentInstanciationStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
		public string Name { get; set; } = null;
		public VHDLReferenceExpression ComponentNameExpression { get; set; } = null;
		public List<VHDLExpression> Parameters { get; set; } = new List<VHDLExpression>();
		public List<VHDLExpression> Generics { get; set; } = new List<VHDLExpression>();
		public override IEnumerable<object> Children { get { return Parameters.Prepend(ComponentNameExpression).Where(x => x != null); } }

		public override void Check(Action<VHDLError> errorListener)
		{
			if (ComponentNameExpression?.Declaration == null)
				return;

			if (!(ComponentNameExpression?.Declaration is VHDLComponentDeclaration))
			{
				errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Name '{0}' is not a component", ComponentNameExpression.GetClassifiedText().Text), ComponentNameExpression.Span));
				return;
			}

			VHDLComponentDeclaration componentDecl = ComponentNameExpression.Declaration as VHDLComponentDeclaration;

			if (Parameters.Any(x => x is VHDLArgumentAssociationExpression) &&
				Parameters.Any(x => !(x is VHDLArgumentAssociationExpression)))
			{
				errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Cannot mix named association and positional port mapping", Parameters.Select(x => x.Span).Aggregate((x, y) => x.Union(y))));
			}

			if (Generics.Any(x => x is VHDLArgumentAssociationExpression) &&
				Generics.Any(x => !(x is VHDLArgumentAssociationExpression)))
			{
				errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Cannot mix named association and positional port mapping", Generics.Select(x => x.Span).Aggregate((x, y) => x.Union(y))));
			}

			EvaluationContext evaluationContext = new EvaluationContext();
			evaluationContext.Push();
			bool named = Generics.FirstOrDefault() is VHDLArgumentAssociationExpression;
			if (named)
			{
				HashSet<string> usedGenerics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var parameter in Generics.OfType<VHDLArgumentAssociationExpression>())
				{
					try
					{
						var arg = parameter.Arguments.Single();
						if (arg is VHDLNameExpression ne)
						{
							string name = ne.Name;

							// Check if generic exist in component
							VHDLGenericDeclaration componentGeneric = componentDecl.Generics.FirstOrDefault(p => string.Compare(p.Name, name, true) == 0);
							if (componentGeneric == null)
							{
								errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Generic '{0}' doesn't exist in component '{1}'", name, componentDecl.Name), arg.Span));
								continue;
							}
							VHDLType portType = componentGeneric.Type.Dereference();
							if (portType is VHDLAbstractArrayType aat)
							{
								// If this the port is an array, we add all the elements
								if (aat.Dimension != 1)
								{
									errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, "Array with dimension != 1 not supported", arg.Span));
									continue;
								}
								VHDLRange r = aat.GetIndexRange(0);
								long start;
								long end;
								if (r?.TryGetIntegerRange(out start, out end) != true)
								{
									errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, "Expression cannot be evaluated", arg.Span));
									continue;
								}
								for (long i = start; i <= end; ++i)
								{
									string n = name + "(" + i.ToString() + ")";
									if (!usedGenerics.Add(n))
									{
										errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Generic already associated '{0}'", n), arg.Span));
										continue;
									}
								}
							}
							else
							{
								if (!usedGenerics.Add(name))
								{
									errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Generic already associated '{0}'", name), arg.Span));
									continue;
								}
							}
							// Check types are same
							VHDLStatementUtilities.CheckExpressionType(parameter.Value, componentGeneric.Type, errorListener);
							evaluationContext[componentGeneric] = parameter.Value.Evaluate(evaluationContext, componentGeneric.Type);
						}
						else
						{
							errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Expected name", arg.Span));
							continue;
						}

					}
					catch (Exception e)
					{
						VHDLLogger.LogException(e);
					}
				}

				// Get list of generics that are not assigned
				List<string> missingGenerics = new List<string>();
				foreach (var p in componentDecl.Generics)
				{
					if (usedGenerics.Contains(p.Name) || p.InitializationExpression != null)
						continue;

					missingGenerics.Add(p.Name);
				}
				if (missingGenerics.Any())
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, string.Format("Missing generics {0} from entity '{1}'", string.Join(", ", missingGenerics.Select(x => "'" + x + "'")), componentDecl.Name), ComponentNameExpression.Span));
			}
			else
			{
				foreach (var (p1, p2) in Generics.Zip(componentDecl.Generics, (x, y) => Tuple.Create(x, y)))
				{
					VHDLType t2 = p2.Type;
					VHDLStatementUtilities.CheckExpressionType(p1, t2, errorListener);
					evaluationContext[p2] = p1.Evaluate(evaluationContext, t2);
				}

				var missingGenerics = componentDecl.Generics.Skip(Generics.Count).Where(x => x.InitializationExpression == null);
				if (missingGenerics.Any())
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Missing generics {0} from entity '{1}'", string.Join(", ", missingGenerics.Select(x => "'" + x.Name + "'")), componentDecl.Name), ComponentNameExpression.Span));
				}
				else if (Generics.Count > componentDecl.Generics.Count())
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Component '{0}' has {1} generics, {2} given", componentDecl.Name, componentDecl.Generics.Count(), Generics.Count), ComponentNameExpression.Span));
				}
			}

			named = Parameters.FirstOrDefault() is VHDLArgumentAssociationExpression;
			if (named)
			{
				HashSet<string> usedPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var parameter in Parameters.OfType<VHDLArgumentAssociationExpression>())
				{
					try
					{
						var arg = parameter.Arguments.Single();
						if (arg is VHDLNameExpression ne)
						{
							string name = ne.Name;

							// Check if port exist in component
							VHDLPortDeclaration componentPort = componentDecl.Ports.FirstOrDefault(p => string.Compare(p.Name, name, true) == 0);
							if (componentPort == null)
							{
								errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Port '{0}' doesn't exist in component '{1}'", name, componentDecl.Name), arg.Span));
								continue;
							}
							VHDLType portType = componentPort.Type.Dereference();
							if (portType is VHDLAbstractArrayType aat)
							{
								// If this the port is an array, we add all the elements
								if (aat.Dimension != 1)
								{
									errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, "Array with dimension != 1 not supported", arg.Span));
									continue;
								}
								VHDLRange r = aat.GetIndexRange(0);
								long start;
								long end;
								if (r?.TryGetIntegerRange(out start, out end) != true)
								{
									errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, "Expression cannot be evaluated", arg.Span));
									continue;
								}
								for (long i = start; i <= end; ++i)
								{
									string n = name + "(" + i.ToString() + ")";
									if (!usedPorts.Add(n))
									{
										errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Port already associated '{0}'", n), arg.Span));
										continue;
									}
								}
							}
							else
							{
								if (!usedPorts.Add(name))
								{
									errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Port already associated '{0}'", name), arg.Span));
									continue;
								}
							}
							// Check types are same
							VHDLStatementUtilities.CheckExpressionType(parameter.Value, componentPort.Type, errorListener, evaluationContext);

						}
						else if (arg is VHDLFunctionCallOrIndexExpression fce)
						{
							if (!(fce.NameExpression is VHDLNameExpression))
							{
								errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Expected name expression", fce.NameExpression.Span));
								continue;
							}

							string name = (fce.NameExpression as VHDLNameExpression)?.Name;
							VHDLPortDeclaration componentPort = componentDecl.Ports.FirstOrDefault(p => string.Compare(p.Name, name, true) == 0);
							if (componentPort == null)
							{
								errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Port '{0}' doesn't exist in component '{1}'", name, componentDecl.Name), arg.Span));
								continue;
							}
							VHDLRange r = (fce.Arguments.First() as VHDLRangeExpression)?.Range;
							VHDLEvaluatedExpression estart = (r?.Start ?? fce.Arguments.First())?.Evaluate(evaluationContext);
							VHDLEvaluatedExpression eend = (r?.End ?? fce.Arguments.First())?.Evaluate(evaluationContext);
							long? iStart = r?.Direction == VHDLRangeDirection.To ? (estart.Result as VHDLIntegerValue)?.Value : (eend.Result as VHDLIntegerValue)?.Value;
							long? iEnd = r?.Direction == VHDLRangeDirection.To ? (eend.Result as VHDLIntegerValue)?.Value : (estart.Result as VHDLIntegerValue)?.Value;
							if (iStart == null || iEnd == null)
							{
								errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Expression cannot be evaluated", arg.Span));
								continue;
							}
							for (long i = iStart.Value; i <= iEnd.Value; ++i)
							{
								string n = name + "(" + i.ToString() + ")";
								if (!usedPorts.Add(n))
								{
									errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Port already associated '{0}'", n), arg.Span));
									continue;
								}
							}
							VHDLType argType = null;
							try
							{
								argType = arg.Evaluate(evaluationContext)?.Type;
							}
							catch (VHDLCodeException ce)
							{
								errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, ce.Message, arg.Span));
							}
							catch (Exception e)
							{
								VHDLLogger.LogException(e);
								continue;
							}
							VHDLStatementUtilities.CheckExpressionType(parameter.Value, argType, errorListener, evaluationContext);
						}
						else
						{
							errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Expected name or index expression", arg.Span));
							continue;
						}

					}
					catch (Exception e)
					{
						VHDLLogger.LogException(e);
					}
				}

				// Get list of ports that are not assigned
				List<string> missingOutPorts = new List<string>();
				List<string> missingInPorts = new List<string>();
				List<string> missingInoutPorts = new List<string>();
				foreach (var p in componentDecl.Ports)
				{
					if (p.Type is VHDLAbstractArrayType aat)
					{
						if (aat.Dimension != 1)
							continue;
						VHDLRange r = aat.GetIndexRange(0);
						VHDLEvaluatedExpression estart = r.Start.Evaluate(evaluationContext);
						VHDLEvaluatedExpression eend = r.End.Evaluate(evaluationContext);
						long? iStart = r.Direction == VHDLRangeDirection.To ? (estart.Result as VHDLIntegerValue)?.Value : (eend.Result as VHDLIntegerValue)?.Value;
						long? iEnd = r.Direction == VHDLRangeDirection.To ? (eend.Result as VHDLIntegerValue)?.Value : (estart.Result as VHDLIntegerValue)?.Value;
						if (iStart == null || iEnd == null)
							continue;

						var m = Enumerable.Range((int)iStart.Value, (int)iEnd.Value - (int)iStart.Value + 1).Select(i => p.Name + "(" + i + ")").Where(x => !usedPorts.Contains(x)).Take(2);
						if (p.Mode == VHDLSignalMode.Out)
							missingOutPorts.AddRange(m);
						else if (p.Mode == VHDLSignalMode.In)
							missingInPorts.AddRange(m);
						else if (p.Mode == VHDLSignalMode.Inout)
							missingInoutPorts.AddRange(m);
					}
					else
					{
						if (usedPorts.Contains(p.Name))
							continue;

						if (p.Mode == VHDLSignalMode.Out)
							missingOutPorts.Add(p.Name);
						else if (p.Mode == VHDLSignalMode.In)
							missingInPorts.Add(p.Name);
						else if (p.Mode == VHDLSignalMode.Inout)
							missingInoutPorts.Add(p.Name);
					}
				}
				if (missingOutPorts.Any())
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, string.Format("Missing out ports {0} from entity '{1}'", string.Join(", ", missingOutPorts.Select(x => "'" + x + "'")), componentDecl.Name), ComponentNameExpression.Span));
				if (missingInPorts.Any())
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Missing in ports {0} from entity '{1}'", string.Join(", ", missingInPorts.Select(x => "'" + x + "'")), componentDecl.Name), ComponentNameExpression.Span));
				if (missingInoutPorts.Any())
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Missing inout ports {0} from entity '{1}'", string.Join(", ", missingInoutPorts.Select(x => "'" + x + "'")), componentDecl.Name), ComponentNameExpression.Span));

			}
			else
			{
				foreach (var (p1, p2) in Parameters.Zip(componentDecl.Ports, (x, y) => Tuple.Create(x, y)))
				{
					VHDLType t2 = p2.Type;
					VHDLStatementUtilities.CheckExpressionType(p1, t2, errorListener, evaluationContext);
				}

				if (Parameters.Count < componentDecl.Ports.Count())
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Missing ports {0} from entity '{1}'", string.Join(", ", componentDecl.Ports.Skip(Parameters.Count).Select(x => "'" + x.Name + "'")), componentDecl.Name), ComponentNameExpression.Span));
				}
				else if (Parameters.Count > componentDecl.Ports.Count())
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Component '{0}' has {1} ports, {2} given", componentDecl.Name, componentDecl.Ports.Count(), Parameters.Count), ComponentNameExpression.Span));
				}
			}
		}

		public void ResolvePortName(IVHDLToResolve overriden, DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
			if (ComponentNameExpression?.Declaration == null)
				return;

			if (overriden is VHDLNameExpression n)
			{
				n.Declaration = VHDLDeclarationUtilities.GetMemberDeclaration(ComponentNameExpression.Declaration, n.Name);
				if (n.Declaration != null)
					deepAnalysisResult.SortedReferences.Add(n.Span.Start,
						new VHDLNameReference(
							Name,
							n.Span,
							n.Declaration));
			}
			else
			{
				overriden.Resolve(deepAnalysisResult, errorListener);
			}
		}
		public void ResolveGenericName(IVHDLToResolve overriden, DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
			if (ComponentNameExpression?.Declaration == null)
				return;

			if (overriden is VHDLNameExpression n)
			{
				var decl = VHDLDeclarationUtilities.GetMemberDeclaration(ComponentNameExpression.Declaration, n.Name);
				if (decl is VHDLGenericDeclaration gendecl)
					n.Declaration = gendecl;

				if (n.Declaration != null)
					deepAnalysisResult.SortedReferences.Add(n.Span.Start,
						new VHDLNameReference(
							Name,
							n.Span,
							n.Declaration));
			}
			else
			{
				overriden.Resolve(deepAnalysisResult, errorListener);
			}
		}
	}
	class VHDLWaitStatement
		: VHDLStatement
	{
		public VHDLWaitStatement(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
		public string Name { get; set; } = null;
		public VHDLExpression UntilCondition { get; set; } = null;
		public VHDLExpression TimeoutExpression { get; set; } = null;

		public override void Check(Action<VHDLError> errorListener)
		{
			if (UntilCondition != null)
				VHDLStatementUtilities.CheckExpressionType(UntilCondition, AnalysisResult.BooleanType, errorListener);
			if (TimeoutExpression != null)
				VHDLStatementUtilities.CheckExpressionType(TimeoutExpression, AnalysisResult.TimeType, errorListener);
		}
		public override IEnumerable<object> Children { get { if (UntilCondition != null) yield return UntilCondition; if (TimeoutExpression != null) yield return TimeoutExpression; } }
	}

	// Not really a statement but that will do
	class VHDLUseClause
		: VHDLStatement
	{
		public VHDLUseClause(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}
		public override void Check(Action<VHDLError> errorListener)
		{
			List<string> parts = new List<string>();
			VHDLExpression e = Name;
			while(e != null)
			{
				if (e is VHDLMemberSelectExpression mse)
				{
					parts.Insert(0, mse.Name);
					e = mse.Expression;
				}
				else if (e is VHDLNameExpression ne)
				{
					parts.Insert(0, ne.Name);
					break;
				}
				else
					break;
			}
			if (parts.Count != 3)
			{
				errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Invalid library format", Name.Span));
				return;
			}

			string libraryName = parts[0];
			string packageName = parts[1];
			string entityName = parts[2];

			VHDLDeclaration decl = null;

			if (string.Compare(libraryName, "work", true) == 0)
			{
				foreach (AnalysisResult ar in AnalysisResult.Document.DocumentTable.EnumerateSiblings(AnalysisResult.Document).Select(x => x.Parser?.AResult))
				{
					if (ar == null)
						continue;

					if (ar.Declarations.TryGetValue(packageName + "@declaration", out var packageDecl) && packageDecl is VHDLPackageDeclaration)
					{
						decl = packageDecl;
						break;
					}
				}
				if (decl == null)
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Name not found '{0}'", packageName), Name.Span));
					return;
				}
			}
			else
			{
				decl = AnalysisResult?.Document?.Project?.GetLibrary(libraryName);
				if (decl == null)
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Library not found '{0}'", libraryName), Name.Span));
					return;
				}

				decl = VHDLDeclarationUtilities.GetMemberDeclaration(decl, packageName);
				if (decl == null)
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Name not found '{0}'", packageName), Name.Span));
					return;
				}
			}


			if (string.Compare(entityName, "all", true) == 0)
				return;

			decl = VHDLDeclarationUtilities.GetMemberDeclaration(decl, entityName);
			if (decl == null)
			{
				errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Name not found '{0}'", entityName), Name.Span));
				return;
			}
		}

		public VHDLExpression Name { get; set; } = null;
	}
}
