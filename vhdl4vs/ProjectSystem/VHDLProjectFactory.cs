using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Project;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace MyCompany.ProjectSystem.VHDL
{
    [Guid("1FAB42EA-3784-4508-980D-C575BB5B5A25")]
    public class VHDLProjectFactory : ProjectFactory
    {
        private VHDLPackage package;

        public VHDLProjectFactory(VHDLPackage package)
            : base(package)
        {
            this.package = package;
        }

        protected override ProjectNode CreateProject()
        {
            VHDLProjectNode project = new VHDLProjectNode(this.package);

            project.SetSite((IOleServiceProvider)((IServiceProvider)this.package).GetService(typeof(IOleServiceProvider)));
            return project;
        }
    }
}
