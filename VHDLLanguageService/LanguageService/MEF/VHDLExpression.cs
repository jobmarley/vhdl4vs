using Antlr4.Runtime;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompany.LanguageServices.VHDL
{
	class VHDLCodeException
		: Exception
	{
		public VHDLCodeException(string message, Span span)
			: base(message)
		{
			Span = span;
		}
		public Span Span { get; set; }
	}
	class VHDLTypeEvalationException
		: VHDLCodeException
	{
		public VHDLTypeEvalationException(string message, Span span)
			: base(message, span)
		{ }
	}
	enum VHDLComparisonResult
	{
		Equal,
		Different,
		Unsure,
	}

	class VHDLEvaluatedExpression
	{
		public VHDLEvaluatedExpression(VHDLType t, VHDLExpression e, VHDLLiteral l)
		{
			Type = t;
			Expression = e;
			Result = l;
		}
		// The evaluated type of the expression
		public VHDLType Type { get; }
		// The expression that evaluated to that
		public VHDLExpression Expression { get; }
		// Can be a constant, or null if the expression cannot be evaluated
		public VHDLLiteral Result { get; }
	}
	abstract class VHDLExpression
	{
		public AnalysisResult AnalysisResult { get; set; } = null;
		public Span Span { get; set; }
		public VHDLExpression(AnalysisResult analysisResult, Span span)
		{
			AnalysisResult = analysisResult;
			Span = span;
		}

		public virtual VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText();
		}

		// Calculate the result of the expression. If the result can be calculated it is contained in the result as a literal
		// ExpectedType is required because in vhdl, function overloading can be done based on return type
		public abstract VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null);

		public virtual IEnumerable<VHDLExpression> Children { get { return Array.Empty<VHDLExpression>(); } }
	}

	abstract class VHDLReferenceExpression
		: VHDLExpression,
		IVHDLToResolve
	{
		protected VHDLReferenceExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span)
		{
		}

		public VHDLDeclaration Declaration { get; set; } = null;

		public virtual void Resolve(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
		}

		public abstract IEnumerable<VHDLDeclaration> GetDeclarations();
	}
	abstract class VHDLLiteral
		: VHDLExpression
	{
		public VHDLLiteral(AnalysisResult analysisResult, Span span, string text)
			: base(analysisResult, span)
		{
			Text = text;
		}

		public string Text { get; set; } = null;
	}

	class VHDLBooleanLiteral
		: VHDLLiteral
	{
		public VHDLBooleanLiteral(bool value)
			: base(null, new Span(), null)
		{
			Value = value;
		}
		public VHDLBooleanLiteral(AnalysisResult analysisResult, Span span, bool value)
			: base(analysisResult, span, "")
		{
			Value = value;
		}
		public bool Value { get; set; }

		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText(Text ?? Value.ToString());
		}

		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, this);
		}
	}
	class VHDLRealLiteral
		: VHDLLiteral
	{
		public VHDLRealLiteral(double value)
			: base(null, new Span(), null)
		{
			Value = value;
		}
		public VHDLRealLiteral(AnalysisResult analysisResult, Span span, double value, string text)
			: base(analysisResult, span, text)
		{
			Value = value;
		}
		public double Value { get; set; }

		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText(Text ?? Value.ToString());
		}

		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return new VHDLEvaluatedExpression(VHDLBuiltinTypeReal.Instance, this, this);
		}
	}
	class VHDLIntegerLiteral
		: VHDLLiteral
	{
		public VHDLIntegerLiteral(long l)
			: base(null, new Span(), null)
		{
			Value = l;
		}
		public VHDLIntegerLiteral(AnalysisResult analysisResult, Span span, long value, string text)
			: base(analysisResult, span, text)
		{
			Value = value;
		}
		public long Value { get; set; }

		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText(Text ?? Value.ToString());
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, this);
		}
	}
	class VHDLNull
		: VHDLLiteral
	{
		public VHDLNull(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span, "null") { }

		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText("null");
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return new VHDLEvaluatedExpression(VHDLBuiltinTypeNull.Instance, this, this);
		}
	}
	class VHDLCharacterLiteral
		: VHDLLiteral
	{
		//public VHDLCharacterLiteral(AnalysisResult analysisResult, Span span)
		//	: base(analysisResult, span) { }
		public VHDLCharacterLiteral(AnalysisResult analysisResult, Span span, char c)
			: base(analysisResult, span, "'" + c.ToString() + "'")
		{
			Value = c;
		}
		public char Value { get; set; }
		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText("'" + Value + "'", "string");
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return new VHDLEvaluatedExpression(new VHDLCharLiteralType(this), this, this);
		}
	}
	class VHDLStringLiteral
		: VHDLLiteral
	{
		//public VHDLStringLiteral(AnalysisResult analysisResult, Span span)
		//	: base(analysisResult, span) { }
		public VHDLStringLiteral(AnalysisResult analysisResult, Span span, string s)
			: base(analysisResult, span, '"' + s + '"')
		{
			Value = s;
		}
		public string Value { get; set; }
		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText('"' + Value + '"', "string");
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return new VHDLEvaluatedExpression(new VHDLStringLiteralType(this), this, this);
		}
	}
	class VHDLBinaryStringLiteral
		: VHDLLiteral
	{
		//public VHDLBinaryStringLiteral(AnalysisResult analysisResult, Span span)
		//	: base(analysisResult, span) { }
		public VHDLBinaryStringLiteral(AnalysisResult analysisResult, Span span, string s)
			: base(analysisResult, span, "b\"" + s + '"')
		{
			Value = s;
		}
		public string Value { get; set; }
		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText("b\"" + Value + '"', "string");
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return new VHDLEvaluatedExpression(new VHDLStringLiteralType(this), this, this);
		}
	}

	class VHDLOctalStringLiteral
		: VHDLLiteral
	{
		//public VHDLOctalStringLiteral(AnalysisResult analysisResult, Span span)
		//	: base(analysisResult, span) { }
		public VHDLOctalStringLiteral(AnalysisResult analysisResult, Span span, string s)
			: base(analysisResult, span, "o\"" + s + "'")
		{
			Value = s;
		}
		public string Value { get; set; }
		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText("o\"" + Value + '"', "string");
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return new VHDLEvaluatedExpression(new VHDLStringLiteralType(this), this, this);
		}
		public VHDLStringLiteral ToStringLiteral()
		{
			string[] table = new string[]
			{
					"000",
					"001",
					"010",
					"011",
					"100",
					"101",
					"110",
					"111",
			};
			StringBuilder sb = new StringBuilder();
			foreach (char c in Value.Select(c => char.ToLower(c)))
			{
				if (c >= '0' && c <= '7')
					sb.Append(table[c - '0']);
				else
					return null;
			}
			return new VHDLStringLiteral(null, new Span(), sb.ToString());
		}
	}
	class VHDLHexStringLiteral
		: VHDLLiteral
	{
		//public VHDLHexStringLiteral(AnalysisResult analysisResult, Span span)
		//	: base(analysisResult, span) { }
		public VHDLHexStringLiteral(AnalysisResult analysisResult, Span span, string s)
			: base(analysisResult, span, "x\"" + s + '"')
		{
			Value = s;
		}
		public string Value { get; set; }
		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText("x\"" + Value + '"', "string");
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return new VHDLEvaluatedExpression(new VHDLStringLiteralType(this), this, this);
		}
		public VHDLStringLiteral ToStringLiteral()
		{
			string[] table = new string[]
			{
				"0000",
				"0001",
				"0010",
				"0011",
				"0100",
				"0101",
				"0110",
				"0111",
				"1000",
				"1001",
				"1010",
				"1011",
				"1100",
				"1101",
				"1110",
				"1111",
			};
			StringBuilder sb = new StringBuilder();
			foreach (char c in Value.Select(c => char.ToLower(c)))
			{
				if (c >= '0' && c <= '9')
					sb.Append(table[c - '0']);
				else if (c >= 'a' && c <= 'f')
					sb.Append(table[c - 'a' + 10]);
				else
					return null;
			}
			return new VHDLStringLiteral(null, new Span(), sb.ToString());
		}
	}
	class VHDLPhysicalLiteral
		: VHDLExpression
	{
		//public VHDLHexStringLiteral(AnalysisResult analysisResult, Span span)
		//	: base(analysisResult, span) { }
		public VHDLPhysicalLiteral(AnalysisResult analysisResult, Span span, VHDLLiteral l, string unit)
			: base(analysisResult, span)
		{
			Literal = l;
			Unit = unit;
		}

		public string Unit { get; set; }
		public VHDLLiteral Literal { get; set; }
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Literal.GetClassifiedText());
			text.Add(Unit);
			return text;
		}

		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Literal };

		VHDLType GetUnitType()
		{
			var match = VHDLDeclarationUtilities.Find(VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start),
				x =>
				{
					if (x is VHDLTypeDeclaration decl && decl.Type?.GetBaseType() is VHDLScalarType scalarType)
					{
						if (scalarType.Units.Contains(Unit))
							return true;
					}
					if (x is VHDLSubTypeDeclaration subtypedecl && subtypedecl.Type?.GetBaseType() is VHDLScalarType subtypescalarType)
					{
						if (subtypescalarType.Units.Contains(Unit))
							return true;
					}
					return false;
				});

			if (match is VHDLTypeDeclaration d)
				return d.Type;
			if (match is VHDLSubTypeDeclaration subtyped)
				return subtyped.Type;
			return null;
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return new VHDLEvaluatedExpression(GetUnitType(), this, null);
		}
	}

	class VHDLUnaryPlusExpression
		: VHDLExpression
	{
		public VHDLUnaryPlusExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLUnaryPlusExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr)
			: base(analysisResult, span)
		{
			Expression = expr;
		}
		public VHDLExpression Expression { get; set; } = null;

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add("+");
			text.Add(Expression.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e = Expression.Evaluate(evaluationContext, expectedType);
			if (e.Result is VHDLIntegerLiteral il)
				return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, new VHDLIntegerLiteral(e.Result.AnalysisResult, new Span(), il.Value, null));
			if (e.Result is VHDLRealLiteral rl)
				return new VHDLEvaluatedExpression(VHDLBuiltinTypeReal.Instance, null, new VHDLRealLiteral(e.Result.AnalysisResult, new Span(), rl.Value, null));

			// Look for an operator function with arguments of the correct type
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"+\"").OfType<VHDLFunctionDeclaration>();

			// filter by return type
			if (expectedType != null)
				operatorDeclarations = operatorDeclarations.Where(x => x.ReturnType.IsCompatible(expectedType) != VHDLCompatibilityResult.No);

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator '+' cannot be applied to operands of type '{0}'",
					e.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLUnaryMinusExpression
		: VHDLExpression
	{
		public VHDLUnaryMinusExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLUnaryMinusExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr)
			: base(analysisResult, span)
		{
			Expression = expr;
		}
		public VHDLExpression Expression { get; set; } = null;

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add("-");
			text.Add(Expression.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e = Expression.Evaluate(evaluationContext, expectedType);
			if (e.Result is VHDLIntegerLiteral il)
				return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, new VHDLIntegerLiteral(e.Result.AnalysisResult, new Span(), -il.Value, null));
			if (e.Result is VHDLRealLiteral rl)
				return new VHDLEvaluatedExpression(VHDLBuiltinTypeReal.Instance, null, new VHDLRealLiteral(e.Result.AnalysisResult, new Span(), -rl.Value, null));

			// Look for an operator function with arguments of the correct type
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"-\"").OfType<VHDLFunctionDeclaration>();

			// filter by return type
			if (expectedType != null)
				operatorDeclarations = operatorDeclarations.Where(x => x.ReturnType.IsCompatible(expectedType) != VHDLCompatibilityResult.No);

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator '-' cannot be applied to operands of type '{0}'",
					e.Type.GetClassifiedText().Text),
				Span);
		}
	}

	class VHDLConcatenateExpression
		: VHDLExpression
	{
		public VHDLConcatenateExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLConcatenateExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" & ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}

		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };


		string ToBitString(VHDLLiteral l)
		{
			return (l as VHDLStringLiteral)?.Value ??
				(l as VHDLHexStringLiteral)?.ToStringLiteral()?.Value ??
				(l as VHDLOctalStringLiteral)?.ToStringLiteral()?.Value ??
				(l as VHDLBinaryStringLiteral)?.Value;
		}
		VHDLEvaluatedExpression ConcatStringArray(VHDLLiteral left, VHDLAbstractArrayType rightType, VHDLLiteral right, bool invert, EvaluationContext evaluationContext)
		{
			// "010" & arr
			string s = ToBitString(left);
			if (s == null)
				return null;

			// check if elements are compatibles eg. <"abc" & std_logic_vector> is invalid
			if (s.Any(c => rightType.ElementType.IsCompatible(new VHDLCharLiteralType(new VHDLCharacterLiteral(null, new Span(), c))) == VHDLCompatibilityResult.No))
				return null;

			// Try to evaluate the size of the concatenation operation
			VHDLExpression count = rightType.GetIndexRange(0)?.Count(rightType.GetIndexType(0));
			if (count != null)
				count = VHDLAddExpression.AddConstant(count, s.Length);
			VHDLEvaluatedExpression eval = count?.Evaluate(evaluationContext);
			if (eval?.Result is VHDLIntegerLiteral il)
			{
				VHDLArraySliceType sliceType = new VHDLArraySliceType(rightType.GetBaseType(), new VHDLRange(new VHDLIntegerLiteral(il.Value - 1), VHDLRangeDirection.DownTo, new VHDLIntegerLiteral(null, new Span(), 0, null)));
				return new VHDLEvaluatedExpression(sliceType, this, null); // should return literal if thats possible
			}
			else
			{
				// cannot evaluate range, just return a slice without range
				VHDLArraySliceType sliceType = new VHDLArraySliceType(rightType.GetBaseType(), null);
				return new VHDLEvaluatedExpression(sliceType, this, null);
			}
		}
		VHDLEvaluatedExpression ConcatStringString(VHDLLiteral left, VHDLLiteral right)
		{
			// "010" & "101"
			string s1 = ToBitString(left);
			string s2 = ToBitString(right);
			if (s1 == null || s2 == null)
				return null;

			VHDLStringLiteral l = new VHDLStringLiteral(null, new Span(), s1 + s2);
			return new VHDLEvaluatedExpression(new VHDLStringLiteralType(l), this, l);
		}
		VHDLEvaluatedExpression ConcatCharString(VHDLCharacterLiteral left, VHDLLiteral right, bool invert)
		{
			// '0' & "101"
			string s = ToBitString(right);
			if (s == null)
				return null;

			VHDLStringLiteral l = invert ? new VHDLStringLiteral(null, new Span(), left.Value + s) : new VHDLStringLiteral(null, new Span(), s + left.Value);
			return new VHDLEvaluatedExpression(new VHDLStringLiteralType(l), this, l);
		}
		VHDLEvaluatedExpression ConcatCharChar(VHDLCharacterLiteral left, VHDLCharacterLiteral right)
		{
			// '0' & '1'
			VHDLStringLiteral l = new VHDLStringLiteral(null, new Span(), left.Value.ToString() + right.Value);
			return new VHDLEvaluatedExpression(new VHDLStringLiteralType(l), this, l);
		}
		VHDLEvaluatedExpression ConcatArrayElement(VHDLEvaluatedExpression left, VHDLEvaluatedExpression right, bool invert, EvaluationContext evaluationContext)
		{
			VHDLAbstractArrayType aat = left.Type.Dereference() as VHDLAbstractArrayType;
			if (aat.ElementType.IsCompatible(right.Type) == VHDLCompatibilityResult.No)
				return null;

			// Try to evaluate the size of the concatenation operation
			VHDLExpression count = aat.GetIndexRange(0)?.Count(aat.GetIndexType(0));
			count = VHDLAddExpression.AddConstant(count, 1);
			VHDLEvaluatedExpression eval = count?.Evaluate(evaluationContext);
			if (eval?.Result is VHDLIntegerLiteral il)
			{
				VHDLArraySliceType sliceType = new VHDLArraySliceType(aat.GetBaseType(), new VHDLRange(new VHDLIntegerLiteral(il.Value - 1), VHDLRangeDirection.DownTo, new VHDLIntegerLiteral(null, new Span(), 0, null)));
				return new VHDLEvaluatedExpression(sliceType, this, null); // should return literal if thats possible
			}
			else
			{
				// cannot evaluate range, just return a slice without range
				VHDLArraySliceType sliceType = new VHDLArraySliceType(aat.GetBaseType(), null);
				return new VHDLEvaluatedExpression(sliceType, this, null);
			}
		}
		VHDLEvaluatedExpression ConcatArrayArray(VHDLAbstractArrayType leftType, VHDLLiteral left, VHDLAbstractArrayType rightType, VHDLLiteral right, EvaluationContext evaluationContext)
		{
			// '0' & arr

			// arrays must be of the same base type
			if (leftType.GetBaseType() != rightType.GetBaseType())
				return null;

			// Try to evaluate the size of the concatenation operation
			VHDLExpression count1 = leftType.GetIndexRange(0)?.Count(leftType.GetIndexType(0));
			VHDLExpression count2 = rightType.GetIndexRange(0)?.Count(rightType.GetIndexType(0));
			VHDLExpression count = new VHDLAddExpression(null, new Span(), count1, count2);
			VHDLEvaluatedExpression eval = count?.Evaluate(evaluationContext);
			if (eval?.Result is VHDLIntegerLiteral il)
			{
				VHDLArraySliceType sliceType = new VHDLArraySliceType(rightType.GetBaseType(), new VHDLRange(new VHDLIntegerLiteral(il.Value - 1), VHDLRangeDirection.DownTo, new VHDLIntegerLiteral(null, new Span(), 0, null)));
				return new VHDLEvaluatedExpression(sliceType, this, null); // should return literal if thats possible
			}
			else
			{
				// cannot evaluate range, just return a slice without range
				VHDLArraySliceType sliceType = new VHDLArraySliceType(rightType.GetBaseType(), null);
				return new VHDLEvaluatedExpression(sliceType, this, null);
			}
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			// Can concatenate only if e1 and e2 are the same array type
			// or if e1 is array type and e2 is string/bit literal
			// If e1 is type t1 : <array of std_logic> and e2 is type t2 : <array of std_logic>, <e1 & e2> is not valid, nor is <e1 <= e2(x downto y) & "0...">
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);

			VHDLType t1 = e1?.Type?.Dereference();
			VHDLType t2 = e2?.Type?.Dereference();
			if (t1 == null || t2 == null)
				return null;

			if (t1 is VHDLStringLiteralType slt)
			{
				if (t2 is VHDLCharLiteralType clt)
				{
					// "010" & '0'
					return ConcatCharString(clt.Literal, slt.Literal, true);
				}
				else if (t2 is VHDLStringLiteralType slt2)
				{
					// "010" & "101"
					return ConcatStringString(slt.Literal, slt2.Literal);
				}
				else if (t2 is VHDLAbstractArrayType aat && aat.IndexTypes.Count() == 1) // dont support multidim arrays
				{
					// "010" & arr
					return ConcatStringArray(slt.Literal, aat, e2.Result, false, evaluationContext);
				}
			}
			else if (t1 is VHDLCharLiteralType clt)
			{
				if (t2 is VHDLCharLiteralType clt2)
				{
					// "010" & '0'
					return ConcatCharChar(clt.Literal, clt2.Literal);
				}
				else if (t2 is VHDLStringLiteralType slt2)
				{
					return ConcatCharString(clt.Literal, slt2.Literal, false);
				}
				else if (t2 is VHDLAbstractArrayType aat && aat.IndexTypes.Count() == 1) // dont support multidim arrays
				{
					return ConcatArrayElement(e1, e2, true, evaluationContext);
				}
			}
			else if (t1 is VHDLAbstractArrayType aat && aat.IndexTypes.Count() == 1)
			{
				if (t2 is VHDLStringLiteralType slt2)
				{
					return ConcatStringArray(slt2.Literal, aat, e1.Result, true, evaluationContext);
				}
				else if (t2 is VHDLAbstractArrayType aat2 && aat2.IndexTypes.Count() == 1) // dont support multidim arrays
				{
					return ConcatArrayArray(aat, e1.Result, aat2, e2.Result, evaluationContext);
				}
				else
				{
					return ConcatArrayElement(e1, e2, false, evaluationContext);
				}
			}


			throw new VHDLTypeEvalationException(
				string.Format("Operator '&' cannot be applied to operands of type '{0}' and '{1}'",
					t1.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLAddExpression
		: VHDLExpression
	{
		public VHDLAddExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLAddExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" + ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };

		VHDLEvaluatedExpression AddIntegers(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsInteger(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				if (e1.Result is VHDLIntegerLiteral l1 && e2.Result is VHDLIntegerLiteral l2)
					result = new VHDLIntegerLiteral(l1.Value + l2.Value);

				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		VHDLEvaluatedExpression AddReals(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsReal(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				if (e1.Result is VHDLRealLiteral l1 && e2.Result is VHDLRealLiteral l2)
					result = new VHDLRealLiteral(l1.Value + l2.Value);

				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			// IDK, since overload is possible by return type, we need to check the return type before evaluating the operands
			// because the operand might be an overloaded function call depending on expected return value.
			// But the return type of an operator cannot be deduced before evaluation of the operands
			VHDLEvaluatedExpression e1 = Expression1?.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2?.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLEvaluatedExpression result = AddIntegers(e1, e2);
			if (result != null)
				return result;
			result = AddIntegers(e2, e1);
			if (result != null)
				return result;

			result = AddReals(e1, e2);
			if (result != null)
				return result;
			result = AddReals(e2, e1);
			if (result != null)
				return result;

			// Look for an operator function with arguments of the correct type
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"+\"").OfType<VHDLFunctionDeclaration>();

			// filter by return type
			if (expectedType != null)
				operatorDeclarations = operatorDeclarations.Where(x => x.ReturnType.IsCompatible(expectedType) != VHDLCompatibilityResult.No);

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator '+' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}

		public static VHDLAddExpression AddConstant(VHDLExpression e, long value)
		{
			return new VHDLAddExpression(null, new Span(), e, new VHDLIntegerLiteral(null, new Span(), value, null));
		}
	}
	class VHDLSubtractExpression
		: VHDLExpression
	{
		public VHDLSubtractExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLSubtractExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" - ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		VHDLEvaluatedExpression SubtractIntegers(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsInteger(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				if (e1.Result is VHDLIntegerLiteral l1 && e2.Result is VHDLIntegerLiteral l2)
					result = new VHDLIntegerLiteral(l1.Value - l2.Value);

				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		VHDLEvaluatedExpression SubtractReals(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsReal(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				if (e1.Result is VHDLRealLiteral l1 && e2.Result is VHDLRealLiteral l2)
					result = new VHDLRealLiteral(l1.Value - l2.Value);

				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1?.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2?.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLEvaluatedExpression result = SubtractIntegers(e1, e2);
			if (result != null)
				return result;
			result = SubtractIntegers(e2, e1);
			if (result != null)
				return result;

			result = SubtractReals(e1, e2);
			if (result != null)
				return result;
			result = SubtractReals(e2, e1);
			if (result != null)
				return result;

			// Look for an operator function with arguments of the correct type
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"-\"").OfType<VHDLFunctionDeclaration>();

			// filter by return type
			if (expectedType != null)
				operatorDeclarations = operatorDeclarations.Where(x => x.ReturnType.IsCompatible(expectedType) != VHDLCompatibilityResult.No);

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator '-' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}

	class VHDLMultiplyExpression
		: VHDLExpression
	{
		public VHDLMultiplyExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLMultiplyExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" * ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		VHDLEvaluatedExpression MultiplyIntegers(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsInteger(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				if (e1.Result is VHDLIntegerLiteral l1 && e2.Result is VHDLIntegerLiteral l2)
					result = new VHDLIntegerLiteral(l1.Value * l2.Value);

				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		VHDLEvaluatedExpression MultiplyReals(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsReal(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				if (e1.Result is VHDLRealLiteral l1 && e2.Result is VHDLRealLiteral l2)
					result = new VHDLRealLiteral(l1.Value * l2.Value);

				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1?.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2?.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLEvaluatedExpression result = MultiplyIntegers(e1, e2);
			if (result != null)
				return result;
			result = MultiplyIntegers(e2, e1);
			if (result != null)
				return result;

			result = MultiplyReals(e1, e2);
			if (result != null)
				return result;
			result = MultiplyReals(e2, e1);
			if (result != null)
				return result;

			// Look for an operator function with arguments of the correct type
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"*\"").OfType<VHDLFunctionDeclaration>();

			// filter by return type
			if (expectedType != null)
				operatorDeclarations = operatorDeclarations.Where(x => x.ReturnType.IsCompatible(expectedType) != VHDLCompatibilityResult.No);

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator '*' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLDivideExpression
		: VHDLExpression
	{
		public VHDLDivideExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLDivideExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" / ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		VHDLEvaluatedExpression DivideIntegers(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsInteger(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				if (e1.Result is VHDLIntegerLiteral l1 && e2.Result is VHDLIntegerLiteral l2)
					result = new VHDLIntegerLiteral(l1.Value / l2.Value);

				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		VHDLEvaluatedExpression DivideReals(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsReal(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				if (e1.Result is VHDLRealLiteral l1 && e2.Result is VHDLRealLiteral l2)
					result = new VHDLRealLiteral(l1.Value / l2.Value);

				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1?.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2?.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLEvaluatedExpression result = DivideIntegers(e1, e2);
			if (result != null)
				return result;
			result = DivideIntegers(e2, e1);
			if (result != null)
				return result;

			result = DivideReals(e1, e2);
			if (result != null)
				return result;
			result = DivideReals(e2, e1);
			if (result != null)
				return result;

			// Look for an operator function with arguments of the correct type
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"/\"").OfType<VHDLFunctionDeclaration>();

			// filter by return type
			if (expectedType != null)
				operatorDeclarations = operatorDeclarations.Where(x => x.ReturnType.IsCompatible(expectedType) != VHDLCompatibilityResult.No);

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator '/' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLModuloExpression
		: VHDLExpression
	{
		public VHDLModuloExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLModuloExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" mod ", "keyword");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		VHDLEvaluatedExpression ModIntegers(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsInteger(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				if (e1.Result is VHDLIntegerLiteral l1 && e2.Result is VHDLIntegerLiteral l2)
					result = new VHDLIntegerLiteral(l1.Value / l2.Value);

				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		VHDLEvaluatedExpression ModReals(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsReal(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				if (e1.Result is VHDLRealLiteral l1 && e2.Result is VHDLRealLiteral l2)
					result = new VHDLRealLiteral(l1.Value / l2.Value);

				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1?.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2?.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLEvaluatedExpression result = ModIntegers(e1, e2);
			if (result != null)
				return result;
			result = ModIntegers(e2, e1);
			if (result != null)
				return result;

			result = ModReals(e1, e2);
			if (result != null)
				return result;
			result = ModReals(e2, e1);
			if (result != null)
				return result;

			// Look for an operator function with arguments of the correct type
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"MOD\"").OfType<VHDLFunctionDeclaration>();

			// filter by return type
			if (expectedType != null)
				operatorDeclarations = operatorDeclarations.Where(x => x.ReturnType.IsCompatible(expectedType) != VHDLCompatibilityResult.No);

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator 'MOD' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLRemainderExpression
		: VHDLExpression
	{
		public VHDLRemainderExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLRemainderExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" rem ", "keyword");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		VHDLEvaluatedExpression RemIntegers(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsInteger(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				if (e1.Result is VHDLIntegerLiteral l1 && e2.Result is VHDLIntegerLiteral l2)
					result = new VHDLIntegerLiteral(l1.Value / l2.Value);

				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		VHDLEvaluatedExpression RemReals(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsReal(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				if (e1.Result is VHDLRealLiteral l1 && e2.Result is VHDLRealLiteral l2)
					result = new VHDLRealLiteral(l1.Value / l2.Value);

				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1?.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2?.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLEvaluatedExpression result = RemIntegers(e1, e2);
			if (result != null)
				return result;
			result = RemIntegers(e2, e1);
			if (result != null)
				return result;

			result = RemReals(e1, e2);
			if (result != null)
				return result;
			result = RemReals(e2, e1);
			if (result != null)
				return result;

			// Look for an operator function with arguments of the correct type
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"REM\"").OfType<VHDLFunctionDeclaration>();

			// filter by return type
			if (expectedType != null)
				operatorDeclarations = operatorDeclarations.Where(x => x.ReturnType.IsCompatible(expectedType) != VHDLCompatibilityResult.No);

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator 'REM' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLAndExpression
		: VHDLExpression
	{
		public VHDLAndExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLAndExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" and ", "keyword");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"and\"").OfType<VHDLFunctionDeclaration>();

			// filter by return type
			if (expectedType != null)
				operatorDeclarations = operatorDeclarations.Where(x => x.ReturnType.IsCompatible(expectedType) != VHDLCompatibilityResult.No);

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			if (AnalysisResult.BooleanType != null &&
				e1.Type.IsCompatible(AnalysisResult.BooleanType) == VHDLCompatibilityResult.Yes &&
				e2.Type.IsCompatible(AnalysisResult.BooleanType) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator 'and' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLAbsoluteExpression
		: VHDLExpression
	{
		public VHDLAbsoluteExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLAbsoluteExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr)
			: base(analysisResult, span)
		{
			Expression = expr;
		}
		public VHDLExpression Expression { get; set; } = null;

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add("abs ", "keyword");
			text.Add(Expression.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e = Expression.Evaluate(evaluationContext);
			if (e.Result is VHDLIntegerLiteral il)
				return new VHDLEvaluatedExpression(e.Type, this, new VHDLIntegerLiteral(Math.Abs(il.Value)));
			if (e.Result is VHDLRealLiteral rl)
				return new VHDLEvaluatedExpression(e.Type, this, new VHDLRealLiteral(Math.Abs(rl.Value)));

			// Look for an operator function with arguments of the correct type
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"abs\"").OfType<VHDLFunctionDeclaration>();

			// filter by return type
			if (expectedType != null)
				operatorDeclarations = operatorDeclarations.Where(x => x.ReturnType.IsCompatible(expectedType) != VHDLCompatibilityResult.No);

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator 'abs' cannot be applied to operands of type '{0}'",
					e.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLNotExpression
		: VHDLExpression
	{
		public VHDLNotExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLNotExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr)
			: base(analysisResult, span)
		{
			Expression = expr;
		}
		public VHDLExpression Expression { get; set; } = null;

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add("not ", "keyword");
			text.Add(Expression.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e = Expression.Evaluate(evaluationContext);
			return new VHDLEvaluatedExpression(e.Type, this, null);
		}
	}
	// a ** b
	class VHDLPowerExpression
		: VHDLExpression
	{
		public VHDLPowerExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLPowerExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" ** ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		VHDLEvaluatedExpression PowerIntegers(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsInteger(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		VHDLEvaluatedExpression PowerReals(VHDLEvaluatedExpression e1, VHDLEvaluatedExpression e2)
		{
			if (VHDLType.IsReal(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
			{
				VHDLLiteral result = null;
				return new VHDLEvaluatedExpression(e1.Type, this, result);
			}
			return null;
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1?.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2?.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLEvaluatedExpression result = PowerIntegers(e1, e2);
			if (result != null)
				return result;
			result = PowerIntegers(e2, e1);
			if (result != null)
				return result;

			result = PowerReals(e1, e2);
			if (result != null)
				return result;
			result = PowerReals(e2, e1);
			if (result != null)
				return result;

			// Look for an operator function with arguments of the correct type
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"**\"").OfType<VHDLFunctionDeclaration>();

			// filter by return type
			if (expectedType != null)
				operatorDeclarations = operatorDeclarations.Where(x => x.ReturnType.IsCompatible(expectedType) != VHDLCompatibilityResult.No);

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator '**' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLShiftLeftLogicalExpression
		: VHDLExpression
	{
		public VHDLShiftLeftLogicalExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLShiftLeftLogicalExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" sll ", "keyword");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			if (!VHDLType.IsInteger(e2.Type))
			{
				throw new VHDLTypeEvalationException(
					string.Format("Operator 'sll' cannot be applied to operands of type '{0}' and '{1}'",
						e1.Type.GetClassifiedText().Text,
						e2.Type.GetClassifiedText().Text),
					Span);
			}

			return new VHDLEvaluatedExpression(e1.Type, this, null);
		}
	}
	class VHDLShiftLeftArithmeticExpression
		: VHDLExpression
	{
		public VHDLShiftLeftArithmeticExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLShiftLeftArithmeticExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" sla ", "keyword");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			if (!VHDLType.IsInteger(e2.Type))
			{
				throw new VHDLTypeEvalationException(
					string.Format("Operator 'sla' cannot be applied to operands of type '{0}' and '{1}'",
						e1.Type.GetClassifiedText().Text,
						e2.Type.GetClassifiedText().Text),
					Span);
			}

			return new VHDLEvaluatedExpression(e1.Type, this, null);
		}
	}
	class VHDLShiftRightLogicalExpression
		: VHDLExpression
	{
		public VHDLShiftRightLogicalExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLShiftRightLogicalExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" srl ", "keyword");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			if (!VHDLType.IsInteger(e2.Type))
			{
				throw new VHDLTypeEvalationException(
					string.Format("Operator 'srl' cannot be applied to operands of type '{0}' and '{1}'",
						e1.Type.GetClassifiedText().Text,
						e2.Type.GetClassifiedText().Text),
					Span);
			}

			return new VHDLEvaluatedExpression(e1.Type, this, null);
		}
	}
	class VHDLShiftRightArithmeticExpression
		: VHDLExpression
	{
		public VHDLShiftRightArithmeticExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLShiftRightArithmeticExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" sra ", "keyword");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			if (!VHDLType.IsInteger(e2.Type))
			{
				throw new VHDLTypeEvalationException(
					string.Format("Operator 'sra' cannot be applied to operands of type '{0}' and '{1}'",
						e1.Type.GetClassifiedText().Text,
						e2.Type.GetClassifiedText().Text),
					Span);
			}

			return new VHDLEvaluatedExpression(e1.Type, this, null);
		}
	}
	class VHDLRotateLeftExpression
		: VHDLExpression
	{
		public VHDLRotateLeftExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLRotateLeftExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" rol ", "keyword");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			if (!VHDLType.IsInteger(e2.Type))
			{
				throw new VHDLTypeEvalationException(
					string.Format("Operator 'rol' cannot be applied to operands of type '{0}' and '{1}'",
						e1.Type.GetClassifiedText().Text,
						e2.Type.GetClassifiedText().Text),
					Span);
			}

			return new VHDLEvaluatedExpression(e1.Type, this, null);
		}
	}
	class VHDLRotateRightExpression
		: VHDLExpression
	{
		public VHDLRotateRightExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLRotateRightExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" ror ", "keyword");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			if (!VHDLType.IsInteger(e2.Type))
			{
				throw new VHDLTypeEvalationException(
					string.Format("Operator 'ror' cannot be applied to operands of type '{0}' and '{1}'",
						e1.Type.GetClassifiedText().Text,
						e2.Type.GetClassifiedText().Text),
					Span);
			}

			return new VHDLEvaluatedExpression(e1.Type, this, null);
		}
	}

	class VHDLIsEqualExpression
		: VHDLExpression
	{
		public VHDLIsEqualExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLIsEqualExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" = ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			var comp1 = e1.Type.IsCompatible(e2.Type);
			var comp2 = e2.Type.IsCompatible(e1.Type);
			if (comp1 == VHDLCompatibilityResult.Yes || comp2 == VHDLCompatibilityResult.Yes)
			{
				if (e1.Result is VHDLIntegerLiteral i1 && e2.Result is VHDLIntegerLiteral i2)
					return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, new VHDLBooleanLiteral(i1.Value == i2.Value));
				if (e1.Result is VHDLRealLiteral r1 && e2.Result is VHDLIntegerLiteral r2)
					return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, new VHDLBooleanLiteral(r1.Value == r2.Value));
				if (e1.Result is VHDLBooleanLiteral b1 && e2.Result is VHDLBooleanLiteral b2)
					return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, new VHDLBooleanLiteral(b1.Value == b2.Value));
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);
			}
			if (comp1 == VHDLCompatibilityResult.Unsure || comp2 == VHDLCompatibilityResult.Unsure)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);

			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"=\"").OfType<VHDLFunctionDeclaration>();

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator '=' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}

	class VHDLNotEqualExpression
		: VHDLExpression
	{
		public VHDLNotEqualExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLNotEqualExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" /= ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			var comp1 = e1.Type.IsCompatible(e2.Type);
			var comp2 = e2.Type.IsCompatible(e1.Type);
			if (comp1 == VHDLCompatibilityResult.Yes || comp2 == VHDLCompatibilityResult.Yes)
			{
				if (e1.Result is VHDLIntegerLiteral i1 && e2.Result is VHDLIntegerLiteral i2)
					return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, new VHDLBooleanLiteral(i1.Value != i2.Value));
				if (e1.Result is VHDLRealLiteral r1 && e2.Result is VHDLIntegerLiteral r2)
					return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, new VHDLBooleanLiteral(r1.Value != r2.Value));
				if (e1.Result is VHDLBooleanLiteral b1 && e2.Result is VHDLBooleanLiteral b2)
					return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, new VHDLBooleanLiteral(b1.Value != b2.Value));
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);
			}
			if (comp1 == VHDLCompatibilityResult.Unsure || comp2 == VHDLCompatibilityResult.Unsure)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);

			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"and\"").OfType<VHDLFunctionDeclaration>();

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator '/=' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLGreaterExpression
		: VHDLExpression
	{
		public VHDLGreaterExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLGreaterExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" > ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLBooleanLiteral result = null;
			VHDLCompatibilityResult compatibility = VHDLCompatibilityResult.Unsure;
			
			if ((VHDLType.IsInteger(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes) ||
				(VHDLType.IsInteger(e2.Type) && e2.Type.IsCompatible(e1.Type) == VHDLCompatibilityResult.Yes))
			{
				compatibility = VHDLCompatibilityResult.Yes;
				if (e1.Result is VHDLIntegerLiteral l1 && e2.Result is VHDLIntegerLiteral l2)
					result = new VHDLBooleanLiteral(l1.Value > l2.Value);
			}
			if ((VHDLType.IsReal(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes) ||
				(VHDLType.IsReal(e2.Type) && e2.Type.IsCompatible(e1.Type) == VHDLCompatibilityResult.Yes))
			{
				compatibility = VHDLCompatibilityResult.Yes;
				if (e1.Result is VHDLRealLiteral l1 && e2.Result is VHDLRealLiteral l2)
					result = new VHDLBooleanLiteral(l1.Value > l2.Value);
			}
			if (e1.Type.Dereference() is VHDLEnumerationType && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);
			if (e2.Type.Dereference() is VHDLEnumerationType && e2.Type.IsCompatible(e1.Type) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);

			if (compatibility == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, result);

			// or when operator exists
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\">\"").OfType<VHDLFunctionDeclaration>();

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator '>' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLGreaterOrEqualExpression
		: VHDLExpression
	{
		public VHDLGreaterOrEqualExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLGreaterOrEqualExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" >= ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLBooleanLiteral result = null;
			VHDLCompatibilityResult compatibility = VHDLCompatibilityResult.Unsure;

			if ((VHDLType.IsInteger(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes) ||
				(VHDLType.IsInteger(e2.Type) && e2.Type.IsCompatible(e1.Type) == VHDLCompatibilityResult.Yes))
			{
				compatibility = VHDLCompatibilityResult.Yes;
				if (e1.Result is VHDLIntegerLiteral l1 && e2.Result is VHDLIntegerLiteral l2)
					result = new VHDLBooleanLiteral(l1.Value >= l2.Value);
			}
			if ((VHDLType.IsReal(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes) ||
				(VHDLType.IsReal(e2.Type) && e2.Type.IsCompatible(e1.Type) == VHDLCompatibilityResult.Yes))
			{
				compatibility = VHDLCompatibilityResult.Yes;
				if (e1.Result is VHDLRealLiteral l1 && e2.Result is VHDLRealLiteral l2)
					result = new VHDLBooleanLiteral(l1.Value >= l2.Value);
			}
			if (e1.Type.Dereference() is VHDLEnumerationType && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);
			if (e2.Type.Dereference() is VHDLEnumerationType && e2.Type.IsCompatible(e1.Type) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);

			if (compatibility == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, result);

			// or when operator exists
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\">=\"").OfType<VHDLFunctionDeclaration>();

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator '>=' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLLowerExpression
		: VHDLExpression
	{
		public VHDLLowerExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLLowerExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" < ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLBooleanLiteral result = null;
			VHDLCompatibilityResult compatibility = VHDLCompatibilityResult.Unsure;

			if ((VHDLType.IsInteger(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes) ||
				(VHDLType.IsInteger(e2.Type) && e2.Type.IsCompatible(e1.Type) == VHDLCompatibilityResult.Yes))
			{
				compatibility = VHDLCompatibilityResult.Yes;
				if (e1.Result is VHDLIntegerLiteral l1 && e2.Result is VHDLIntegerLiteral l2)
					result = new VHDLBooleanLiteral(l1.Value < l2.Value);
			}
			if ((VHDLType.IsReal(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes) ||
				(VHDLType.IsReal(e2.Type) && e2.Type.IsCompatible(e1.Type) == VHDLCompatibilityResult.Yes))
			{
				compatibility = VHDLCompatibilityResult.Yes;
				if (e1.Result is VHDLRealLiteral l1 && e2.Result is VHDLRealLiteral l2)
					result = new VHDLBooleanLiteral(l1.Value < l2.Value);
			}
			if (e1.Type.Dereference() is VHDLEnumerationType && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);
			if (e2.Type.Dereference() is VHDLEnumerationType && e2.Type.IsCompatible(e1.Type) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);

			if (compatibility == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, result);

			// or when operator exists
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"<\"").OfType<VHDLFunctionDeclaration>();

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator '<' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLLowerOrEqualExpression
		: VHDLExpression
	{
		public VHDLLowerOrEqualExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLLowerOrEqualExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" <= ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLBooleanLiteral result = null;
			VHDLCompatibilityResult compatibility = VHDLCompatibilityResult.Unsure;

			if ((VHDLType.IsInteger(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes) ||
				(VHDLType.IsInteger(e2.Type) && e2.Type.IsCompatible(e1.Type) == VHDLCompatibilityResult.Yes))
			{
				compatibility = VHDLCompatibilityResult.Yes;
				if (e1.Result is VHDLIntegerLiteral l1 && e2.Result is VHDLIntegerLiteral l2)
					result = new VHDLBooleanLiteral(l1.Value <= l2.Value);
			}
			if ((VHDLType.IsReal(e1.Type) && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes) ||
				(VHDLType.IsReal(e2.Type) && e2.Type.IsCompatible(e1.Type) == VHDLCompatibilityResult.Yes))
			{
				compatibility = VHDLCompatibilityResult.Yes;
				if (e1.Result is VHDLRealLiteral l1 && e2.Result is VHDLRealLiteral l2)
					result = new VHDLBooleanLiteral(l1.Value <= l2.Value);
			}
			if (e1.Type.Dereference() is VHDLEnumerationType && e1.Type.IsCompatible(e2.Type) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);
			if (e2.Type.Dereference() is VHDLEnumerationType && e2.Type.IsCompatible(e1.Type) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);

			if (compatibility == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, result);

			// or when operator exists
			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"<=\"").OfType<VHDLFunctionDeclaration>();

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator '<=' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLAssignExpression
		: VHDLExpression
	{
		public VHDLAssignExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLAssignExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" == ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return null;
		}
	}
	class VHDLOrExpression
		: VHDLExpression
	{
		public VHDLOrExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLOrExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" or ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"or\"").OfType<VHDLFunctionDeclaration>();

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			if (AnalysisResult.BooleanType != null &&
				e1.Type.IsCompatible(AnalysisResult.BooleanType) == VHDLCompatibilityResult.Yes &&
				e2.Type.IsCompatible(AnalysisResult.BooleanType) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator 'or' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLNandExpression
		: VHDLExpression
	{
		public VHDLNandExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLNandExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" nand ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"nand\"").OfType<VHDLFunctionDeclaration>();

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			if (AnalysisResult.BooleanType != null &&
				e1.Type.IsCompatible(AnalysisResult.BooleanType) == VHDLCompatibilityResult.Yes &&
				e2.Type.IsCompatible(AnalysisResult.BooleanType) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator 'nand' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLNorExpression
		: VHDLExpression
	{
		public VHDLNorExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLNorExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" nor ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"nor\"").OfType<VHDLFunctionDeclaration>();

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			if (AnalysisResult.BooleanType != null &&
				e1.Type.IsCompatible(AnalysisResult.BooleanType) == VHDLCompatibilityResult.Yes &&
				e2.Type.IsCompatible(AnalysisResult.BooleanType) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator 'nor' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLXorExpression
		: VHDLExpression
	{
		public VHDLXorExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLXorExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" xor ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"xor\"").OfType<VHDLFunctionDeclaration>();

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			if (AnalysisResult.BooleanType != null &&
				e1.Type.IsCompatible(AnalysisResult.BooleanType) == VHDLCompatibilityResult.Yes &&
				e2.Type.IsCompatible(AnalysisResult.BooleanType) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator 'xor' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}
	class VHDLXnorExpression
		: VHDLExpression
	{
		public VHDLXnorExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLXnorExpression(AnalysisResult analysisResult, Span span, VHDLExpression expr1, VHDLExpression expr2)
			: base(analysisResult, span)
		{
			Expression1 = expr1;
			Expression2 = expr2;
		}
		public VHDLExpression Expression1 { get; set; } = null;
		public VHDLExpression Expression2 { get; set; } = null;
		
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression1.GetClassifiedText());
			text.Add(" xnor ");
			text.Add(Expression2.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression1, Expression2 };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression e1 = Expression1.Evaluate(evaluationContext);
			VHDLEvaluatedExpression e2 = Expression2.Evaluate(evaluationContext);
			if (e1 == null || e2 == null)
				return null;

			VHDLDeclaration enclosingDecl = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
			IEnumerable<VHDLFunctionDeclaration> operatorDeclarations = VHDLDeclarationUtilities.FindAllNames(enclosingDecl, "\"xnor\"").OfType<VHDLFunctionDeclaration>();

			VHDLFunctionDeclaration bestMatch = VHDLDeclarationUtilities.GetBestMatch(operatorDeclarations, e1.Type, e2.Type);
			if (bestMatch != null)
				return new VHDLEvaluatedExpression(bestMatch.ReturnType, this, null);

			if (AnalysisResult.BooleanType != null &&
				e1.Type.IsCompatible(AnalysisResult.BooleanType) == VHDLCompatibilityResult.Yes &&
				e2.Type.IsCompatible(AnalysisResult.BooleanType) == VHDLCompatibilityResult.Yes)
				return new VHDLEvaluatedExpression(AnalysisResult.BooleanType, this, null);

			throw new VHDLTypeEvalationException(
				string.Format("Operator 'xnor' cannot be applied to operands of type '{0}' and '{1}'",
					e1.Type.GetClassifiedText().Text,
					e2.Type.GetClassifiedText().Text),
				Span);
		}
	}

	class VHDLNameExpression
		: VHDLReferenceExpression
	{
		public VHDLNameExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLNameExpression(AnalysisResult analysisResult, Span span, string name)
			: base(analysisResult, span)
		{
			Name = name;
		}
		public string Name { get; set; } = null;

		public override void Resolve(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
			try
			{
				VHDLDeclaration enclosingDeclaration = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
				Declaration = VHDLDeclarationUtilities.FindName(enclosingDeclaration, Name);
				if (Declaration == null)
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("The name '{0}' does not exist in the current context", Name), Span));
				else
					deepAnalysisResult.SortedReferences.Add(Span.Start,
						new VHDLNameReference(
							Name,
							Span,
							Declaration));
			}
			catch (VHDLNameNotFoundException e)
			{
				errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, e.Message, e.Span));
			}
		}

		public override IEnumerable<VHDLDeclaration> GetDeclarations()
		{
			try
			{
				VHDLDeclaration enclosingDeclaration = VHDLDeclarationUtilities.GetEnclosingDeclaration(AnalysisResult, Span.Start);
				return VHDLDeclarationUtilities.FindAllNames(enclosingDeclaration, Name);
			}
			catch (Exception)
			{
			}
			return Array.Empty<VHDLDeclaration>();
		}
		public override VHDLClassifiedText GetClassifiedText()
		{
			if (Declaration != null)
				return Declaration.GetClassifiedName();
			else
				return new VHDLClassifiedText(Name);
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			if (evaluationContext?.Contains(Declaration) == true)
				return evaluationContext[Declaration];

			if (Declaration is VHDLConstantDeclaration constantDecl)
				return new VHDLEvaluatedExpression(constantDecl.Type, this, constantDecl.InitializationExpression?.Evaluate(evaluationContext)?.Result);
			if (Declaration is VHDLAbstractVariableDeclaration d)
			{
				return new VHDLEvaluatedExpression(d.Type, this, null);
			}
			return null;
		}

	}
	class VHDLFunctionCallOrIndexExpression
		: VHDLExpression
	{
		public VHDLFunctionCallOrIndexExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLFunctionCallOrIndexExpression(AnalysisResult analysisResult, Span span, VHDLExpression expression, IEnumerable<VHDLExpression> arguments)
			: base(analysisResult, span)
		{
			NameExpression = expression;
			Arguments = new List<VHDLExpression>(arguments);
		}
		public VHDLExpression NameExpression { get; set; } = null;
		public List<VHDLExpression> Arguments { get; set; } = null;
				
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(NameExpression.GetClassifiedText());
			text.Add("(");
			foreach (var arg in Arguments.Take(Arguments.Count - 1))
			{
				text.Add(arg.GetClassifiedText());
				text.Add(", ");
			}
			text.Add(Arguments.Last().GetClassifiedText());
			text.Add(")");
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => Arguments.Prepend(NameExpression);


		private int IndexOf<T>(IEnumerable<T> l, Func<T, bool> predicate)
		{
			int i = 0;
			foreach (T item in l)
			{
				if (predicate(item))
					return i;
				++i;
			}
			return -1;
		}
		private IEnumerable<VHDLExpression> ReorderParameters(VHDLFunctionDeclaration declaration, IEnumerable<VHDLExpression> arguments)
		{
			if (!arguments.Any())
				return arguments;

			if (!(arguments.First() is VHDLArgumentAssociationExpression))
				return arguments;

			VHDLExpression[] args = new VHDLExpression[arguments.Count()];
			foreach (var a in arguments)
			{
				VHDLArgumentAssociationExpression aae = a as VHDLArgumentAssociationExpression;
				if (aae == null)
					throw new VHDLCodeException(string.Format("Cannot mix positionnal and named arguments"), Span);

				VHDLNameExpression ne = aae.Argument as VHDLNameExpression;
				if (ne == null)
					throw new VHDLCodeException(string.Format("Positionnal argument other than name not supported"), Span);

				int i = IndexOf(declaration.Parameters, x => string.Compare(x.Name, ne.Name, true) == 0);
				if (i == -1)
					throw new VHDLCodeException(string.Format("Parameter name not found '{0}'", ne.Name), Span);

				args[i] = aae.Value;
			}
			return args;
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression evaluatedName = NameExpression.Evaluate(evaluationContext);
			if (NameExpression is VHDLReferenceExpression r)
			{
				if (r == null)
					return null;
				if (r.Declaration is VHDLSubprogramDeclaration fd)
				{
					var declarations = r.GetDeclarations().OfType<VHDLFunctionDeclaration>().ToArray();
					declarations = declarations.Distinct().ToArray();
					declarations = declarations.Select(x => x.Body ?? x).Distinct().ToArray(); // keep function declaration only when body is not found
					// filter by return type
					if (expectedType != null)
						declarations = declarations.Where(x => x.ReturnType.IsCompatible(expectedType) != VHDLCompatibilityResult.No).ToArray();

					if (declarations.Count() == 1)
					{
						// There is no alternatives, so we can deduce the expected parameter types.
						// This can help resolve ambiguities in parameters
						VHDLFunctionDeclaration d = declarations.First();
						if (d.Parameters.Count != Arguments.Count)
							throw new VHDLCodeException(string.Format("'{0}' arguments expected, got '{1}'", d.Parameters.Count, Arguments.Count), Span);

						var evaluatedParameters = ReorderParameters(d, Arguments).Zip(d.Parameters, (x, y) => Tuple.Create(x.Evaluate(evaluationContext, y.Type), y));
						foreach (var (ev, ex) in evaluatedParameters)
						{
							if (ex.Type.IsCompatible(ev.Type) == VHDLCompatibilityResult.No)
								throw new VHDLCodeException(string.Format("Wrong argument type, '{0}' expected, got '{1}'",
									ex.Type.GetClassifiedText()?.Text ?? "<error type>",
									ev.Type.GetClassifiedText()?.Text ?? "<error type>"), ev?.Expression?.Span ?? Span);
						}

						VHDLEvaluatedExpression result = (d as VHDLFunctionBodyDeclaration)?.EvaluateCall(evaluatedParameters.Select(x => x.Item1), evaluationContext);
						if (result == null)
							result = new VHDLEvaluatedExpression(d.ReturnType, this, null);
						return result;
					}
					List<Tuple<VHDLEvaluatedExpression, VHDLFunctionDeclaration>> results = new List<Tuple<VHDLEvaluatedExpression, VHDLFunctionDeclaration>>();
					foreach (var d in declarations)
					{
						try
						{
							if (d.Parameters.Count != Arguments.Count)
								continue;

							var evaluatedParameters = ReorderParameters(d, Arguments).Zip(d.Parameters, (x, y) => Tuple.Create(x.Evaluate(evaluationContext, y.Type), y)).ToArray();
							if (evaluatedParameters.Any(x => x.Item2.Type.IsCompatible(x.Item1.Type) == VHDLCompatibilityResult.No))
								continue;
							VHDLEvaluatedExpression result = (d as VHDLFunctionBodyDeclaration)?.EvaluateCall(evaluatedParameters.Select(x => x.Item1), evaluationContext);
							if (result == null)
								result = new VHDLEvaluatedExpression(d.ReturnType, this, null);
							results.Add(Tuple.Create(result, d));
						}
						catch (Exception ex)
						{ }
					}
					if (results.Count == 0)
					{
						throw new VHDLCodeException(string.Format("No function found that matches the given parameters"), Span);
					}
					if (results.Count > 1)
					{
						throw new VHDLCodeException(string.Format("Call to {0} is ambiguous. Possibilities are {1}",
							NameExpression?.GetClassifiedText()?.Text ?? "<error>",
							string.Join(", ", results.Select(x => "'" + x?.Item2?.GetClassifiedName()?.Text ?? "<error>" + "'"))), Span);
					}
					return results[0].Item1;
				}
				if (r.Declaration is VHDLTypeDeclaration || r.Declaration is VHDLSubTypeDeclaration)
				{
					VHDLType t = (r.Declaration as VHDLTypeDeclaration)?.Type ?? (r.Declaration as VHDLSubTypeDeclaration)?.Type;

					if (Arguments.Count != 1)
						throw new VHDLCodeException("Type cast can only have 1 argument", Span);

					VHDLEvaluatedExpression evaluatedArg = Arguments[0].Evaluate(evaluationContext);
					if (!evaluatedArg.Type.IsCastable(t))
					{
						throw new VHDLCodeException(string.Format("Type '{0}' cannot be converted to type '{1}'", evaluatedArg.Type.GetClassifiedText().Text, t.Declaration.GetClassifiedName().Text), Span);
					}

					if (evaluatedArg.Type is VHDLAbstractArrayType eargAat && eargAat.IsConstrained)
					{
						VHDLArraySliceType ast = new VHDLArraySliceType(t, eargAat.GetIndexRange(0));
						return new VHDLEvaluatedExpression(ast, this, null);
					}
					return new VHDLEvaluatedExpression(t, this, null);
				}
			}

			if (evaluatedName?.Type?.Dereference() is VHDLAbstractArrayType aat)
			{
				if (aat.Dimension != Arguments.Count)
					throw new VHDLCodeException(string.Format("Array of dimension {0} expects {0} arguments, {1} given", aat.Dimension, Arguments.Count), Span);

				if (aat.Dimension == 1)
				{
					if (Arguments[0] is VHDLRangeExpression range)
					{
						// This is wrong, we should make a slice
						// it's not the same as the array type because the size is different
						// But it keeps compatibility rules
						return new VHDLEvaluatedExpression(new VHDLArraySliceType(aat.GetBaseType(), range?.Range), this, null);
					}
					else
					{
						VHDLEvaluatedExpression evaluatedArg = Arguments[0].Evaluate(evaluationContext);
						if (aat.IndexTypes.First().IsCompatible(evaluatedArg.Type) == VHDLCompatibilityResult.No)
							throw new VHDLCodeException(string.Format("Wrong argument type, '{0}' expected, got '{1}'",
								aat.IndexTypes.First()?.GetClassifiedText()?.Text ?? "<error type>",
								evaluatedArg.Type?.GetClassifiedText()?.Text ?? "<error type>"), Span);

						VHDLRange ra = aat.GetIndexRange(0);
						if (ra?.IsOutOfBound(Arguments[0]) == VHDLCompatibilityResult.Yes)
							throw new VHDLCodeException(string.Format("Argument is out of bounds"), Span);
					}

					return new VHDLEvaluatedExpression(aat.ElementType, this, null);
				}
				else
				{
					foreach (var (x, y) in aat.IndexTypes.Zip(Arguments, (x, y) => Tuple.Create(x, y)))
					{
						VHDLEvaluatedExpression evaluatedArg = y.Evaluate(evaluationContext);
						if (evaluatedArg.Type.IsCompatible(x) == VHDLCompatibilityResult.No)
							throw new VHDLCodeException(string.Format("Wrong argument type, '{0}' expected, got '{1}'",
								aat.IndexTypes.First()?.GetClassifiedText()?.Text ?? "<error type>",
								evaluatedArg.Type?.GetClassifiedText()?.Text ?? "<error type>"), Span);
					}

					return new VHDLEvaluatedExpression(aat.ElementType, this, null);
				}
			}
			return null;
		}
	}
	class VHDLMemberSelectExpression
		: VHDLReferenceExpression
	{
		public VHDLMemberSelectExpression(AnalysisResult analysisResult, Span span, Span nameSpan)
			: base(analysisResult, span)
		{
			NameSpan = nameSpan;
		}
		public VHDLMemberSelectExpression(AnalysisResult analysisResult, Span span, Span nameSpan, VHDLExpression expression, string name)
			: base(analysisResult, span)
		{
			Expression = expression;
			Name = name;
			NameSpan = nameSpan;
		}

		public Span NameSpan { get; set; }
		public VHDLExpression Expression { get; set; } = null;
		public string Name { get; set; } = null;
		public override void Resolve(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
			try
			{ 
				if (Expression is VHDLReferenceExpression)
				{
					VHDLDeclaration decl = (Expression as VHDLReferenceExpression)?.Declaration;
					if (decl != null)
					{
						Declaration = VHDLDeclarationUtilities.GetMemberDeclaration(decl, Name);

						if (Declaration != null)
							deepAnalysisResult.SortedReferences.Add(NameSpan.Start,
								new VHDLNameReference(
									Name,
									NameSpan,
									Declaration));
					}
				}
			}
			catch (VHDLNameNotFoundException e)
			{
				errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, e.Message, e.Span));
			}
		}
		public override IEnumerable<VHDLDeclaration> GetDeclarations()
		{
			try
			{
				if (Expression is VHDLReferenceExpression)
				{
					VHDLDeclaration decl = (Expression as VHDLReferenceExpression)?.Declaration;
					if (decl != null)
						return VHDLDeclarationUtilities.GetAllMemberDeclarations(decl, Name);
				}
			}
			catch (Exception)
			{
			}
			return Array.Empty<VHDLDeclaration>();
		}
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression.GetClassifiedText());
			text.Add(".");
			if (Declaration != null)
				text.Add(Declaration?.GetClassifiedName());
			else
				text.Add(Name);
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			VHDLEvaluatedExpression ee = Expression?.Evaluate(evaluationContext);
			if (ee?.Type?.Dereference()?.Declaration != null)
			{
				VHDLDeclaration decl = VHDLDeclarationUtilities.GetMemberDeclaration(ee?.Type?.Dereference()?.Declaration, Name);
				if (decl is VHDLRecordElementDeclaration red)
					return new VHDLEvaluatedExpression(red.Type, this, null);
				return null;
			}

			if (Declaration is VHDLAbstractVariableDeclaration d)
				return new VHDLEvaluatedExpression(d.Type, this, null);
			return null;
		}
	}

	// Thing like <function>(arg1(4 DOWNTO 0) => value, ...)
	class VHDLArgumentAssociationExpression
		: VHDLExpression
	{
		public VHDLArgumentAssociationExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLArgumentAssociationExpression(AnalysisResult analysisResult, Span span, VHDLExpression argument, VHDLExpression value)
			: base(analysisResult, span)
		{
			Argument = argument;
			Value = value;
		}
		public VHDLExpression Argument { get; set; } = null;
		public VHDLExpression Value { get; set; } = null;
		
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Argument.GetClassifiedText());
			text.Add(" => ");
			text.Add(Value.GetClassifiedText());
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Argument, Value };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return null;
		}
	}

	// <expr> downto|to <expr>
	class VHDLRangeExpression
		: VHDLExpression
	{
		public VHDLRangeExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLRangeExpression(AnalysisResult analysisResult, Span span, VHDLExpression expression1, VHDLExpression expression2, VHDLRangeDirection direction)
			: base(analysisResult, span)
		{
			Range = new VHDLRange() {  Start = expression1, End = expression2, Direction = direction };
		}
		public VHDLRange Range { get; set; } = null;
		
		public override VHDLClassifiedText GetClassifiedText()
		{
			return Range.GetClassifiedText();
		}
		public override IEnumerable<VHDLExpression> Children => Range == null ? Array.Empty<VHDLExpression>() : (Range.End != null ? new VHDLExpression[] { Range.Start, Range.End } : new VHDLExpression[] { Range.Start });
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return null;
		}
	}

	class VHDLAttributeExpression
		: VHDLExpression
	{
		public VHDLAttributeExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }
		public VHDLAttributeExpression(AnalysisResult analysisResult, Span span, VHDLExpression expression, string attribute)
			: base(analysisResult, span)
		{
			Expression = expression;
			Attribute = attribute;
		}
		public VHDLExpression Expression { get; set; } = null;
		public string Attribute { get; set; } = null;
		
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(Expression.GetClassifiedText());
			text.Add("'" + Attribute);
			return text;
		}
		public override IEnumerable<VHDLExpression> Children => new VHDLExpression[] { Expression };
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			if (string.Compare(Attribute, "length", true) == 0)
			{
				VHDLEvaluatedExpression e = Expression?.Evaluate(evaluationContext);
				VHDLType t = e?.Type?.Dereference();
				if (t is VHDLAbstractArrayType aat && aat.IndexTypes.Count() == 1)
				{
					VHDLRange r = aat.GetIndexRange(0);
					VHDLEvaluatedExpression l = r?.Count(aat.GetIndexType(0)).Evaluate(evaluationContext);
					return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, l?.Result);
				}
				else if(t is VHDLArraySliceType ast)
				{
					VHDLRange r = ast.Range;
					VHDLEvaluatedExpression l = r?.Count((ast.GetBaseType() as VHDLArrayType)?.GetIndexType(0)).Evaluate(evaluationContext);
					return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, l?.Result);
				}
				throw new VHDLCodeException(string.Format("'length' attribute can only be used on array, got '{0}'",
					e?.Type?.GetClassifiedText()?.Text ?? "<error type>"), Span);
			}
			else if (string.Compare(Attribute, "left", true) == 0)
			{
				VHDLEvaluatedExpression e = Expression?.Evaluate(evaluationContext);
				VHDLType t = e?.Type?.Dereference();
				if (t is VHDLAbstractArrayType aat && aat.IndexTypes.Count() == 1)
				{
					return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, null);
				}
				else if (t is VHDLArraySliceType ast)
				{
					return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, null);
				}
				throw new VHDLCodeException(string.Format("'left' attribute can only be used on array, got '{0}'",
					e?.Type?.GetClassifiedText()?.Text ?? "<error type>"), Span);
			}
			else if (string.Compare(Attribute, "right", true) == 0)
			{
				VHDLEvaluatedExpression e = Expression?.Evaluate(evaluationContext);
				VHDLType t = e?.Type?.Dereference();
				if (t is VHDLAbstractArrayType aat && aat.IndexTypes.Count() == 1)
				{
					return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, null);
				}
				else if (t is VHDLArraySliceType ast)
				{
					return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, null);
				}
				throw new VHDLCodeException(string.Format("'right' attribute can only be used on array, got '{0}'",
					e?.Type?.GetClassifiedText()?.Text ?? "<error type>"), Span);
			}
			else if (string.Compare(Attribute, "high", true) == 0)
			{
				VHDLEvaluatedExpression e = Expression?.Evaluate(evaluationContext);
				VHDLType t = e?.Type?.Dereference();
				if (t is VHDLAbstractArrayType aat && aat.IndexTypes.Count() == 1)
				{
					return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, null);
				}
				else if (t is VHDLArraySliceType ast)
				{
					return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, null);
				}
				else if (Expression is VHDLReferenceExpression r && r.Declaration is VHDLTypeDeclaration td)
				{
					if (td.Type is VHDLEnumerationType et)
					{
						return new VHDLEvaluatedExpression(et, this, null);
					}
					else if (td.Type is VHDLAbstractArrayType aat2 && aat2.Dimension == 1 && aat2.IsConstrained)
					{
						return new VHDLEvaluatedExpression(aat2.GetIndexType(0), this, null);
					}
					else if (td.Type is VHDLScalarType st)
					{
						VHDLRange range = st.GetRange();
						if (range.Direction == VHDLRangeDirection.DownTo)
							return range.Start.Evaluate(evaluationContext);
						else
							return range.End.Evaluate(evaluationContext);
					}
				}
				throw new VHDLCodeException(string.Format("'high' attribute can only be used on array and enum type, got '{0}'",
					e?.Type?.GetClassifiedText()?.Text ?? "<error type>"), Span);
			}
			else if (string.Compare(Attribute, "low", true) == 0)
			{
				VHDLEvaluatedExpression e = Expression?.Evaluate(evaluationContext);
				VHDLType t = e?.Type?.Dereference();
				if (t is VHDLAbstractArrayType aat && aat.IndexTypes.Count() == 1)
				{
					return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, null);
				}
				else if (t is VHDLArraySliceType ast)
				{
					return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, null);
				}
				else if (Expression is VHDLReferenceExpression r && r.Declaration is VHDLTypeDeclaration td)
				{
					if (td.Type is VHDLEnumerationType et)
					{
						return new VHDLEvaluatedExpression(et, this, null);
					}
					else if (td.Type is VHDLAbstractArrayType aat2 && aat2.Dimension == 1 && aat2.IsConstrained)
					{
						return new VHDLEvaluatedExpression(aat2.GetIndexType(0), this, null);
					}
					else if (td.Type is VHDLScalarType st)
					{
						VHDLRange range = st.GetRange();
						if (range.Direction == VHDLRangeDirection.DownTo)
							return range.End.Evaluate(evaluationContext);
						else
							return range.Start.Evaluate(evaluationContext);
					}
				}
				throw new VHDLCodeException(string.Format("'low' attribute can only be used on array and enum type, got '{0}'",
					e?.Type?.GetClassifiedText()?.Text ?? "<error type>"), Span);
			}
			else if (string.Compare(Attribute, "ascending", true) == 0)
			{
				VHDLEvaluatedExpression e = Expression?.Evaluate(evaluationContext);
				VHDLType t = e?.Type?.Dereference();
				if (t is VHDLAbstractArrayType aat && aat.IndexTypes.Count() == 1)
				{
					return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, null);
				}
				else if (t is VHDLArraySliceType ast)
				{
					return new VHDLEvaluatedExpression(VHDLBuiltinTypeInteger.Instance, this, null);
				}
				throw new VHDLCodeException(string.Format("'ascending' attribute can only be used on array, got '{0}'",
					e?.Type?.GetClassifiedText()?.Text ?? "<error type>"), Span);
			}
			throw new VHDLCodeException(string.Format("Unknown attribute '{0}'", Attribute), Span);
		}
	}

	class VHDLOthersExpression
		: VHDLExpression
	{
		public VHDLOthersExpression(AnalysisResult analysisResult, Span span)
			: base(analysisResult, span) { }

		
		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText("others", "keyword");
		}
		public override VHDLEvaluatedExpression Evaluate(EvaluationContext evaluationContext, VHDLType expectedType = null)
		{
			return null;
		}
	}
}
