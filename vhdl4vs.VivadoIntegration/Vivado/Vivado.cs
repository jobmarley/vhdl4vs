using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace vhdl4vs.VivadoIntegration.Vivado
{
	class TclObject
	{
		private Vivado m_vivado = null;
		public TclObject(Vivado v, string name)
		{
			m_vivado = v;
			Name = name;
		}
		public string GetProperty(string name)
		{
			return m_vivado.GetProperty(Name, name).Result;
		}
		public string[] ListProperties()
		{
			return m_vivado.ListProperties(Name).Result;
		}
		public void SetProperty(string name, string value)
		{

		}

		public string Name { get; }
	}
	internal class Vivado
		: IDisposable
	{
		System.Diagnostics.Process m_process = null;
		TcpClient m_client = null;
		NetworkStream m_networkStream = null;
		StreamReader m_streamReader = null;
		StreamWriter m_streamWriter = null;
		private bool disposedValue;

		public Vivado()
		{
			m_process = new System.Diagnostics.Process();
			m_process.StartInfo.FileName = @"cmd";
			
			string ScriptPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location);
			ScriptPath = System.IO.Path.Combine(ScriptPath, "Vivado", "vivado_server.tcl");
			ScriptPath = ScriptPath.Replace('\\', '/');
			m_process.StartInfo.Arguments = @"/k C:\Xilinx\Vivado\2021.2\bin\vivado.bat -mode tcl " + string.Format("-source \"{0}\"", ScriptPath);
			//p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			m_process.Start();
			// This is necessary to make sure the process is killed when VS closes unexpectedly
			ChildProcessTracker.AddProcess(m_process);

			TcpListener listener = new TcpListener(IPAddress.Loopback, 9900);
			listener.Start();
			m_client = listener.AcceptTcpClient();
			listener.Stop();
			m_networkStream = m_client.GetStream();
			m_streamReader = new StreamReader(m_networkStream);
			m_streamWriter = new StreamWriter(m_networkStream);
		}

		public void OpenProject(string filepath)
		{
			filepath = filepath.Replace('\\', '/');
			m_streamWriter.WriteLine(string.Format("execute open_project {0}", filepath));
			m_streamWriter.Flush();
			string s = m_streamReader.ReadLine();
		}
		public async Task OpenHardwareManager()
		{
			await m_streamWriter.WriteLineAsync("execute open_hw_manager");
			await m_streamWriter.FlushAsync();
			string s = await m_streamReader.ReadLineAsync();
		}
		public async Task<string> ConnectHardwareServer()
		{
			await m_streamWriter.WriteLineAsync("execute connect_hw_server");
			await m_streamWriter.FlushAsync();
			return await m_streamReader.ReadLineAsync();
		}
		public async Task<string> GetProperty(string obj, string prop)
		{
			await m_streamWriter.WriteLineAsync(string.Format("execute get_property {0} {1}", prop, obj));
			await m_streamWriter.FlushAsync();
			return await m_streamReader.ReadLineAsync();
		}
		public async Task<string[]> ListProperties(string obj)
		{
			await m_streamWriter.WriteLineAsync(string.Format("execute list_property {0}", obj));
			await m_streamWriter.FlushAsync();
			return (await m_streamReader.ReadLineAsync()).Split();
		}
		public async Task Exit()
		{
			await m_streamWriter.WriteLineAsync("execute exit");
		}

		public async Task<string> GetCurrentHWServer()
		{
			await m_streamWriter.WriteLineAsync("execute current_hw_server");
			await m_streamWriter.FlushAsync();
			return await m_streamReader.ReadLineAsync();
		}
		public async Task<string> GetCurrentHWTarget()
		{
			await m_streamWriter.WriteLineAsync("execute current_hw_target");
			await m_streamWriter.FlushAsync();
			return await m_streamReader.ReadLineAsync();
		}
		public async Task OpenHWTarget()
		{
			await m_streamWriter.WriteLineAsync("execute open_hw_target");
			await m_streamWriter.FlushAsync();
			await m_streamReader.ReadLineAsync();
		}
		public TclObject CurrentBoard => new TclObject(this, "[current_board]");
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				if (m_process != null)
				{
					m_client.Close();
					m_client.Dispose();
					m_client = null;
					m_process.Kill();
					m_process.Dispose();
					m_process = null;
				}

				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~Vivado()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
