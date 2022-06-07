/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

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
using System.Xml;
using System.Text.Json;

namespace vhdl4vs.VivadoIntegration
{
	public enum ComponentPortType
	{
		Simple,
		Interface,
	}
	public class ComponentPort
	{ 
		public ComponentPort() { }
		public ComponentPort(string name, ComponentPortType type)
		{
			Name = name;
			Type = type;
		}
		public string Name { get; set; }
		public ComponentPortType Type { get; set; } = ComponentPortType.Simple;
	}
	/// <summary>
	/// Interaction logic for HDLComponentControl.xaml
	/// </summary>
	public partial class HDLComponentControl : UserControl
	{
		public HDLComponentControl()
		{
			InitializeComponent();
		}

		public List<ComponentPort> InPorts { get; } = new List<ComponentPort>();
		public List<ComponentPort> OutPorts { get; } = new List<ComponentPort>();

		public static readonly DependencyProperty ComponentNameProperty = DependencyProperty.Register("ComponentName", typeof(string), typeof(HDLComponentControl));
		public string ComponentName
		{
			get { return (string)GetValue(ComponentNameProperty); }
			set { SetValue(ComponentNameProperty, value); }
		}

		ComponentPortType ParseComponentPortType(string s)
		{
			ComponentPortType type = ComponentPortType.Simple;
			if (s == "signal")
				type = ComponentPortType.Simple;
			else if (s == "interface")
				type = ComponentPortType.Interface;
			return type;
		}

		void LoadFromXml(string filepath)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(filepath);
			XmlElement docElem = doc.DocumentElement;

			XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
			nsmgr.AddNamespace("s", "http://www.spiritconsortium.org/XMLSchema/SPIRIT/1685-2009");
			nsmgr.AddNamespace("x", "http://www.xilinx.com");

			int inCount = 0;
			int outCount = 0;
			foreach (XmlNode interfaceNode in docElem.SelectNodes("./s:busInterfaces/s:busInterface", nsmgr))
			{
				string name = interfaceNode.SelectSingleNode("./s:displayName", nsmgr)?.InnerText ??
					interfaceNode.SelectSingleNode("./s:name", nsmgr)?.InnerText;
				string desc = interfaceNode.SelectSingleNode("./s:description", nsmgr)?.InnerText;
				string type = interfaceNode.SelectSingleNode("./s:busType", nsmgr)?.Attributes?.GetNamedItem("spirit:library")?.InnerText;
				string polarity = interfaceNode.SelectSingleNode("./s:parameters/s:parameter[s:name='POLARITY']/s:value", nsmgr)?.InnerText;
				string enabled = interfaceNode.SelectSingleNode("./s:vendorExtensions/x:busInterfaceInfo/x:enablement/x:isEnabled", nsmgr)?.InnerText;
				if (enabled == "false" || name == null || type == null)
					continue;

				string physicalPortName = interfaceNode.SelectSingleNode("./s:portMaps/s:portMap/s:physicalPort/s:name", nsmgr)?.InnerText;
				string direction = docElem.SelectSingleNode(string.Format("./s:model/s:ports/s:port[s:name='{0}']/s:wire/s:direction", physicalPortName), nsmgr)?.InnerText;

				string protocol = interfaceNode.SelectSingleNode("./s:parameters/s:parameter[s:name='PROTOCOL']/s:value", nsmgr)?.Attributes?.GetNamedItem("spirit:id")?.InnerText;
				
				ComponentPort port = new ComponentPort(name, ParseComponentPortType(type));
				if (direction == "out" || protocol == "BUSIFPARAM_VALUE.M_AXI.PROTOCOL")
				{
					OutPorts.Add(port);
					++outCount;
				}
				else // "BUSIFPARAM_VALUE.S_AXI.PROTOCOL"
				{
					InPorts.Add(port);
					++inCount;
				}
			}

			Height = 140 + Math.Max(inCount, outCount) * 30;
			Width = 400;
		}
		void LoadFromJson(JsonElement element)
		{
			int inCount = 0;
			int outCount = 0;

			JsonElement? interfacePorts = element.GetProperty2("interface_ports");
			if (interfacePorts != null)
			{
				foreach (var e in interfacePorts?.EnumerateObject())
				{
					ComponentPort port = new ComponentPort(e.Name, ComponentPortType.Interface);
					if (e.Value.GetProperty2("mode")?.GetString() == "Master")
					{
						OutPorts.Add(port);
						++outCount;
					}
					else
					{
						InPorts.Add(port);
						++inCount;
					}
				}
			}
			JsonElement? ports = element.GetProperty2("ports");
			if (ports != null)
			{
				foreach (var e in ports?.EnumerateObject())
				{
					string direction = e.Value.GetProperty2("direction")?.GetString();
					string polarity = e.Value.GetProperty2("parameters")?.GetProperty2("POLARITY")?.GetProperty2("value")?.GetString();
					ComponentPort port = new ComponentPort(e.Name, ComponentPortType.Simple);
					if (direction == "O")
					{
						OutPorts.Add(port);
						++outCount;
					}
					else
					{
						InPorts.Add(port);
						++inCount;
					}
				}
			}
			Height = 140 + Math.Max(inCount, outCount) * 30;
			Width = 400;
		}
		public void LoadFromJsonEntry(JsonElement e, string genDirectory)
		{
			string name = e.GetProperty2("inst_hier_path")?.GetString();
			
			if (e.GetProperty2("reference_info")?.GetProperty2("ref_type")?.GetString() == "hdl")
			{
				ComponentName = name;
				LoadFromJson(e);
			}
			else if (e.TryGetProperty("xci_name", out JsonElement xciName))
			{
				string xmlPath = System.IO.Path.Combine(genDirectory, "ip", xciName.GetString(), xciName.GetString() + ".xml");
				ComponentName = name;
				LoadFromXml(xmlPath);
			}
		}
	}
}
