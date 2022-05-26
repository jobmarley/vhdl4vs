/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace MyCompany.Project.VHDL
{
	[Export]
	[AppliesTo(MyUnconfiguredProject.UniqueCapability)]
	[ProjectTypeRegistration(VHDLProjectTypePackage.ProjectTypeGuid, "VHDL Project", "#2", ProjectExtension, Language, resourcePackageGuid: VHDLProjectTypePackage.PackageGuidString, PossibleProjectExtensions = ProjectExtension)]
	public class MyUnconfiguredProject
	{
		/// <summary>
		/// The file extension used by your project type.
		/// This does not include the leading period.
		/// </summary>
		internal const string ProjectExtension = "vhdproj";

		/// <summary>
		/// A project capability that is present in your project type and none others.
		/// This is a convenient constant that may be used by your extensions so they
		/// only apply to instances of your project type.
		/// </summary>
		/// <remarks>
		/// This value should be kept in sync with the capability as actually defined in your .targets.
		/// </remarks>
		public const string UniqueCapability = "VHDLProjectType";

		internal const string Language = "VHDL";

		[ImportingConstructor]
		public MyUnconfiguredProject(UnconfiguredProject unconfiguredProject)
		{
			this.ProjectHierarchies = new OrderPrecedenceImportCollection<IVsHierarchy>(projectCapabilityCheckProvider: unconfiguredProject);
		}

		[Import]
		internal UnconfiguredProject UnconfiguredProject { get; private set; }

		[Import]
		internal IActiveConfiguredProjectSubscriptionService SubscriptionService { get; private set; }

		[Import]
		internal IProjectThreadingService ProjectThreadingService { get; private set; }

		[Import]
		internal ActiveConfiguredProject<ConfiguredProject> ActiveConfiguredProject { get; private set; }

		[Import]
		internal ActiveConfiguredProject<MyConfiguredProject> MyActiveConfiguredProject { get; private set; }

		[ImportMany(ExportContractNames.VsTypes.IVsProject, typeof(IVsProject))]
		internal OrderPrecedenceImportCollection<IVsHierarchy> ProjectHierarchies { get; private set; }

		internal IVsHierarchy ProjectHierarchy
		{
			get { return this.ProjectHierarchies.Single().Value; }
		}
	}

	/*[Export(typeof(Microsoft.VisualStudio.ProjectSystem.Build.IDeployProvider))]
	[AppliesTo(MyUnconfiguredProject.UniqueCapability)]
	internal class DeployProvider1 : Microsoft.VisualStudio.ProjectSystem.Build.IDeployProvider
	{
		/// <summary>
		/// Provides access to the project's properties.
		/// </summary>
		[Import]
		private ProjectProperties Properties { get; set; }

		public async Task DeployAsync(System.Threading.CancellationToken cancellationToken, System.IO.TextWriter outputPaneWriter)
		{
			var generalProperties = await this.Properties.GetConfigurationGeneralPropertiesAsync();
			string targetFramework = await generalProperties.StartItem.GetEvaluatedValueAtEndAsync();
			await outputPaneWriter.WriteAsync(targetFramework);
		}

		public bool IsDeploySupported
		{
			get { return true; }
		}

		public void Commit()
		{
		}

		public void Rollback()
		{
		}
	}*/
}
