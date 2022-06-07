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
		}

		private static AnalysisResult AnalysisResult { get; set; } = null;

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

		private void AssertCompatible(VHDLType t1, VHDLType t2)
		{
			Assert.IsTrue(VHDLType.AreCompatible(t1, t2) == VHDLCompatibilityResult.Yes,
				$"'{t1.GetClassifiedText().Text}' should be compatible with '{t2.GetClassifiedText().Text}'");
		}
		private void AssertNotCompatible(VHDLType t1, VHDLType t2)
		{
			Assert.IsFalse(VHDLType.AreCompatible(t1, t2) == VHDLCompatibilityResult.Yes,
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
			AssertNotCompatible(VHDLBuiltinTypeInteger.Instance, integer_strong_type_1);

			// no
			AssertNotCompatible(integer_strong_type_1, integer_strong_type_2);
			AssertNotCompatible(integer_strong_type_2, integer_strong_type_1);

			// yes
			AssertCompatible(integer_strong_type_1, subtype1);
			AssertCompatible(subtype1, integer_strong_type_1);

			// no
			AssertNotCompatible(integer_strong_type_2, subtype1);
			AssertNotCompatible(subtype1, integer_strong_type_2);

			// yes
			AssertCompatible(integer_strong_type_1, subtype2);
			AssertCompatible(subtype2, integer_strong_type_1);

			// yes
			AssertCompatible(integer_strong_type_1, weak_range_type);
			AssertNotCompatible(weak_range_type, integer_strong_type_1);
			AssertCompatible(subtype2, weak_range_type);
			AssertNotCompatible(weak_range_type, subtype2);
			AssertNotCompatible(VHDLBuiltinTypeInteger.Instance, weak_range_type);
			AssertCompatible(weak_range_type, VHDLBuiltinTypeInteger.Instance);

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
			//AssertCompatible(VHDLBuiltinTypeInteger.Instance, integer_strong_type_1);

			// integer_strong_type_1 <= integer_strong_type_2
			AssertNotCompatible(real_strong_type_1, real_strong_type_2);
			AssertNotCompatible(real_strong_type_2, real_strong_type_1);

			// integer_strong_type_1 <= integer_weak_type_1
			AssertCompatible(real_strong_type_1, real_weak_type_1);
			AssertCompatible(real_weak_type_1, real_strong_type_1);

			// integer_strong_type_2 <= integer_weak_type_1
			AssertNotCompatible(real_strong_type_2, real_weak_type_1);
			AssertNotCompatible(real_weak_type_1, real_strong_type_2);

			AssertNotCompatible(real_strong_type_1, integer_strong_type_1);
			AssertNotCompatible(integer_strong_type_1, real_strong_type_1);
		}


		[TestMethod]
		public void EnumCompatibilityTests()
		{
			VHDLEnumerationType enum_type_1 = CreateEnum(new string[] { "'0'", "'1'", "'x'", "baba", "bobo" });
			VHDLEnumerationType enum_type_2 = CreateEnum(new string[] { "'0'", "'1'", "'x'", "baba", "bobo" });

			VHDLTypeDeclaration enum_type_1_decl = new VHDLTypeDeclaration(AnalysisResult, null, null, "enum_type_1", null);
			enum_type_1_decl.Type = enum_type_1;
			VHDLEnumerationValueDeclaration enum_type_1_value_decl = (enum_type_1.Values[3] as VHDLNameEnumerationValue).Declaration;

			// no
			AssertNotCompatible(enum_type_1, VHDLBuiltinTypeInteger.Instance);
			AssertNotCompatible(VHDLBuiltinTypeInteger.Instance, enum_type_1);

			// yes
			AssertCompatible(enum_type_1, Char('0').Evaluate(null).Type);
			// no
			AssertNotCompatible(Char('0').Evaluate(null).Type, enum_type_1);

			// no
			AssertNotCompatible(enum_type_1, Char('z').Evaluate(null).Type);
			AssertNotCompatible(Char('z').Evaluate(null).Type, enum_type_1);

			// yes
			AssertCompatible(enum_type_1, enum_type_1_value_decl.Type);
			AssertCompatible(enum_type_1_value_decl.Type, enum_type_1);

		}
	}
}
