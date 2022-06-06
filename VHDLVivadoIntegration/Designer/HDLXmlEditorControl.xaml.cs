using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.Json;
using System.ComponentModel.Design;

namespace vhdl4vs.VivadoIntegration
{
	/// <summary>
	/// Interaction logic for HDLXmlEditorControl.xaml
	/// </summary>
	public partial class HDLXmlEditorControl : UserControl
	{
		IMenuCommandService m_menuCommandService = null;
		public HDLXmlEditorControl(IMenuCommandService menuCommandServer)
		{
			m_menuCommandService = menuCommandServer;
			InitializeComponent();
			//HDLComponentControl comp = new HDLComponentControl();
			//comp.Ports.Add(new ComponentPort("resetn", ComponentPortType.Simple));
			//comp.Ports.Add(new ComponentPort("zogzog", ComponentPortType.Interface));
			//comp.Width = 400;
			//comp.Height = 400;
			//Canvas.SetTop(comp, 50);
			//Canvas.SetLeft(comp, 50);
			//this.canvas1.Children.Add(comp);
			this.MouseDown += HDLXmlEditorControl_MouseDown;
			this.MouseUp += HDLXmlEditorControl_MouseUp;
			this.MouseMove += HDLXmlEditorControl_MouseMove;
		}

		Point m_moveStart;
		Point m_moveStartCanvas;
		Point m_canvasPosition;
		bool m_mouseMove = false;
		private void HDLXmlEditorControl_MouseMove(object sender, MouseEventArgs e)
		{
			if (m_mouseMove)
			{
				m_canvasPosition = e.GetPosition(this) - m_moveStart + m_moveStartCanvas;
				canvas1.RenderTransform = new TranslateTransform(m_canvasPosition.X, m_canvasPosition.Y);
			}
		}
		private void HDLXmlEditorControl_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (m_mouseMove)
			{
				canvas1.ReleaseMouseCapture();
				m_mouseMove = false;
			}
			else if (e.ChangedButton == MouseButton.Right)
			{
				int HDLDesignerMenuID = 0x133;
				CommandID menuCommandID = new CommandID(VivadoHardwareManagerWindowCommand.CommandSet, HDLDesignerMenuID);
				Point p = PointToScreen(e.GetPosition(this));
				m_menuCommandService.ShowContextMenu(menuCommandID, (int)p.X, (int)p.Y);
			}
		}

		private void HDLXmlEditorControl_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && canvas1.CaptureMouse())
			{
				m_moveStart = e.GetPosition(this);
				m_moveStartCanvas = m_canvasPosition;
				m_mouseMove = true;
			}
		}

		public void LoadFile(string filepath)
		{
			JsonDocument doc = JsonDocument.Parse(new System.IO.FileStream(filepath, System.IO.FileMode.Open, System.IO.FileAccess.Read));
			JsonElement designElem;
			if (!doc.RootElement.TryGetProperty("design", out designElem))
				return;

			string genDirectoryValue = designElem.GetProperty2("design_info")?.GetProperty2("gen_directory")?.GetString();

			string normalizedGenDirectory = null;
			if (genDirectoryValue != null)
				normalizedGenDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filepath), genDirectoryValue));

			JsonElement? componentsElem = designElem.GetProperty2("components");
			if (componentsElem != null)
			{
				int x = 0;
				foreach (var e in componentsElem?.EnumerateObject())
				{
					HDLComponentControl component = new HDLComponentControl();
					component.ComponentName = e.Name;
					component.LoadFromJsonEntry(e.Value, normalizedGenDirectory);
					Canvas.SetLeft(component, x);
					Canvas.SetTop(component, 0);
					x += 450;
					this.canvas1.Children.Add(component);

					//HDLComponentControl component = new HDLComponentControl();
					//component.ComponentName = e.Name;
					//component.Width = 200;
					//component.Height = 200;
					//Canvas.SetLeft(component, x);
					//Canvas.SetTop(component, 0);
					//x += 250;
					//this.canvas1.Children.Add(component);
				}
			}
		}
	}
}
