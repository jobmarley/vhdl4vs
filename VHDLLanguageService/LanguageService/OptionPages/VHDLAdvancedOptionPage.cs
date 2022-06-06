using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompany.LanguageServices.VHDL
{
	internal class VHDLAdvancedOptionPage
		: DialogPage
	{
        private bool m_EnableFunctionExecution = false;

        [Category("Performance")]
        [DisplayName("Enable function execution")]
        [Description("Enable execution of subprogram code during function call evaluation")]
        public bool EnableFunctionExecution
        {
            get { return m_EnableFunctionExecution; }
            set { m_EnableFunctionExecution = value; }
        }
    }
}
