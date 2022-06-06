using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace vhdl4vs.VivadoIntegration
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class VivadoHardwareManagerWindowCommand
	{
		public const uint cmdidWindowsMedia = 0x100;
		public const int cmdidWindowsMediaOpen = 0x132;
		public const int VivadoHMWindowToolbarID = 0x1000;
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("76a9b486-a820-40d3-b8a2-b6eff436355b");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly AsyncPackage package;

		/// <summary>
		/// Initializes a new instance of the <see cref="VivadoHardwareManagerWindowCommand"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		/// <param name="commandService">Command service to add command to, not null.</param>
		private VivadoHardwareManagerWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
		{
			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new MenuCommand(this.Execute, menuCommandID);
			commandService.AddCommand(menuItem);
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static VivadoHardwareManagerWindowCommand Instance
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the service provider from the owner package.
		/// </summary>
		private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
		{
			get
			{
				return this.package;
			}
		}

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static async Task InitializeAsync(AsyncPackage package)
		{
			// Switch to the main thread - the call to AddCommand in VivadoHardwareManagerWindowCommand's constructor requires
			// the UI thread.
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
			Instance = new VivadoHardwareManagerWindowCommand(package, commandService);
			var toolbarbtnCmdID = new CommandID(CommandSet, cmdidWindowsMediaOpen);
			var menuItem = new MenuCommand(new EventHandler(Instance.ButtonHandler), toolbarbtnCmdID);
			commandService.AddCommand(menuItem);
		}
		private void ButtonHandler(object sender, EventArgs arguments)
		{
		}
		/// <summary>
		/// Shows the tool window when the menu item is clicked.
		/// </summary>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event args.</param>
		private void Execute(object sender, EventArgs e)
		{
			this.package.JoinableTaskFactory.RunAsync(async delegate
			{
				ToolWindowPane window = await this.package.ShowToolWindowAsync(typeof(VivadoHardwareManagerWindow), 0, true, this.package.DisposalToken);
				if ((null == window) || (null == window.Frame))
				{
					throw new NotSupportedException("Cannot create tool window");
				}
			});
		}
	}
}
