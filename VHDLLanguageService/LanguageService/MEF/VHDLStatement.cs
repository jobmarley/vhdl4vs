using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompany.LanguageServices.VHDL
{
	class VHDLStatementUtilities
	{
		public static void CheckExpressionType(VHDLExpression expression, VHDLType expectedType, Action<VHDLError> errorListener)
		{
			if (expression != null)
			{
				try
				{
					VHDLEvaluatedExpression eval = expression.Evaluate();
					if (eval?.Type == null)
						errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Type cannot be evaluated", expression.Span));

					if (expectedType.IsCompatible(eval.Type) == VHDLCompatibilityResult.No)
						errorListener?.Invoke(new VHDLError(0,
									PredefinedErrorTypeNames.SyntaxError,
									string.Format("Cannot implicitly convert type '{0}' to '{1}'",
										eval.Type?.GetClassifiedText()?.Text ?? "<error type>",
										expectedType?.GetClassifiedText()?.Text ?? "<error type>"),
									expression.Span));
				}
				catch (VHDLCodeException e)
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, e.Message, e.Span));
				}
				catch (Exception e)
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Internal error", expression.Span));
				}
			}
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
					type = NameExpression.Evaluate()?.Type;
					if (type == null)
						errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Type cannot be evaluated", NameExpression.Span));
				}
				catch (VHDLCodeException e)
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, e.Message, e.Span));
				}
				catch (Exception e)
				{
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
					type = NameExpression.Evaluate()?.Type;
					if (type == null)
						errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Type cannot be evaluated", NameExpression.Span));
				}
				catch (VHDLCodeException e)
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, e.Message, e.Span));
				}
				catch (Exception e)
				{
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
				return;
			}

			if (!Parameters.Any())
			{
				if (componentDecl.Ports.Any())
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Missing ports {0} from entity '{1}'", string.Join(", ", componentDecl.Ports.Select(x => "'" + x.Name + "'")), componentDecl.Name), ComponentNameExpression.Span));
				}
				return;
			}

			bool named = Parameters.First() is VHDLArgumentAssociationExpression;
			if (named)
			{
				HashSet<string> usedPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var parameter in Parameters.OfType<VHDLArgumentAssociationExpression>())
				{
					try
					{
						// Check if name is a name expression (could be a slice, but too complicated for now)
						if (!(parameter.Argument is VHDLNameExpression))
						{
							errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Expected name expression", parameter.Argument.Span));
							continue;
						}

						// Check if port was already used in this instantiation
						string name = (parameter.Argument as VHDLNameExpression).Name;
						if (usedPorts.Contains(name))
						{
							errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Port already associated '{0}'", name), parameter.Argument.Span));
							continue;
						}
						usedPorts.Add(name);

						// Check if port exist in component
						VHDLPortDeclaration componentPort = componentDecl.Ports.FirstOrDefault(p => string.Compare(p.Name, name, true) == 0);
						if (componentPort == null)
						{
							errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Port '{0}' doesn't exist in component '{1}'", name, componentDecl.Name), parameter.Argument.Span));
							continue;
						}

						// Check types are same
						VHDLStatementUtilities.CheckExpressionType(parameter.Value, componentPort.Type, errorListener);
					}
					catch (Exception ex)
					{

					}
				}

				var missingPorts = componentDecl.Ports.Where(p => !usedPorts.Contains(p.Name));
				if (missingPorts.Any(x => x.Mode == VHDLSignalMode.Out))
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, string.Format("Missing out ports {0} from entity '{1}'", string.Join(", ", missingPorts.Select(x => "'" + x.Name + "'")), componentDecl.Name), ComponentNameExpression.Span));
				if (missingPorts.Any(x => x.Mode == VHDLSignalMode.In))
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Missing in ports {0} from entity '{1}'", string.Join(", ", missingPorts.Select(x => "'" + x.Name + "'")), componentDecl.Name), ComponentNameExpression.Span));
				if (missingPorts.Any(x => x.Mode == VHDLSignalMode.Inout))
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Missing inout ports {0} from entity '{1}'", string.Join(", ", missingPorts.Select(x => "'" + x.Name + "'")), componentDecl.Name), ComponentNameExpression.Span));

			}
			else
			{
				foreach (var (p1, p2) in Parameters.Zip(componentDecl.Ports, (x, y) => Tuple.Create(x, y)))
				{
					VHDLType t2 = p2.Type;
					VHDLStatementUtilities.CheckExpressionType(p1, t2, errorListener);
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
