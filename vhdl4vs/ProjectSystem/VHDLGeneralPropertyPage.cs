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

using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Project;

using System.ComponentModel;

namespace MyCompany.ProjectSystem.VHDL
{
    [ComVisible(true)]
    [Guid("EEFD340F-44AD-4A99-8C55-F2701777B9CD")]
    public class VHDLGeneralPropertyPage : SettingsPage
    {
        private string assemblyName;
        private OutputType outputType;
        private string defaultNamespace;

        public VHDLGeneralPropertyPage()
        {
            this.Name = "General";
        }

        [Category("AssemblyName")]
        [DisplayName("AssemblyName")]
        [Description("The output file holding assembly metadata.")]
        public string AssemblyName
        {
            get { return this.assemblyName; }
        }
        [Category("Application")]
        [DisplayName("OutputType")]
        [Description("The type of application to build.")]
        public OutputType OutputType
        {
            get { return this.outputType; }
            set { this.outputType = value; this.IsDirty = true; }
        }
        [Category("Application")]
        [DisplayName("DefaultNamespace")]
        [Description("Specifies the default namespace for added items.")]
        public string DefaultNamespace
        {
            get { return this.defaultNamespace; }
            set { this.defaultNamespace = value; this.IsDirty = true; }
        }

        protected override void BindProperties()
        {
            this.assemblyName = this.ProjectMgr.GetProjectProperty("AssemblyName", true);
            this.defaultNamespace = this.ProjectMgr.GetProjectProperty("RootNamespace", false);

            string outputType = this.ProjectMgr.GetProjectProperty("OutputType", false);
            this.outputType = (OutputType)Enum.Parse(typeof(OutputType), outputType);
        }

        protected override int ApplyChanges()
        {
            this.ProjectMgr.SetProjectProperty("AssemblyName", this.assemblyName);
            this.ProjectMgr.SetProjectProperty("OutputType", this.outputType.ToString());
            this.ProjectMgr.SetProjectProperty("RootNamespace", this.defaultNamespace);
            this.IsDirty = false;

            return VSConstants.S_OK;
        }
    }
}
