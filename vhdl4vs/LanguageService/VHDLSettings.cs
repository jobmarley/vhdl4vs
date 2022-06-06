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
