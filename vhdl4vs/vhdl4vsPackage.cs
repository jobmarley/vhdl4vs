/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace vhdl4vs
{
    // Xml Designer
    // Register the class as a Designer View in cooperation with the Xml Editor
    /*[ProvideXmlEditorChooserDesignerView("hdlxml", "xml", LogicalViewID.Designer, 0x60,
		DesignerLogicalViewEditor = typeof(HDLXmlEditorFactory),
		Namespace = "http://schemas.mycompany.com/developer/hdlxml/2005",
		MatchExtensionAndNamespace = true)]
	// And which type of files we want to handle
	[ProvideEditorExtension(typeof(EditorFactory), EditorFactory.Extension, 0x40, NameResourceID = 106)]
	// We register that our editor supports LOGVIEWID_Designer logical view
	[ProvideEditorLogicalView(typeof(EditorFactory), LogicalViewID.Designer)]

	// We register the XML Editor ("{FA3CD31E-987B-443A-9B81-186104E8DAC1}") as an EditorFactoryNotify
	// object to handle our ".vstemplate" file extension for the following projects:
	// Microsoft Visual Basic Project
	[EditorFactoryNotifyForProject("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}", EditorFactory.Extension, GuidList.guidXmlChooserEditorFactory)]
	// Microsoft Visual C# Project
	[EditorFactoryNotifyForProject("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", EditorFactory.Extension, GuidList.guidXmlChooserEditorFactory)]*/

    // VHDL Language Service
    //	Project Type attributes
    /*[ProvideProjectFactory(typeof(VHDLProjectFactory), "VHDL",
        "VHDL Project Files (*.vhdproj);*.vhdproj", "vhdproj", "vhdproj",
        "NullPath",//@"Templates\Projects\MyHDL",
        LanguageVsTemplate = "VHDL")]
    [ProvideObject(typeof(VHDLGeneralPropertyPage))]*/

    //  Language service attributes
    [ProvideService(typeof(VHDLLanguageService),
                             ServiceName = "VHDL Language Service")]
    [ProvideService(typeof(SVHDLSettings), IsAsyncQueryable = true)]
    [ProvideLanguageService(typeof(VHDLLanguageService),
                             "VHDL",
                             106,             // resource ID of localized language name
                             CodeSense = true,             // Supports IntelliSense
                             RequestStockColors = false,   // Supplies custom colors
                             EnableCommenting = true,      // Supports commenting out code
                             EnableAsyncCompletion = true  // Supports background parsing
                             )]
    [ProvideLanguageExtension(typeof(VHDLLanguageService),
                                       ".vhd")]
    [ProvideLanguageCodeExpansion(
             typeof(VHDLLanguageService),
             "VHDL", // Name of language used as registry key.
             106,           // Resource ID of localized name of language service.
             "VHDL",  // language key used in snippet templates.
             @"%InstallRoot%\VHDL\SnippetsIndex.xml",  // Path to snippets index
             SearchPaths = @"%InstallRoot%\VHDL\Snippets\%LCID%\Snippets\;" +
                           @"%TestDocs%\Code Snippets\VHDL\Test Code Snippets"
             )]
    [ProvideLanguageEditorOptionPage(typeof(VHDLAdvancedOptionPage), "VHDL", "", "Advanced", null, 0)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(vhdl4vsPackage.PackageGuidString)]
    public sealed class vhdl4vsPackage : AsyncPackage
    {
        /// <summary>
        /// VHDLLanguageServicePackage GUID string.
        /// </summary>
        public const string PackageGuidString = "a9293fd0-afa2-4425-912b-4cfe7165eaa1";

        #region Package Members

        async System.Threading.Tasks.Task<object> LanguageServiceCreatorAsync(IAsyncServiceContainer container, CancellationToken ct, Type serviceType)
        {
            VHDLLanguageService ls = new VHDLLanguageService();
            ls.Initialize();
            return ls;
        }

        async System.Threading.Tasks.Task<object> VHDLSettingsServiceCreatorAsync(IAsyncServiceContainer container, CancellationToken ct, Type serviceType)
		{
            VHDLSettingsService s = new VHDLSettingsService(this);
            s.Initialize();
            return s;
        }
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            AddService(typeof(VHDLLanguageService), LanguageServiceCreatorAsync);
            AddService(typeof(SVHDLSettings), VHDLSettingsServiceCreatorAsync, true);

            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        }

        #endregion
    }
}
