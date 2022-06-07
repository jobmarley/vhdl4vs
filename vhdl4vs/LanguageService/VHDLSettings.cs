/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs
{
	interface IVHDLSettings
	{
		bool EnableFunctionExecution { get; }
		uint PerformanceCounter { get; }
	}
	[Guid("2C19FD80-49A0-4F26-9CD5-98D406CEC0C0")]
	interface SVHDLSettings
	{

	}
	public class VHDLSettingsService
		: SVHDLSettings,
		IVHDLSettings
	{
		private VHDLAdvancedOptionPage m_advancedOptionPage = null;
		private vhdl4vsPackage m_package = null;
		public VHDLSettingsService(vhdl4vsPackage package)
		{
			m_package = package;
		}
		public void Initialize()
		{
			m_advancedOptionPage = (VHDLAdvancedOptionPage)m_package.GetDialogPage(typeof(VHDLAdvancedOptionPage));
		}

		public bool EnableFunctionExecution => m_advancedOptionPage.EnableFunctionExecution;
		public uint PerformanceCounter => m_advancedOptionPage.PerformanceCounter;
	}
}
