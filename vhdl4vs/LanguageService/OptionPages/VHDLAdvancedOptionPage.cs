/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

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
