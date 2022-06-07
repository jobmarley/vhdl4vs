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
using Microsoft.VisualStudio.Project;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
//using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Shell.Interop;

namespace MyCompany.ProjectSystem.VHDL
{
    public class VHDLProjectNode : ProjectNode
    {
        #region Fields

        private IVsObjectManager2 m_objectManager2 = null;

        #endregion

        #region Properties

        /*public override int ImageIndex
        {
            get
            {
                return (int)VHDLProjectNode.ImageName.MLApplication;
            }
        }*/

        #endregion

        new public enum ImageName
        {
            //MLApplication = ProjectNode.ImageName.ImageLast + 1,
            //MLFile = ProjectNode.ImageName.ImageLast + 2,
            MLMethod = ProjectNode.ImageName.ImageLast + 1,

        }
        public override FileNode CreateFileNode(ProjectElement item)
        {
            if (item.GetFullPathForElement().EndsWith(".vhd", StringComparison.CurrentCultureIgnoreCase))
                return new VHDLFileNode(this, item);
            else
                return new FileNode(this, item);
        }

        private VHDLPackage package;


        /*public override int ImageIndex
		{
			get { return (int)ImageName.MLApplication; }
		}*/

        public override object GetIconHandle(bool open)
        {
            return null;
        }


        static VHDLProjectNode()
        {
            //imageList = Utilities.GetImageList(typeof(MLProjectNode).Assembly.GetManifestResourceStream("MLProjectNode.Resources.MLProjectNode.bmp"));
        }

        public VHDLProjectNode(VHDLPackage package)
            : base()
        {
            this.package = package;

            /*
             * Image are old ones. Gotta update with images from vs image library 2017.
             * Besides, old imagelist is 24bpp bmp with magenta keycolor. It doesnt look nice.
             * So change it to 32bit and load new images from png. That way it looks exactly like VS2017 and we dont modify MPFproj.
             * /!\ Some hierarchy nodes convert the image to HIcon (check HierarchyNode.GetProperty) which result in artifacts.
             * We should return null for HIcon but valid info for imagelist/imageindex.
             */

            this.ImageHandler.ImageList.ColorDepth = ColorDepth.Depth32Bit;
            this.ImageHandler.ImageList.TransparentColor = Color.Transparent;

            Image IconReference2017Updated = Image.FromStream(typeof(VHDLProjectNode).Assembly.GetManifestResourceStream("Microsoft.VisualStudio.Project.Resources.Reference_16x.png"));
            this.ImageHandler.ImageList.Images[(int)ProjectNode.ImageName.OpenReferenceFolder] = IconReference2017Updated;
            this.ImageHandler.ImageList.Images[(int)ProjectNode.ImageName.ReferenceFolder] = IconReference2017Updated;
            this.ImageHandler.ImageList.Images[(int)ProjectNode.ImageName.Reference] = IconReference2017Updated;
            this.ImageHandler.ImageList.Images[(int)ProjectNode.ImageName.Folder] = Image.FromStream(typeof(VHDLProjectNode).Assembly.GetManifestResourceStream("Microsoft.VisualStudio.Project.Resources.Folder_16x.png"));
            this.ImageHandler.ImageList.Images[(int)ProjectNode.ImageName.OpenFolder] = Image.FromStream(typeof(VHDLProjectNode).Assembly.GetManifestResourceStream("Microsoft.VisualStudio.Project.Resources.FolderOpen_16x.png"));


            //this.ImageHandler.AddImage(Image.FromStream(typeof(VHDLProjectNode).Assembly.GetManifestResourceStream("MyCompany.ProjectSystem.MyLanguage.Resources.Images.MLApplication_16x.png")));
            this.ImageHandler.AddImage(Image.FromStream(typeof(VHDLProjectNode).Assembly.GetManifestResourceStream("Microsoft.VisualStudio.Project.Resources.Method_purple_16x.png")));
        }
        public override Guid ProjectGuid
        {
            get { return typeof(VHDLProjectFactory).GUID; }
        }
        public override string ProjectType
        {
            get { return "VHDL"; }
        }

        protected override Guid[] GetConfigurationIndependentPropertyPages()
        {
            Guid[] result = new Guid[1];
            result[0] = typeof(VHDLGeneralPropertyPage).GUID;
            return result;
        }
        protected override Guid[] GetPriorityProjectDesignerPages()
        {
            Guid[] result = new Guid[1];
            result[0] = typeof(VHDLGeneralPropertyPage).GUID;
            return result;
        }
        protected internal override void ProcessFiles()
        {
            base.ProcessFiles();

            this.DisableQueryEdit = true;

            this.EventTriggeringFlag = ProjectNode.EventTriggering.DoNotTriggerHierarchyEvents | ProjectNode.EventTriggering.DoNotTriggerTrackerEvents;


            /*
             * Go through all code files.
             * If file is oppened :
             *      - Register background parser callback and update hierarchy on parse complete.
             * else :
             *      - Read file and parse it, then update hierarchy.
             * /
             * 
            /*for (HierarchyNode n = this.FirstChild; n != null; n = n.NextSibling)
            {
                if (n is FileNode)
                {
                    try
                    {
                        BuildAction buildAction = (BuildAction)n.NodeProperties.GetProperties()["BuildAction"].GetValue(n.NodeProperties);
                        if (buildAction == BuildAction.Compile && n.Url.EndsWith(".ML", StringComparison.CurrentCultureIgnoreCase))
                        {
                            HierarchyNode newNode = new MLFunctionNode(this, "test");
                            n.AddChild(newNode);
                            int qzdqd = 0;
                        }
                    }
                    catch (Exception e)
                    {

                    }
                }
            }*/


            this.SetProjectFileDirty(false);
            this.EventTriggeringFlag = ProjectNode.EventTriggering.TriggerAll;
            this.DisableQueryEdit = false;
        }

        protected override void Reload()
        {
            base.Reload();

           /* //  Update object browser
            m_objectManager2 = (IVsObjectManager2)this.Site.GetService(typeof(SVsObjectManager));
            uint dwCookie = 0;
            MLSimpleLibrary library = new MLSimpleLibrary(this);
            m_objectManager2.RegisterSimpleLibrary(library, out dwCookie);
            int qzdqzd = 0;*/
        }

        public override bool IsCodeFile(string fileName)
        {
#warning This is bad. It ignores the template "ItemType" parameter. Check ProjectNode.AddFileToMsBuild and Microsoft.VisualStudio.TemplateWizard.dll => Wizard.Execute/EnumContentsAddItem

            if (fileName.EndsWith(".vhd"))
                return true;

            return base.IsCodeFile(fileName);
        }
    }
}
