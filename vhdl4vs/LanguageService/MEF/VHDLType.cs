﻿/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Antlr4.Runtime;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs
{
	struct VHDLCompatibilityResult
	{
		VHDLCompatibilityResult(int v)
		{
			value = v;
		}
		private int value;
		public static readonly VHDLCompatibilityResult No = new VHDLCompatibilityResult(0);
		public static readonly VHDLCompatibilityResult Yes = new VHDLCompatibilityResult(1);
		public static readonly VHDLCompatibilityResult Unsure = new VHDLCompatibilityResult(2);

		public static bool operator ==(VHDLCompatibilityResult r1, VHDLCompatibilityResult r2)
		{
			return r1.value == r2.value;
		}
		public static bool operator !=(VHDLCompatibilityResult r1, VHDLCompatibilityResult r2)
		{
			return r1.value != r2.value;
		}
		public static VHDLCompatibilityResult operator&(VHDLCompatibilityResult r1, VHDLCompatibilityResult r2)
		{
			if (r1 == No || r2 == No)
				return No;
			if (r1 == Unsure || r2 == Unsure)
				return Unsure;
			return Yes;
		}
		public static VHDLCompatibilityResult operator|(VHDLCompatibilityResult r1, VHDLCompatibilityResult r2)
		{
			if (r1 == Yes || r2 == Yes)
				return Yes;
			if (r1 == Unsure || r2 == Unsure)
				return Unsure;
			return No;
		}
		public static bool operator false(VHDLCompatibilityResult r1)
		{
			return r1 == No;
		}
		public static bool operator true(VHDLCompatibilityResult r1)
		{
			return r1 == Yes;
		}
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
			return this == t;
		}

		// Is non null only when the type is the result of a code declaration, type or subtype
		public VHDLDeclaration Declaration { get; set; } = null;

		private static VHDLCompatibilityResult AreCompatibleImpl(VHDLType t1, VHDLType t2, VHDLConstantValue v1, VHDLConstantValue v2)
		{
			t1 = t1?.Dereference();
			t2 = t2?.Dereference();

			if (t1 == null || t2 == null)
				return VHDLCompatibilityResult.No;

			if (t1 == t2)
				return VHDLCompatibilityResult.Yes;

			if (IsInteger(t1) && t1.GetBaseType() != null && t1.GetBaseType() == t2.GetBaseType())
				return VHDLCompatibilityResult.Yes;
			if (IsInteger(t1) && t2 == VHDLBuiltinTypeInteger.Instance)
				return VHDLCompatibilityResult.Yes;

			if (IsReal(t1) && t1.GetBaseType() != null && t1.GetBaseType() == t2.GetBaseType())
				return VHDLCompatibilityResult.Yes;
			if (IsReal(t1) && t2 == VHDLBuiltinTypeReal.Instance)
				return VHDLCompatibilityResult.Yes;

			if (t1 is VHDLAbstractArrayType aat1)
			{
				if (aat1.Dimension == 1 && t2 is VHDLConcatenatedArrayType cat)
				{
					if (AreCompatible(aat1.ElementType, cat.ElementType) == VHDLCompatibilityResult.No)
						return VHDLCompatibilityResult.No;

					VHDLRange r1 = aat1.GetIndexRange(0);
					VHDLEvaluatedExpression count1 = r1?.Count(aat1.GetIndexType(0))?.Evaluate(new EvaluationContext());
					VHDLRange r2 = cat.GetIndexRange(0);
					VHDLEvaluatedExpression count2 = r2?.Count(cat.GetIndexType(0))?.Evaluate(new EvaluationContext());
					if (count1?.Result is VHDLIntegerValue c1 && count2?.Result is VHDLIntegerValue c2)
						return (c1.Value == c2.Value) ? VHDLCompatibilityResult.Yes : VHDLCompatibilityResult.No;

					// Cannot make sure that size match
					return VHDLCompatibilityResult.Unsure;
				}
				if (aat1.Dimension == 1 && t2 is VHDLStringLiteralType slt)
				{
					//string s = (slt.Literal as VHDLStringLiteral)?.Value ??
					//	(slt.Literal as VHDLHexStringLiteral)?.ToStringLiteral()?.Value ??
					//	(slt.Literal as VHDLOctalStringLiteral)?.ToStringLiteral()?.Value ??
					//	(slt.Literal as VHDLBinaryStringLiteral)?.Value;

					//if (s == null)
					//	return VHDLCompatibilityResult.No;

					//foreach (char c in s)
					//	if (AreCompatible(aat1.ElementType, new VHDLCharLiteralType(new VHDLCharacterLiteral(null, new Span(), c))) == VHDLCompatibilityResult.No)
					//		return VHDLCompatibilityResult.No;

					// Try to check if size match
					VHDLRange r1 = aat1.GetIndexRange(0);
					VHDLEvaluatedExpression count1 = r1?.Count(aat1.GetIndexType(0))?.Evaluate(new EvaluationContext());
					if (count1?.Result is VHDLIntegerValue v && v2 is VHDLArrayValue av2)
						return (v.Value == av2.Value.Count()) ? VHDLCompatibilityResult.Yes : VHDLCompatibilityResult.No;

					// Cannot make sure that size match
					return VHDLCompatibilityResult.Unsure;
				}
				if (t2 is VHDLAbstractArrayType aat2 && aat1.GetBaseType() != null && aat1.GetBaseType() == aat2.GetBaseType())
				{
					if (aat1.Dimension != aat2.Dimension)
						return VHDLCompatibilityResult.No;

					// Try to check if size match
					for (int i = 0; i < aat2.Dimension; i++)
					{
						VHDLRange r1 = aat1.GetIndexRange(0);
						VHDLEvaluatedExpression count1 = r1?.Count(aat1.GetIndexType(0))?.Evaluate(new EvaluationContext());
						VHDLRange r2 = aat2.GetIndexRange(0);
						VHDLEvaluatedExpression count2 = r2?.Count(aat2.GetIndexType(0))?.Evaluate(new EvaluationContext());
						if (count1?.Result is VHDLIntegerValue c1 && count2?.Result is VHDLIntegerValue c2)
							return (c1.Value == c2.Value) ? VHDLCompatibilityResult.Yes : VHDLCompatibilityResult.No;
					}
					// Cannot make sure that size match
					return VHDLCompatibilityResult.Unsure;
				}
				if (t2 is VHDLAggregatedType at)
				{
					if (aat1.Dimension > 1)
					{
						VHDLCompatibilityResult result = VHDLCompatibilityResult.Yes;
						for (int i = 1; i < aat1.Dimension; i++)
						{
							VHDLArrayType tmpArrayType = new VHDLArrayType(aat1.ElementType, aat1.IndexTypes.Skip(i));
							result = result && AreCompatible(tmpArrayType, at.ElementType);
						}
						return result;
					}
					else
					{
						if (AreCompatible(aat1.ElementType, at.ElementType) == VHDLCompatibilityResult.No)
							return VHDLCompatibilityResult.No;

						// Try to check if size match
						VHDLRange r1 = aat1.GetIndexRange(0);
						VHDLEvaluatedExpression count1 = r1?.Count(aat1.GetIndexType(0))?.Evaluate(new EvaluationContext());
						VHDLRange r2 = at.Range;
						VHDLEvaluatedExpression count2 = r2?.Count(at.IndexType)?.Evaluate(new EvaluationContext());
						if (count1?.Result is VHDLIntegerValue c1 && count2?.Result is VHDLIntegerValue c2)
							return (c1.Value == c2.Value) ? VHDLCompatibilityResult.Yes : VHDLCompatibilityResult.No;

						// Cannot make sure that size match
						return VHDLCompatibilityResult.Unsure;
					}
				}
				return VHDLCompatibilityResult.No;
			}
			if (t1.GetBaseType() is VHDLEnumerationType et)
			{
				if (t2.GetBaseType() == t1)
					return VHDLCompatibilityResult.Yes;

				if (t2 is VHDLCharLiteralType)
				{
					if (v2 == null)
						return VHDLCompatibilityResult.Unsure;
					if (et.Values.Any(x => x is VHDLCharEnumerationValue cev && (v2 as VHDLCharValue)?.Value == cev.Literal.Value))
						return VHDLCompatibilityResult.Yes;
					else
						return VHDLCompatibilityResult.No;
				}

				return VHDLCompatibilityResult.No;
			}
			if (t1 is VHDLAggregatedType at1 && t2 is VHDLAggregatedType at2)
			{
				if (AreCompatible(at1.ElementType, at2.ElementType) == VHDLCompatibilityResult.No)
					return VHDLCompatibilityResult.No;

				// Try to check if size match
				VHDLRange r1 = at1.Range;
				VHDLEvaluatedExpression count1 = r1?.Count(at1.IndexType)?.Evaluate(new EvaluationContext());
				VHDLRange r2 = at2.Range;
				VHDLEvaluatedExpression count2 = r2?.Count(at2.IndexType)?.Evaluate(new EvaluationContext());
				if (count1?.Result is VHDLIntegerValue c1 && count2?.Result is VHDLIntegerValue c2)
					return (c1.Value == c2.Value) ? VHDLCompatibilityResult.Yes : VHDLCompatibilityResult.No;

				return VHDLCompatibilityResult.Unsure;
			}
			//if (t1 is VHDLScalarType st)
			//{
			//	if (t1.GetBaseType() != null && t1.GetBaseType() == t2.GetBaseType())
			//		return VHDLCompatibilityResult.Yes;
			//	if (t1.GetBaseType() != t1)
			//	return AreCompatible(t1.GetBaseType(), t2.GetBaseType());
			//}
			return VHDLCompatibilityResult.No;
		}
		// Returns true if types are compatibles with eachother
		// AreCompatible(a,b) is always equal to AreCompatible(b, a)
		// Compatiblity is an abstract thing. To some extent it represent assignability.
		// For instance 0 is compatible with NATURAL, because 0 can be assigned to NATURAL.
		// But 0 is also compatible with 1, even though assigning 0 to 1 is impossible.
		public static VHDLCompatibilityResult AreCompatible(VHDLType t1, VHDLType t2, VHDLConstantValue v1 = null, VHDLConstantValue v2 = null)
		{
			return AreCompatibleImpl(t1, t2, v1, v2) || AreCompatibleImpl(t2, t1, v2, v1);
		}
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

		public override VHDLType GetBaseType()
		{
			return this;
		}
	}
	class VHDLCharLiteralType
		: VHDLType
	{
		// Prevent instanciation, just use VHDLBuiltinTypeReal.Instance
		private VHDLCharLiteralType()
		{
		}

		public static readonly VHDLCharLiteralType Instance = new VHDLCharLiteralType();

		public override VHDLType GetBaseType()
		{
			return this;
		}
		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText("char", "vhdl.type");
		}
	}
	class VHDLStringLiteralType
		: VHDLType
	{
		// Prevent instanciation, just use VHDLBuiltinTypeReal.Instance
		private VHDLStringLiteralType()
		{
		}

		public static readonly VHDLStringLiteralType Instance = new VHDLStringLiteralType();

		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText("string", "vhdl.type");
		}
		public override VHDLType GetBaseType()
		{
			return this;
		}
	}
	class VHDLEnumerationElement
	{
		public VHDLEnumerationElement(VHDLType t)
		{
			Type = t;
		}
		public VHDLType Type { get; set; }

		public virtual VHDLClassifiedText GetClassifiedText()
		{
			return null;
		}
	}
	class VHDLNameEnumerationValue
		: VHDLEnumerationElement
	{
		public VHDLNameEnumerationValue(VHDLType t, VHDLEnumerationValueDeclaration d)
			: base(t)
		{
			Declaration = d;
		}
		public VHDLEnumerationValueDeclaration Declaration { get; } = null;

		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText(Declaration.UndecoratedName, "vhdl.constant");
		}
	}
	class VHDLCharEnumerationValue
		: VHDLEnumerationElement
	{
		public VHDLCharEnumerationValue(VHDLType t, VHDLCharacterLiteral l)
			: base(t)
		{
			Literal = l;
		}
		public VHDLCharacterLiteral Literal { get; } = null;
		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText(Literal.Text, "vhdl.string");
		}
	}
	class VHDLEnumerationType
		: VHDLType
	{
		public List<VHDLEnumerationElement> Values { get; set; } = new List<VHDLEnumerationElement>();

		public override VHDLClassifiedText GetClassifiedText()
		{
			if (Declaration?.Name == null)
			{
				var vals = Values.Take(3).Select(x => x.GetClassifiedText());
				if (Values.Count() > 3)
					vals = vals.Append(new VHDLClassifiedText("..."));
				return new VHDLClassifiedText("enum", "vhdl.keyword") + new VHDLClassifiedText("(") + vals.Aggregate((x, y) => x + new VHDLClassifiedText(", ") + y) + new VHDLClassifiedText(")");
			}
			return new VHDLClassifiedText(Declaration.Name, "vhdl.type");
		}

		public override VHDLType GetBaseType()
		{
			return this;
		}

		public int GetIndexOf(VHDLConstantValue v)
		{
			if (v is VHDLEnumValue ev)
				return Values.IndexOf(ev.Value);

			if (v is VHDLCharValue c)
			{
				for (int i = 0; i < Values.Count; i++)
					if (Values[i] is VHDLCharEnumerationValue x && x.Literal.Value == c.Value)
						return i;
			}
			return -1;
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


			VHDLExpression s = null;
			VHDLExpression e = null;

			if (t is VHDLEnumerationType et)
			{
				VHDLEvaluatedExpression evaluatedStart = Start.Evaluate(new EvaluationContext(), t);
				VHDLEvaluatedExpression evaluatedEnd = End.Evaluate(new EvaluationContext(), t);
				if (evaluatedStart != null && evaluatedEnd != null)
				{
					int iStart = et.GetIndexOf(evaluatedStart.Result);
					int iEnd = et.GetIndexOf(evaluatedStart.Result);
					s = new VHDLIntegerLiteral(iStart);
					e = new VHDLIntegerLiteral(iEnd);
				}
			}
			else
			{
				s = Start;
				e = End;
			}

			if (Direction == VHDLRangeDirection.To)
			{
				VHDLAddExpression endPlus1 = new VHDLAddExpression(Start.AnalysisResult, End.Span,
					e,
					new VHDLIntegerLiteral(Start.AnalysisResult, Start.Span, 1, "1"));
				return new VHDLSubtractExpression(Start.AnalysisResult, Start.Span.Union(End.Span), endPlus1, s);
			}
			else
			{
				VHDLAddExpression startPlus1 = new VHDLAddExpression(Start.AnalysisResult, Start.Span,
					s,
					new VHDLIntegerLiteral(Start.AnalysisResult, Start.Span, 1, "1"));
				return new VHDLSubtractExpression(Start.AnalysisResult, Start.Span.Union(End.Span), startPlus1, e);
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
			long? iStart = Direction == VHDLRangeDirection.To ? (estart.Result as VHDLIntegerValue)?.Value : (eend.Result as VHDLIntegerValue)?.Value;
			long? iEnd = Direction == VHDLRangeDirection.To ? (eend.Result as VHDLIntegerValue)?.Value : (estart.Result as VHDLIntegerValue)?.Value;
			if (iStart == null || iEnd == null)
				return false;

			start = iStart.Value;
			end = iEnd.Value;
			return true;
		}
		public VHDLCompatibilityResult IsOutOfBound(VHDLExpression e, EvaluationContext evaluationContext = null)
		{
			if (evaluationContext == null)
				evaluationContext = new EvaluationContext();

			VHDLEvaluatedExpression estart = Start?.Evaluate(evaluationContext);
			VHDLEvaluatedExpression eend = End?.Evaluate(evaluationContext);
			long? iStart = Direction == VHDLRangeDirection.To ? (estart?.Result as VHDLIntegerValue)?.Value : (eend?.Result as VHDLIntegerValue)?.Value;
			long? iEnd = Direction == VHDLRangeDirection.To ? (eend?.Result as VHDLIntegerValue)?.Value : (estart?.Result as VHDLIntegerValue)?.Value;
			if (iStart == null || iEnd == null)
				return VHDLCompatibilityResult.Unsure;

			VHDLEvaluatedExpression ee = e?.Evaluate(evaluationContext);
			if (ee.Result is VHDLIntegerValue l)
			{
				if (l.Value >= iStart.Value && l.Value <= iEnd.Value)
					return VHDLCompatibilityResult.No;
				else
					return VHDLCompatibilityResult.Yes;
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
			else if (t is VHDLEnumerationType et)
			{
				return new VHDLRange(null, VHDLRangeDirection.To, null);
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
		public override bool IsCastable(VHDLType t)
		{
			VHDLArrayType arrayType1 = GetBaseType() as VHDLArrayType;
			VHDLArrayType arrayType2 = t.GetBaseType() as VHDLArrayType;
			if (arrayType1 == null || arrayType2 == null)
				return false;

			if (AreCompatible(arrayType1.ElementType, arrayType2.ElementType) == VHDLCompatibilityResult.Yes)
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
			else if (t is VHDLEnumerationType)
				return t;

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
				VHDLLogger.LogException(e);
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
		public VHDLUnconstrainedType(VHDLType type)
		{
			Type = type;
		}
		public VHDLType Type { get; set; } = null;

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = Type?.GetClassifiedText() ?? new VHDLClassifiedText();
			text.Add(" range ", "keyword");
			text.Add("<>");
			return text;
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
		// The declaration this reference is pointing to
		public VHDLDeclaration ToDeclaration
		{
			get
			{
				// declaration must not be cached, because redoing the deep analysis will update the reference
				return m_declaration ?? Expression?.Declaration;
			}
		}

		public VHDLType ResolvedType
		{
			get
			{
				if (ToDeclaration == null)
					return null;
				if (ToDeclaration is VHDLTypeDeclaration)
					return (ToDeclaration as VHDLTypeDeclaration).Type;
				if (ToDeclaration is VHDLSubTypeDeclaration)
					return (ToDeclaration as VHDLSubTypeDeclaration).Type;
				return null;
			}
		}

		public override VHDLClassifiedText GetClassifiedText()
		{
			return (Declaration ?? ToDeclaration).GetClassifiedName();
		}

		public override VHDLType Dereference()
		{
			return ResolvedType?.Dereference();
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
			return (ArrayType.GetBaseType() as VHDLArrayType)?.GetIndexType(i);
		}
	}
	// This is necessary, for stuff like eg. "array_signal <= ('1', '0', '1', '0');"
	// We need a type that represent an array with possible evaluated value, but doesn't have an underlying array type
	class VHDLAggregatedType
		: VHDLType
	{
		public VHDLAggregatedType(VHDLRange range, VHDLType elementType, VHDLType indexType)
		{
			Range = range;
			ElementType = elementType;
			IndexType = indexType;
		}
		public VHDLRange Range { get; } = null;

		public VHDLType ElementType { get; } = null;
		public VHDLType IndexType { get; } = null;

		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText("<aggregate>");
		}
	}

	class VHDLConcatenatedArrayType
		: VHDLAbstractArrayType
	{
		public VHDLConcatenatedArrayType(VHDLType elementType, VHDLRange range)
		{
			m_elementType = elementType;
			Range = range;
			VHDLScalarType st = new VHDLScalarType(false);
			st.Range = range;
			m_indexTypes = new List<VHDLType>() { st };
		}

		private VHDLType m_elementType = null;
		public override VHDLType ElementType => m_elementType;

		private List<VHDLType> m_indexTypes = new List<VHDLType>();
		public VHDLRange Range { get; } = null;
		public override IEnumerable<VHDLType> IndexTypes => m_indexTypes;

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText("array ", "keyword");
			text.Add("(");
			if (Range != null)
			{
				text.Add(Range.GetClassifiedText());
			}
			else
			{
				text.Add("range ", "keyword");
				text.Add("<>");
			}
			text.Add(") ");
			text.Add(" of ", "keyword");
			text.Add(ElementType.GetClassifiedText());
			return text;
		}

		public override VHDLType GetBaseType()
		{
			return this;
		}

		public override VHDLType GetIndexType(int i)
		{
			return m_indexTypes[i];
		}
	}

	class VHDLAccessType
		: VHDLType
	{
		public VHDLType Type { get; set; } = null;
		public VHDLAccessType(VHDLType type)
		{
			Type = type;
		}
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText("access ", "keyword");
			try
			{
				text.Add(Type.GetClassifiedText());
			}
			catch (Exception e)
			{
				text.Add("<error type>");
			}
			return text;
		}
		public override VHDLType GetBaseType()
		{
			return this;
		}
		public override VHDLType Dereference()
		{
			return this;
		}
	}
	class VHDLFileType
		: VHDLType
	{
		public VHDLType Type { get; set; } = null;
		public VHDLFileType(VHDLType type)
		{
			Type = type;
		}
		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText("file of ", "keyword");
			try
			{
				text.Add(Type.GetClassifiedText());
			}
			catch (Exception e)
			{
				text.Add("<error type>");
			}
			return text;
		}
		public override VHDLType GetBaseType()
		{
			return this;
		}
	}

	// This is a utility type, it should only appears for range attributes
	class VHDLRangeType
		: VHDLType
	{
		public VHDLType IndexType { get; set; } = null;
		public VHDLRangeType(VHDLType indexType)
			: base()
		{
			IndexType = indexType;
		}

		public override VHDLClassifiedText GetClassifiedText()
		{
			VHDLClassifiedText text = new VHDLClassifiedText("range", "keyword");
			return text;
		}
	}
}
