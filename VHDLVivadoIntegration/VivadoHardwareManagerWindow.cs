using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace vhdl4vs.VivadoIntegration
{
	/// <summary>
	/// This class implements the tool window exposed by this package and hosts a user control.
	/// </summary>
	/// <remarks>
	/// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
	/// usually implemented by the package implementer.
	/// <para>
	/// This class derives from the ToolWindowPane class provided from the MPF in order to use its
	/// implementation of the IVsUIElementPane interface.
	/// </para>
	/// </remarks>
	[Guid("d1ec799c-ca78-40fb-86f8-d5ffd074551f")]
	public class VivadoHardwareManagerWindow : ToolWindowPane
	{
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			m_vivado.Dispose();
			m_vivado = null;
		}
		private VivadoHMWindowControl m_windowControl = null;
		private VivadoHMView1 m_view1 = null;
		private VivadoHMView2 m_view2 = null;
		private Vivado.Vivado m_vivado = null;
		/// <summary>
		/// Initializes a new instance of the <see cref="VivadoHardwareManagerWindow"/> class.
		/// </summary>
		public VivadoHardwareManagerWindow() : base(null)
		{
			this.Caption = "Vivado Hardware Manager";

			// This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
			// we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
			// the object returned by the Content property.
			m_windowControl = new VivadoHMWindowControl();
			m_view1 = new VivadoHMView1();
			m_view2 = new VivadoHMView2();
			m_view1.OpenHMButton.Click += (x,y) =>
			{
				m_view1.OpenHMButton.IsEnabled = false;
				_ = Task.Run(() => OpenHMButtonOnClickAsync(x, y));
			};
			m_windowControl.Content = m_view1;
			this.Content = m_windowControl;
			this.ToolBar = new CommandID(VivadoHardwareManagerWindowCommand.CommandSet, VivadoHardwareManagerWindowCommand.VivadoHMWindowToolbarID);
			this.ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

		}

		async Task<bool> VivadoConnectAsync()
		{
			m_vivado = new Vivado.Vivado();
			await m_vivado.OpenHardwareManager();
			await m_vivado.ConnectHardwareServer();
			await m_vivado.OpenHWTarget();

			return true;
		}
		async Task OpenHMButtonOnClickAsync(object sender, RoutedEventArgs e)
		{
			if (await VivadoConnectAsync())
			{
				var serverName = await m_vivado.GetCurrentHWServer();
				var targetName = await m_vivado.GetCurrentHWTarget();
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				m_windowControl.Content = m_view2;
				var item1 = new TreeViewItem();
				item1.Header = serverName;
				m_view2.TreeView1.Items.Add(item1);
				var item2 = new TreeViewItem();
				item2.Header = targetName;
				item1.Items.Add(item2);
			}
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			m_view1.OpenHMButton.IsEnabled = true;
		}

	}
}
