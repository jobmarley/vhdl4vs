/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

using vhdl4vs;

namespace vhdl4vsTests
{
	[TestClass]
	public class TypeCompatibilityTests
	{
		static VHDLEnumerationType CreateEnum(IEnumerable<string> values)
		{
			VHDLEnumerationType type = new VHDLEnumerationType();
			List<VHDLEnumerationElement> elements = new List<VHDLEnumerationElement>();
			foreach (string s in values)
			{
				if (s.StartsWith("'"))
					elements.Add(new VHDLCharEnumerationValue(type, new VHDLCharacterLiteral(null, new Span(), s[1])));
				else
					elements.Add(new VHDLNameEnumerationValue(type, new VHDLEnumerationValueDeclaration(null, null, "TRUE", type)));
			}
			type.Values = elements;
			return type;
		}
		[ClassInitialize]
		public static void TestInit(TestContext context)
		{
			AnalysisResult = new AnalysisResult();
			AnalysisResult.BooleanType = CreateEnum(new string[]{ "TRUE", "FALSE" });


			IntegerType = CreateRangeType(int.MinValue + 1, int.MaxValue); // -2147483647 to 2147483647
			NaturalType = CreateRangeType(0, int.MaxValue); // 0 to 2147483647
		}

		private static AnalysisResult AnalysisResult { get; set; } = null;
		private static VHDLScalarType IntegerType { get; set; } = null;
		private static VHDLScalarType NaturalType { get; set; } = null;

		private VHDLIntegerLiteral Integer(int i)
		{
			return new VHDLIntegerLiteral(AnalysisResult, new Span(), i, i.ToString());
		}
		private VHDLRealLiteral Real(double d)
		{
			return new VHDLRealLiteral(AnalysisResult, new Span(), d, d.ToString());
		}
		private VHDLCharacterLiteral Char(char c)
		{
			return new VHDLCharacterLiteral(AnalysisResult, new Span(), c);
		}

		private void AssertCompatible(VHDLType t1, VHDLType t2, VHDLConstantValue v1 = null, VHDLConstantValue v2 = null)
		{
			Assert.IsTrue(VHDLType.AreCompatible(t1, t2, v1, v2) == VHDLCompatibilityResult.Yes,
				$"'{t1.GetClassifiedText().Text}' should be compatible with '{t2.GetClassifiedText().Text}'");
		}
		private void AssertNotCompatible(VHDLType t1, VHDLType t2, VHDLConstantValue v1 = null, VHDLConstantValue v2 = null)
		{
			Assert.IsFalse(VHDLType.AreCompatible(t1, t2, v1, v2) == VHDLCompatibilityResult.Yes,
				$"'{t1.GetClassifiedText().Text}' should not be compatible with '{t2.GetClassifiedText().Text}'");
		}
		[TestMethod]
		public void IntegerCompatibilityTests()
		{
			// type t1 is 0 to 1000000;
			VHDLScalarType integer_strong_type_1 = new VHDLScalarType(true);
			integer_strong_type_1.Range = new VHDLRange(Integer(0), VHDLRangeDirection.To, Integer(1000000));

			// type t2 is 0 to 1000000;
			VHDLScalarType integer_strong_type_2 = new VHDLScalarType(true);
			integer_strong_type_2.Range = new VHDLRange(Integer(0), VHDLRangeDirection.To, Integer(1000000));

			// subtype t3 is t1;
			VHDLScalarType subtype1 = new VHDLScalarType(false);
			subtype1.Type = integer_strong_type_1;

			// subtype t4 is t1 range 50 downto 0;
			VHDLScalarType subtype2 = new VHDLScalarType(false);
			subtype2.Type = integer_strong_type_1;
			subtype2.Range = new VHDLRange(Integer(50), VHDLRangeDirection.DownTo, Integer(0));

			// for i in 60 downto 0
			VHDLScalarType weak_range_type = new VHDLScalarType(false);
			weak_range_type.Range = new VHDLRange(Integer(60), VHDLRangeDirection.DownTo, Integer(0));


			// yes
			AssertCompatible(integer_strong_type_1, VHDLBuiltinTypeInteger.Instance);

			// no
			AssertNotCompatible(integer_strong_type_1, integer_strong_type_2);

			// yes
			AssertCompatible(integer_strong_type_1, subtype1);

			// no
			AssertNotCompatible(integer_strong_type_2, subtype1);

			// yes
			AssertCompatible(integer_strong_type_1, subtype2);

			// yes
			AssertCompatible(integer_strong_type_1, weak_range_type);
			AssertCompatible(subtype2, weak_range_type);
			AssertNotCompatible(VHDLBuiltinTypeInteger.Instance, weak_range_type);

		}
		[TestMethod]
		public void RealCompatibilityTests()
		{
			VHDLScalarType real_strong_type_1 = new VHDLScalarType(true);
			real_strong_type_1.Range = new VHDLRange(Real(0), VHDLRangeDirection.To, Real(1000000));
			VHDLScalarType real_strong_type_2 = new VHDLScalarType(true);
			real_strong_type_2.Range = new VHDLRange(Real(0), VHDLRangeDirection.To, Real(1000000));
			VHDLScalarType real_weak_type_1 = new VHDLScalarType(false);
			real_weak_type_1.Type = real_strong_type_1;
			VHDLScalarType integer_strong_type_1 = new VHDLScalarType(true);
			integer_strong_type_1.Range = new VHDLRange(Integer(0), VHDLRangeDirection.To, Integer(1000000));

			// integer_strong_type_1 <= builtin int
			AssertCompatible(real_strong_type_1, VHDLBuiltinTypeInteger.Instance);

			// integer_strong_type_1 <= integer_strong_type_2
			AssertNotCompatible(real_strong_type_1, real_strong_type_2);

			// integer_strong_type_1 <= integer_weak_type_1
			AssertCompatible(real_strong_type_1, real_weak_type_1);

			// integer_strong_type_2 <= integer_weak_type_1
			AssertNotCompatible(real_strong_type_2, real_weak_type_1);

			AssertNotCompatible(real_strong_type_1, integer_strong_type_1);
		}


		[TestMethod]
		public void EnumCompatibilityTests()
		{
			VHDLEnumerationType enum_type_1 = CreateEnum(new string[] { "'0'", "'1'", "'x'", "baba", "bobo" });
			VHDLEnumerationType enum_type_2 = CreateEnum(new string[] { "'0'", "'1'", "'x'", "baba", "bobo" });

			VHDLTypeDeclaration enum_type_1_decl = new VHDLTypeDeclaration(AnalysisResult, null, null, "enum_type_1", null);
			enum_type_1_decl.Type = enum_type_1;
			VHDLEnumerationValueDeclaration enum_type_1_value_decl = (enum_type_1.Values[3] as VHDLNameEnumerationValue).Declaration;

			var char0 = Char('0');
			var evaluatedChar0 = char0.Evaluate(new EvaluationContext());
			var charz = Char('z');
			var evaluatedCharz = charz.Evaluate(new EvaluationContext());

			// no
			AssertNotCompatible(enum_type_1, VHDLBuiltinTypeInteger.Instance);

			// yes
			AssertCompatible(enum_type_1, evaluatedChar0.Type, null, evaluatedChar0.Result);

			// no
			AssertNotCompatible(enum_type_1, evaluatedCharz.Type, null, evaluatedCharz.Result);

			// yes
			AssertCompatible(enum_type_1, enum_type_1_value_decl.Type);
		}

		static VHDLIndexConstrainedType IndexConstrained(int a, int b, VHDLArrayType t)
		{
			return new VHDLIndexConstrainedType(t, new VHDLType[] { CreateRangeType(a, b) });
		}

		// scalar can be a strong type (INTEGER -2147483647 to 2147483647), or a subtype (NATURAL is INTEGER 0 to 2147483647)
		static VHDLScalarType CreateRangeType(int a, int b, VHDLType t = null)
		{
			VHDLScalarType st = new VHDLScalarType(t == null);
			st.Range = new VHDLRange(new VHDLIntegerLiteral(a), a < b ? VHDLRangeDirection.To : VHDLRangeDirection.DownTo, new VHDLIntegerLiteral(b));
			st.Type = t;
			return st;
		}
		static VHDLScalarType CreateRangeType(float a, float b, VHDLType t = null)
		{
			VHDLScalarType st = new VHDLScalarType(t == null);
			st.Range = new VHDLRange(new VHDLRealLiteral(a), a < b ? VHDLRangeDirection.To : VHDLRangeDirection.DownTo, new VHDLRealLiteral(b));
			st.Type = t;
			return st;
		}
		static VHDLArraySliceType CreateSliceType(int a, int b, VHDLType t)
		{
			return new VHDLArraySliceType(t, new VHDLRange(new VHDLIntegerLiteral(a), a < b ? VHDLRangeDirection.To : VHDLRangeDirection.DownTo, new VHDLIntegerLiteral(b)));
		}
		[TestMethod]
		public void ArrayCompatibilityTests()
		{
			VHDLEnumerationType enum_type_1 = CreateEnum(new string[] { "'0'", "'1'", "'x'", "baba", "bobo" });
			VHDLArrayType unconstrainedArrayType1 = new VHDLArrayType(enum_type_1, new VHDLType[] { new VHDLUnconstrainedType(IntegerType) });
			VHDLArrayType arrayType2 = new VHDLArrayType(enum_type_1, new VHDLType[] { CreateRangeType(31, 0) });
			VHDLArrayType arrayType3 = new VHDLArrayType(enum_type_1, new VHDLType[] { CreateRangeType(31, 0) });

			// no
			AssertNotCompatible(arrayType2, arrayType3);

			// index constrained
			VHDLIndexConstrainedType arrayType4 = IndexConstrained(31, 0, unconstrainedArrayType1);
			VHDLIndexConstrainedType arrayType5 = IndexConstrained(31, 0, unconstrainedArrayType1);
			VHDLIndexConstrainedType arrayType6 = IndexConstrained(0, 31, unconstrainedArrayType1);

			AssertCompatible(arrayType4, arrayType5);

			AssertCompatible(arrayType4, arrayType6);

			// slices
			VHDLArraySliceType arrayType7 = CreateSliceType(0, 15, arrayType5);
			VHDLArraySliceType arrayType8 = CreateSliceType(0, 15, arrayType6);
			VHDLArraySliceType arrayType9 = CreateSliceType(1, 15, arrayType6);

			AssertCompatible(arrayType7, arrayType8);
			AssertNotCompatible(arrayType8, arrayType9);
		}
	}
}
