using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompany.LanguageServices.VHDL
{
	// This is something that expression can be evaluated to.
	// This represent constant values that can be directly used
	class VHDLConstantValue
	{
		public virtual VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText();
		}
	}
	class VHDLBooleanValue
		: VHDLConstantValue
	{
		public VHDLBooleanValue(bool v)
		{
			Value = v;
		}
		public bool Value { get; set; }
		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText(Value ? "TRUE" : "FALSE", "vhdl.constant");
		}
	}
	class VHDLIntegerValue
		: VHDLConstantValue
	{
		public VHDLIntegerValue(long v)
		{
			Value = v;
		}
		public long Value { get; set; }
		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText(Value.ToString());
		}
	}
	class VHDLRealValue
		: VHDLConstantValue
	{
		public VHDLRealValue(double v)
		{
			Value = v;
		}
		public double Value { get; set; }
	}
	class VHDLCharValue
		: VHDLConstantValue
	{
		public VHDLCharValue(char v)
		{
			Value = v;
		}
		public char Value { get; set; }
		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText(Value.ToString());
		}
	}
	class VHDLArrayValue
		: VHDLConstantValue
	{
		public VHDLArrayValue(IEnumerable<VHDLConstantValue> v)
		{
			Value = v;
		}
		public IEnumerable<VHDLConstantValue> Value { get; set; }

		public static VHDLArrayValue FromString(string s)
		{
			return new VHDLArrayValue(s.Select(x => new VHDLCharValue(x)));
		}
		public override VHDLClassifiedText GetClassifiedText()
		{
			return new VHDLClassifiedText();
		}
	}
	class VHDLEnumValue
		: VHDLConstantValue
	{
		public VHDLEnumValue(VHDLEnumerationElement v)
		{
			Value = v;
		}
		public VHDLEnumerationElement Value { get; set; }
		public override VHDLClassifiedText GetClassifiedText()
		{
			if (Value is VHDLNameEnumerationValue n)
				return new VHDLClassifiedText(n.Declaration.Name, "vhdl.constant");
			if (Value is VHDLCharEnumerationValue c)
				return new VHDLClassifiedText(c.Literal.Text);
			return new VHDLClassifiedText();
		}
	}
}
