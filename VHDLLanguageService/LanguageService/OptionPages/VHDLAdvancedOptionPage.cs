using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs
{
	internal class VHDLAdvancedOptionPage
		: DialogPage
    {
        private bool m_EnableFunctionExecution = false;
        private uint m_PerformanceCounter = 30;

        [Category("Performance")]
        [DisplayName("Enable function execution")]
        [Description("Enable execution of function code during function call evaluation")]
        public bool EnableFunctionExecution
        {
            get { return m_EnableFunctionExecution; }
            set { m_EnableFunctionExecution = value; }
        }

        [Category("Performance")]
        [DisplayName("Code execution performance counter")]
        [Description("The maximum number of steps allowed in a single call")]
        public uint PerformanceCounter
        {
            get { return m_PerformanceCounter; }
            set { m_PerformanceCounter = value; }
        }
    }
}
