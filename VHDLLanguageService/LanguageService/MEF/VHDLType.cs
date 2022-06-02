using Antlr4.Runtime;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompany.LanguageServices.VHDL
{
	enum VHDLCompatibilityResult
	{
		Yes,
		No,
		Unsure,
	}
	//// # <typedef>, subtype_indication
	//<typename> RANGE <name>
	//<typename> RANGE<expr> DOWNTO<expr>
	//<typename> (RANGE<name>, ...)
	//<typename> (RANGE<expr> DOWNTO <expr> , ...)
	//<typename> (<typedef> , ...)


	//// # unconstrained
	//ARRAY(<typename> RANGE<>, ...) OF<typedef>
	//// constrained
	//ARRAY(<typedef>, ...) OF<typedef>
	//ARRAY(<expr> [DOWNTO<expr>]?, ...) OF<typedef>

	//// # scalar
	//(enum)
	//RANGE<typename>
	//RANGE<expr> DOWNTO<expr>

	//// # signal
	//SIGNAL <name> : <typedef> ;
	abstract class VHDLType
	{
		public virtual VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText();
		}

		// Return true if a variable of this type can be assigned a value of the given type
		public virtual VHDLCompatibilityResult IsCompatible(VHDLType type)
		{
			return VHDLCompatibilityResult.No;
		}

		public virtual VHDLType Dereference()
		{
			return this;
		}
		public virtual VHDLType GetBaseType()
		{
			return null;
		}

		public static bool IsInteger(VHDLType t)
		{
			t = t.GetBaseType();
			if (t == VHDLBuiltinTypeInteger.Instance)
				return true;

			if (!(t is VHDLScalarType))
				return false;
			VHDLScalarType scalarType = (VHDLScalarType)t;
			if (scalarType.Range.DeduceType() == VHDLBuiltinTypeInteger.Instance)
				return true;

			return false;
		}
		public static bool IsReal(VHDLType t)
		{
			t = t.GetBaseType();
			if (t == VHDLBuiltinTypeReal.Instance)
				return true;

			if (!(t is VHDLScalarType))
				return false;
			VHDLScalarType scalarType = (VHDLScalarType)t;
			if (scalarType.Range.DeduceType() == VHDLBuiltinTypeReal.Instance)
				return true;

			return false;
		}

		public virtual bool IsCastable(VHDLType t)
		{
			return false;
		}

		// Is non null only when the type is the result of a code declaration, type or subtype
		public VHDLDeclaration Declaration { get; set; } = null;
	}
	class VHDLBuiltinTypeInteger
		: VHDLType
	{
		// Prevent instanciation, just use VHDLBuiltinTypeInteger.Instance
		private VHDLBuiltinTypeInteger() { }
		public static readonly VHDLBuiltinTypeInteger Instance = new VHDLBuiltinTypeInteger();

		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText("integer", "vhdl.type");
		}

		public override VHDLCompatibilityResult IsCompatible(VHDLType type)
		{
			if (type == Instance)
				return VHDLCompatibilityResult.Yes;
			return VHDLCompatibilityResult.No;
		}
		public override VHDLType GetBaseType()
		{
			return this;
		}
	}
	class VHDLBuiltinTypeReal
		: VHDLType
	{
		// Prevent instanciation, just use VHDLBuiltinTypeReal.Instance
		private VHDLBuiltinTypeReal() { }
		public static readonly VHDLBuiltinTypeReal Instance = new VHDLBuiltinTypeReal();

		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText("real", "vhdl.type");
		}

		public override VHDLCompatibilityResult IsCompatible(VHDLType type)
		{
			if (type == Instance)
				return VHDLCompatibilityResult.Yes;
			return VHDLCompatibilityResult.No;
		}
		public override VHDLType GetBaseType()
		{
			return this;
		}
	}
	class VHDLBuiltinTypeNull
		: VHDLType
	{
		// Prevent instanciation, just use VHDLBuiltinTypeReal.Instance
		private VHDLBuiltinTypeNull() { }
		public static readonly VHDLBuiltinTypeNull Instance = new VHDLBuiltinTypeNull();
		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText("null", "keyword");
		}

		public override VHDLCompatibilityResult IsCompatible(VHDLType type)
		{
			return VHDLCompatibilityResult.No;
		}
		public override VHDLType GetBaseType()
		{
			return this;
		}
	}
	class VHDLCharLiteralType
		: VHDLType
	{
		// Prevent instanciation, just use VHDLBuiltinTypeReal.Instance
		public VHDLCharLiteralType(VHDLCharacterLiteral l)
		{
			Literal = l;
		}

		public VHDLCharacterLiteral Literal { get; set; } = null;

		public override VHDLCompatibilityResult IsCompatible(VHDLType type)
		{
			return VHDLCompatibilityResult.No;
		}
		public override VHDLType GetBaseType()
		{
			return this;
		}
		public override VHDLClassifiedText GetClassifiedText()
		{
			return Literal.GetClassifiedText();
		}
	}
	class VHDLStringLiteralType
		: VHDLType
	{
		// Prevent instanciation, just use VHDLBuiltinTypeReal.Instance
		public VHDLStringLiteralType(VHDLLiteral l)
		{
			Literal = l;
		}

		public VHDLLiteral Literal { get; set; } = null;


		public override VHDLClassifiedText GetClassifiedText()
		{
			return Literal.GetClassifiedText();
		}
		public override VHDLCompatibilityResult IsCompatible(VHDLType type)
		{
			return VHDLCompatibilityResult.No;
		}
		public override VHDLType GetBaseType()
		{
			return this;
		}
	}
	class VHDLEnumerationType
		: VHDLType
	{
		public List<string> Values { get; set; } = new List<string>();

		public override VHDLClassifiedText GetClassifiedText()
		{
			if (Values.Count == 0)
				return new VHDLClassifiedText("()");

			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add("( ");
			for (int i = 0; i < Values.Count; ++i)
			{
				string v = Values[i];
				if (v.StartsWith("'"))
					text.Add(v, "string");
				else
					text.Add(v, "vhdl.constant");

				if (i < Values.Count - 1)
					text.Add(", ");
			}

			text.Add(" )");
			return text;
		}

		// That is really complicated.
		// Lets say we have e1 <= 'x', then if we use the expression we can detect if its valid
		// But if we have "<array of e> <= a & b & func1(c)" then it gets pretty complicated
		public override VHDLCompatibilityResult IsCompatible(VHDLType type)
		{
			if (type.GetBaseType() == this)
				return VHDLCompatibilityResult.Yes;

			//if (Values.Any(x => x.StartsWith("'")) && type == VHDLBuiltinTypeChar.Instance)
			//	return true;

			if (type is VHDLCharLiteralType clt && Values.Contains("'" + clt.Literal.Value + "'"))
				return VHDLCompatibilityResult.Yes;

			return VHDLCompatibilityResult.No;
		}
		public override VHDLType GetBaseType()
		{
			return this;
		}
	}
	enum VHDLRangeDirection
	{
		To,
		DownTo,
	}
	class VHDLRange
	{
		public VHDLRange() { }
		public VHDLRange(VHDLExpression start, VHDLRangeDirection dir, VHDLExpression end)
		{
			Start = start;
			End = end;
			Direction = dir;
		}
		public VHDLExpression Start { get; set; } = null;
		public VHDLExpression End { get; set; } = null;
		public VHDLRangeDirection Direction { get; set; }

		public VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = Start.GetClassifiedText();
			if (End != null)
			{
				text.Add(Direction == VHDLRangeDirection.To ? " to " : " downto ", "keyword");
				text.Add(End.GetClassifiedText());
			}
			return text;
		}
		// Get the expression of the number of values in that range, type is necessary for enums ('U' DOWNTO '0')
		// It is an expression cause it can be variable (eg. "function f(constant width : INTEGER) return std_logic_vector(width-1 DOWNTO 0)")
		public VHDLExpression Count(VHDLType t)
		{
			if (t.GetBaseType() is VHDLEnumerationType)
				return null;

			if (Start == null || End == null)
				return null;

			if (Direction == VHDLRangeDirection.To)
			{
				VHDLAddExpression endPlus1 = new VHDLAddExpression(Start.AnalysisResult, End.Span,
					End,
					new VHDLIntegerLiteral(Start.AnalysisResult, Start.Span, 1, "1"));
				return new VHDLSubtractExpression(Start.AnalysisResult, Start.Span.Union(End.Span), endPlus1, Start);
			}
			else
			{
				VHDLAddExpression startPlus1 = new VHDLAddExpression(Start.AnalysisResult, Start.Span,
					Start,
					new VHDLIntegerLiteral(Start.AnalysisResult, Start.Span, 1, "1"));
				return new VHDLSubtractExpression(Start.AnalysisResult, Start.Span.Union(End.Span), startPlus1, End);
			}
		}

		public VHDLType DeduceType()
		{
			return (Start ?? End)?.Evaluate(new EvaluationContext())?.Type;
		}

		public bool TryGetIntegerRange(out long start, out long end)
		{
			start = 0;
			end = 0;
			VHDLEvaluatedExpression estart = Start?.Evaluate(new EvaluationContext());
			VHDLEvaluatedExpression eend = End?.Evaluate(new EvaluationContext());
			long? iStart = Direction == VHDLRangeDirection.To ? (estart.Result as VHDLIntegerLiteral)?.Value : (eend.Result as VHDLIntegerLiteral)?.Value;
			long? iEnd = Direction == VHDLRangeDirection.To ? (eend.Result as VHDLIntegerLiteral)?.Value : (estart.Result as VHDLIntegerLiteral)?.Value;
			if (iStart == null || iEnd == null)
				return false;

			start = iStart.Value;
			end = iEnd.Value;
			return true;
		}
		public VHDLCompatibilityResult IsOutOfBound(VHDLExpression e)
		{
			VHDLEvaluatedExpression estart = Start?.Evaluate(new EvaluationContext());
			VHDLEvaluatedExpression eend = End?.Evaluate(new EvaluationContext());
			long? iStart = Direction == VHDLRangeDirection.To ? (estart.Result as VHDLIntegerLiteral)?.Value : (eend.Result as VHDLIntegerLiteral)?.Value;
			long? iEnd = Direction == VHDLRangeDirection.To ? (eend.Result as VHDLIntegerLiteral)?.Value : (estart.Result as VHDLIntegerLiteral)?.Value;
			if (iStart == null || iEnd == null)
				return VHDLCompatibilityResult.Unsure;

			VHDLEvaluatedExpression ee = e?.Evaluate(new EvaluationContext());
			if (ee.Result is VHDLIntegerLiteral l)
			{
				if (l.Value >= iStart.Value && l.Value <= iEnd.Value)
					return VHDLCompatibilityResult.Yes;
				else
					return VHDLCompatibilityResult.No;
			}

			return VHDLCompatibilityResult.Unsure;
		}
	}
	abstract class VHDLAbstractArrayType
		: VHDLType
	{
		public abstract VHDLType ElementType { get; }
		public virtual bool IsConstrained => IndexTypes.All(x => !(x is VHDLUnconstrainedType));
		public abstract IEnumerable<VHDLType> IndexTypes { get; }
		public virtual int Dimension => IndexTypes.Count();
		// Return the range of the given index type, or null if that index is unconstrained
		public virtual VHDLRange GetIndexRange(int i)
		{
			VHDLType t = IndexTypes.ElementAt(i).Dereference();
			if (t is VHDLScalarType st)
			{
				return st.Range;
			}
			else if (t is VHDLUnconstrainedType ut)
			{
				return null;
			}
			throw new Exception("VHDLAbstractArrayType.GetRange cannot find range");
		}
		// Return the type of the given index type
		public abstract VHDLType GetIndexType(int i);

		protected VHDLRange GetRange(VHDLType t)
		{
			if (t is VHDLReferenceType)
				return GetRange(t.Dereference());
			if (t is VHDLScalarType)
				return (t as VHDLScalarType).Range;

			return null;
		}
		public override VHDLCompatibilityResult IsCompatible(VHDLType type)
		{
			type = type.Dereference();
			// type a is array (NATURAL range <>) of std_logic;
			// std_logic_vector(1 downto 0) <= std_logic_vector(2 downto 0) // bad
			// std_logic_vector(1 downto 0) <= std_logic_vector(1 downto 0) // good
			// 
			VHDLArrayType baseType1 = GetBaseType() as VHDLArrayType;

			if (type is VHDLStringLiteralType slt)
			{
				if (IndexTypes.Count() != 1)
					return VHDLCompatibilityResult.No;

				string s = (slt.Literal as VHDLStringLiteral)?.Value ??
					(slt.Literal as VHDLHexStringLiteral)?.ToStringLiteral()?.Value ??
					(slt.Literal as VHDLOctalStringLiteral)?.ToStringLiteral()?.Value ??
					(slt.Literal as VHDLBinaryStringLiteral)?.Value;

				if (s == null)
					return VHDLCompatibilityResult.No;

				foreach (char c in s)
					if (baseType1.ElementType.IsCompatible(new VHDLCharLiteralType(new VHDLCharacterLiteral(null, new Span(), c))) == VHDLCompatibilityResult.No)
						return VHDLCompatibilityResult.No;

				// Try to check if size match
				VHDLType t1 = IndexTypes.First();
				VHDLRange r1 = GetRange(t1);
				VHDLEvaluatedExpression count1 = r1?.Count(t1)?.Evaluate(new EvaluationContext());
				if (count1?.Result is VHDLIntegerLiteral l1 && l1.Value != s.Length)
					return VHDLCompatibilityResult.No;

				return VHDLCompatibilityResult.Yes;
			}

			VHDLArrayType baseType2 = type.GetBaseType() as VHDLArrayType;

			if (baseType1 == null || baseType2 == null)
				return VHDLCompatibilityResult.No;

			// Will do for now
			// In fact we should evaluate function return values, and it should match the size of this
			if (baseType2 != baseType1)
				return VHDLCompatibilityResult.No;


			if (type is VHDLAbstractArrayType aat && baseType2 == baseType1)
			{
				// Same array type, need to check the size is the same
				if (aat.IndexTypes.Count() == 1)
				{
					VHDLType t1 = IndexTypes.First();
					VHDLType t2 = aat.IndexTypes.First();

					// Can happen in function eg. function "and" (l, r : std_logic_vector) alias lv : std_logic_vector( 1 to l'LENGTH ) is l;
					if (aat is VHDLArrayType)
						return VHDLCompatibilityResult.Unsure;

					VHDLRange r1 = GetRange(t1);
					VHDLRange r2 = GetRange(t2);
					if (r1 == null || r2 == null)
						return VHDLCompatibilityResult.Unsure;

					VHDLEvaluatedExpression count1 = r1.Count(t1)?.Evaluate(new EvaluationContext());
					VHDLEvaluatedExpression count2 = r2.Count(t2)?.Evaluate(new EvaluationContext());
					if (count1.Result is VHDLIntegerLiteral l1 && count2.Result is VHDLIntegerLiteral l2)
						return l1.Value == l2.Value ? VHDLCompatibilityResult.Yes : VHDLCompatibilityResult.No;

					// else we don't know, these are complex expressions
				}
			}
			else if (type is VHDLArraySliceType ast && baseType2 == baseType1)
			{
				// Same array type, need to check the size is the same
				if (baseType1.IndexTypes.Count() == 1)
				{
					VHDLType t1 = IndexTypes.First();
					VHDLType t2 = (ast.ArrayType as VHDLAbstractArrayType).IndexTypes.First();

					VHDLRange r1 = GetRange(t1);
					VHDLRange r2 = ast.Range;
					if (r1 == null || r2 == null)
						return VHDLCompatibilityResult.No;

					VHDLEvaluatedExpression count1 = r1.Count(t1)?.Evaluate(new EvaluationContext());
					VHDLEvaluatedExpression count2 = r2.Count(t2)?.Evaluate(new EvaluationContext());
					if (count1.Result is VHDLIntegerLiteral l1 && count2.Result is VHDLIntegerLiteral l2)
						return l1.Value == l2.Value ? VHDLCompatibilityResult.Yes : VHDLCompatibilityResult.No;

					// else we don't know, these are complex expressions
				}
			}

			return VHDLCompatibilityResult.Unsure;
		}
		public override bool IsCastable(VHDLType t)
		{
			VHDLArrayType arrayType1 = GetBaseType() as VHDLArrayType;
			VHDLArrayType arrayType2 = t.GetBaseType() as VHDLArrayType;
			if (arrayType1 == null || arrayType2 == null)
				return false;

			if (arrayType1.ElementType.IsCompatible(arrayType2.ElementType) == VHDLCompatibilityResult.Yes)
				return true;

			return false;
		}
	}
	class VHDLArrayType
		: VHDLAbstractArrayType
	{
		public VHDLArrayType(VHDLType elementType, IEnumerable<VHDLType> indexTypes)
		{
			m_elementType = elementType;
			m_indexTypes = indexTypes;
		}
		private IEnumerable<VHDLType> m_indexTypes = null;
		public override IEnumerable<VHDLType> IndexTypes => m_indexTypes;

		private VHDLType m_elementType = null;
		public override VHDLType ElementType => m_elementType;

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			if (Declaration != null)
			{
				text.Add(Declaration.GetClassifiedName());
				if (IndexTypes.Count() > 0 && IsConstrained)
				{
					text.Add("(");
					foreach (VHDLType type in IndexTypes.Take(IndexTypes.Count() - 1))
					{
						text.Add(type.GetClassifiedText());
						text.Add(", ");
					}
					text.Add(IndexTypes.Last().GetClassifiedText());
					text.Add(")");
				}
				return text;
			}
			else
			{
				text.Add("array ", "keyword");
				if (IndexTypes.Count() > 0)
				{
					text.Add("(");
					foreach (VHDLType type in IndexTypes.Take(IndexTypes.Count() - 1))
					{
						text.Add(type.GetClassifiedText());
						text.Add(", ");
					}
					text.Add(IndexTypes.Last().GetClassifiedText());
					text.Add(")");
				}
				else
					text.Add("()");

				text.Add(" of ", "keyword");
				text.Add(ElementType.GetClassifiedText());
				return text;
			}
		}
		public override VHDLType GetBaseType()
		{
			return this;
		}

		public override VHDLType GetIndexType(int i)
		{
			VHDLType t = IndexTypes.ElementAt(i).Dereference();
			if (t is VHDLScalarType st)
			{
				return st.GetBaseType();
			}
			else if (t is VHDLUnconstrainedType ut)
			{
				return ut.Type;
			}
			throw new Exception("VHDLArrayType.GetIndexType failed");
		}
	}

	// Used to constrain the indices of an array
	class VHDLIndexConstrainedType
		: VHDLAbstractArrayType
	{
		public VHDLIndexConstrainedType(VHDLType arrayType, IEnumerable<VHDLType> indexTypes)
		{
			ArrayType = arrayType;
			m_indexTypes = indexTypes;
		}
		// Must be an unconstrained array
		public VHDLType ArrayType { get; set; } = null;
		private IEnumerable<VHDLType> m_indexTypes = null;
		public override IEnumerable<VHDLType> IndexTypes => m_indexTypes;
		public override VHDLType ElementType => (ArrayType.Dereference() as VHDLArrayType).ElementType;
		public override VHDLClassifiedText GetClassifiedText()
		{
			try
			{
				VHDLClassifiedText text = ArrayType?.GetClassifiedText();
				if (text == null)
					return null;
				if (IndexTypes.Count() > 0)
				{
					text.Add(" (");
					foreach (VHDLType type in IndexTypes.Take(IndexTypes.Count() - 1))
					{
						text.Add(type.GetClassifiedText());
						text.Add(", ");
					}
					text.Add(IndexTypes.Last().GetClassifiedText());
					text.Add(")");
				}
				else
					text.Add("()");
				return text;
			}
			catch (Exception e)
			{
				return null;
			}
		}
		private VHDLRange GetRange(VHDLType arrayType, int i)
		{
			if (arrayType is VHDLArrayType)
			{
				return GetRange((arrayType as VHDLArrayType).IndexTypes.ElementAtOrDefault(i));
			}
			else if (arrayType is VHDLIndexConstrainedType)
			{
				// First we try to get a range on the index type
				VHDLType t = (arrayType as VHDLIndexConstrainedType).IndexTypes.ElementAtOrDefault(i);
				return GetRange(t);
				// /!\ Actually, I think VHDLIndexConstrainedType force to constrain any type, so it's not possible to have a type without range here
				//if (t == null) // if not found, we try to find a range on the base array type
				//	GetRange((arrayType as VHDLIndexConstrainedType).ArrayType, i);
			}
			else if (arrayType is VHDLReferenceType)
				return GetRange((arrayType as VHDLReferenceType).ResolvedType, i);
			return null;
		}

		public override VHDLType GetBaseType()
		{
			return ArrayType.GetBaseType();
		}

		public override VHDLType GetIndexType(int i)
		{
			return (ArrayType.Dereference() as VHDLArrayType)?.GetIndexType(i);
		}
	}
	class VHDLScalarType
		: VHDLType
	{
		public VHDLScalarType(bool isStrong)
		{
			IsStrong = isStrong;
		}
		// This is non null in subtypes, null in types
		public VHDLType Type { get; set; } = null;
		public VHDLRange Range { get; set; } = null;
		public bool IsSubtype { get { return Type != null; } }
		// True if this is a strong type. Strong types are not compatible between each other
		public bool IsStrong { get; protected set; }
		public List<string> Units { get; set; } = new List<string>();
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			if (Type != null)
			{
				text.Add(Type.GetClassifiedText());
				text.Add(" ");
			}
			if (Range != null)
			{
				text.Add("range ", "keyword");
				text.Add(Range.GetClassifiedText());
			}
			return text;
		}

		public override VHDLCompatibilityResult IsCompatible(VHDLType type)
		{
			VHDLType baseType = GetBaseType();
			VHDLType baseType2 = type.GetBaseType();
			if (baseType == null) // only happen if name resolve error or...
				return VHDLCompatibilityResult.No;

			if (baseType == baseType2)
				return VHDLCompatibilityResult.Yes;

			if (VHDLType.IsInteger(baseType) && baseType2 == VHDLBuiltinTypeInteger.Instance)
				return VHDLCompatibilityResult.Yes;
			if (VHDLType.IsReal(baseType) && baseType2 == VHDLBuiltinTypeReal.Instance)
				return VHDLCompatibilityResult.Yes;

			return VHDLCompatibilityResult.No;
		}
		public override VHDLType GetBaseType()
		{
			if (IsStrong)
				return this;
			else
			{
				if (IsSubtype)
					return Type.GetBaseType();
				else
					return Range.DeduceType().GetBaseType();
			}
		}

		public VHDLRange GetRange()
		{
			if (Range != null)
				return Range;
			if (Type != null)
			{
				VHDLType t = Type.Dereference();
				if (t is VHDLScalarType st)
					return st.GetRange();
				if (t is VHDLAbstractArrayType aat && aat.Dimension == 1)
					return aat.GetIndexRange(0);
			}
			return null;
		}
	}
	// Only in arrays
	class VHDLUnconstrainedType
		: VHDLType
	{
		public VHDLReferenceType Type { get; set; } = null;

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = Type?.GetClassifiedText() ?? new VHDLClassifiedText();
			text.Add(" range ", "keyword");
			text.Add("<>");
			return text;
		}

		public override VHDLCompatibilityResult IsCompatible(VHDLType type)
		{
			// This happens in functions eg. "FUNCTION resolved ( s : std_ulogic_vector ) ... s(i) ..." (std_ulogic_vector is unconstrained)
			return Type.IsCompatible(type);
		}
		public override VHDLType GetBaseType()
		{
			return Type?.GetBaseType();
		}
	}
	class VHDLReferenceType
		: VHDLType
	{
		public VHDLReferenceType() { }
		public VHDLReferenceType(VHDLDeclaration decl)
		{
			m_declaration = decl;
		}
		public VHDLReferenceType(VHDLReferenceExpression expr)
		{
			Expression = expr;
		}
		public VHDLReferenceExpression Expression { get; set; } = null;
		private VHDLDeclaration m_declaration = null;
		public VHDLDeclaration Declaration
		{
			get
			{
				if (m_declaration == null && Expression != null)
					m_declaration = Expression.Declaration;
				return m_declaration;
			}
		}
		public VHDLType ResolvedType
		{
			get
			{
				if (Declaration == null)
					return null;
				if (Declaration is VHDLTypeDeclaration)
					return (Declaration as VHDLTypeDeclaration).Type;
				if (Declaration is VHDLSubTypeDeclaration)
					return (Declaration as VHDLSubTypeDeclaration).Type;
				return null;
			}
		}

		public override VHDLClassifiedText GetClassifiedText()
		{
			return Declaration?.GetClassifiedName();
		}

		public override VHDLCompatibilityResult IsCompatible(VHDLType type)
		{
			if (ResolvedType == null)
				return VHDLCompatibilityResult.No;

			return ResolvedType.IsCompatible(type);
		}

		public override VHDLType Dereference()
		{
			return ResolvedType;
		}
		public override VHDLType GetBaseType()
		{
			return ResolvedType?.GetBaseType();
		}

		public override bool IsCastable(VHDLType t)
		{
			return ResolvedType?.IsCastable(t) == true;
		}
	}
	class VHDLRecordType
		: VHDLType
	{
		public VHDLDeclaration Declaration { get; set; } = null;
		public IEnumerable<VHDLRecordElementDeclaration> Fields => Declaration.Children.OfType<VHDLRecordElementDeclaration>();

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText();
			text.Add(" record ", "keyword");
			return text;
		}

		public override VHDLCompatibilityResult IsCompatible(VHDLType type)
		{
			return type.GetBaseType() == this ? VHDLCompatibilityResult.Yes : VHDLCompatibilityResult.No;
		}
		public override VHDLType GetBaseType()
		{
			return this;
		}

	}
	class VHDLArraySliceType
		: VHDLAbstractArrayType
	{
		public VHDLArraySliceType(VHDLType arrayType, VHDLRange range)
		{
			ArrayType = arrayType;
			Range = range;
			VHDLScalarType st = new VHDLScalarType(false);
			st.Range = range;
			m_indexTypes = new List<VHDLType>() { st };
		}
		// Must be an unconstrained array
		public VHDLType ArrayType { get; } = null;
		public VHDLRange Range { get; } = null;

		public override VHDLType ElementType => (GetBaseType() as VHDLArrayType)?.ElementType;

		private List<VHDLType> m_indexTypes = new List<VHDLType>();
		public override IEnumerable<VHDLType> IndexTypes => m_indexTypes;

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLDeclaration decl = ArrayType.GetBaseType()?.Declaration;
			if (decl != null)
			{
				VHDLClassifiedText text = new VHDLClassifiedText();
				text.Add(decl.GetClassifiedName());
				text.Add("(");
				text.Add(Range.GetClassifiedText());
				text.Add(")");
				return text;
			}
			return new VHDLClassifiedText("<array slice type>");
		}

		public override VHDLType GetBaseType()
		{
			return ArrayType.GetBaseType();
		}

		public override VHDLType GetIndexType(int i)
		{
			return (ArrayType.Dereference() as VHDLArrayType)?.GetIndexType(i);
		}
	}
}
