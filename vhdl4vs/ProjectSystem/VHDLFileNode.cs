/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Microsoft.VisualStudio.Project;
using System.Drawing;
using System.Windows.Forms;
using System.Resources;

namespace MyCompany.ProjectSystem.VHDL
{
    /*[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class MLLocDisplayNameAttribute
        : DisplayNameAttribute
    {
        #region fields
        string name;
        #endregion

        #region ctors
        public MLLocDisplayNameAttribute(string name)
        {
            this.name = name;
        }
        #endregion

        #region properties
        public override string DisplayName
        {
            get
            {
                string result = Properties.SR.ResourceManager.GetString(this.name, CultureInfo.CurrentUICulture);
                if (result == null)
                {
                    Debug.Assert(false, "String resource '" + this.name + "' is missing");
                    result = this.name;
                }
                return result;
            }
        }
        #endregion
    }
    [AttributeUsage(AttributeTargets.All)]
    internal sealed class MLSRDescriptionAttribute
        : DescriptionAttribute
    {
        private bool replaced;

        public MLSRDescriptionAttribute(string description)
            : base(description)
        {
        }

        public override string Description
        {
            get
            {
                if (!replaced)
                {
                    replaced = true;
                    DescriptionValue = Properties.SR.ResourceManager.GetString(base.Description, CultureInfo.CurrentUICulture);
                }
                return base.Description;
            }
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    internal sealed class MLSRCategoryAttribute : CategoryAttribute
    {

        public MLSRCategoryAttribute(string category)
            : base(category)
        {
        }

        protected override string GetLocalizedString(string value)
        {
            return Properties.SR.ResourceManager.GetString(value, CultureInfo.CurrentUICulture);
        }
    }*/

    [ComVisible(true)]
    public class VHDLFileNode
        : FileNode
    {
        private VHDLProjectNode projectMgr2 = null;
        public VHDLProjectNode ProjectMgr2
        {
            get
            {
                return projectMgr2;
            }
            set
            {
                projectMgr2 = value;
            }
        }
        /*public override int ImageIndex
        {
            get
            {
                return (int)VHDLProjectNode.ImageName.MLFile;
            }
        }*/

        /// <summary>
        /// Default implementation convert image to hicon which makes artifacts. Return null to force use of imageIndex.
        /// </summary>
        public override object GetIconHandle(bool open)
        {
            return null;
        }

        public VHDLFileNode(VHDLProjectNode root, ProjectElement element)
            : base(root, element)
        {
            projectMgr2 = root;
        }

        /*protected override NodeProperties CreatePropertiesObject()
        {
            ISingleFileGenerator generator = this.CreateSingleFileGenerator();

            return generator == null ? new MLFileNodeProperties(this) : new MLSingleFileGeneratorNodeProperties(this);
        }*/
    }

    /*public class OutputLanguageConverter : EnumConverter
    {

        public OutputLanguageConverter()
            : base(typeof(OutputLanguage))
        {

        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string str = value as string;

            if (str != null)
            {
                if (str == "Verilog") return OutputLanguage.Verilog;

                if (str == "VHDL") return OutputLanguage.VHDL;
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                string result = null;

                // In some cases if multiple nodes are selected the windows form engine
                // calls us with a null value if the selected node's property values are not equal
                // Example of windows form engine passing us null: File set to Compile, Another file set to None, bot nodes are selected, and the build action combo is clicked.
                if (value != null)
                {
                    result = ((OutputLanguage)value).ToString();//SR.GetString(((BuildAction)value).ToString(), culture);
                }
                else
                {
                    //result = SR.GetString(BuildAction.None.ToString(), culture);
                    result = "Verilog";
                }

                if (result != null) return result;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context)
        {
            return true;
        }

        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new OutputLanguage[] { OutputLanguage.Verilog, OutputLanguage.VHDL });
        }
    }

    /// <summary>
    /// An enumeration that describes the type of action to be taken by the build.
    /// </summary>
    [PropertyPageTypeConverterAttribute(typeof(OutputLanguageConverter))]
    public enum OutputLanguage
    {
        Verilog,
        VHDL,
    }

    [ComVisible(true)]
    public class MLFileNodeProperties
        : FileNodeProperties
    {
        #region properties
        [SRCategoryAttribute(SR.Advanced)]
        [MLLocDisplayName("OutputLanguage")]
        [MLSRDescriptionAttribute("OutputLanguageDescription")]
        public virtual OutputLanguage OutputLanguage
        {
            get
            {
                string value = this.Node.ItemNode.GetMetadata("OutputLanguage");
                if (value == null || value.Length == 0)
                {
                    return OutputLanguage.Verilog;
                }
                return (OutputLanguage)Enum.Parse(typeof(OutputLanguage), value);
            }
            set
            {
                this.Node.ItemNode.SetMetadata("OutputLanguage", value.ToString());
            }
        }
        #endregion

        #region ctors
        public MLFileNodeProperties(HierarchyNode node)
            : base(node)
        {
        }
        #endregion

        #region overridden methods
        public override string GetClassName()
        {
            return Properties.SR.ResourceManager.GetString("MLFileProperties", CultureInfo.CurrentUICulture);
            //return SR.GetString(SR.FileProperties, CultureInfo.CurrentUICulture);
        }
        #endregion
    }

    [ComVisible(true)]
    public class MLSingleFileGeneratorNodeProperties
        : MLFileNodeProperties
    {
        #region fields
        private EventHandler<HierarchyNodeEventArgs> onCustomToolChanged;
        private EventHandler<HierarchyNodeEventArgs> onCustomToolNameSpaceChanged;
        #endregion

        #region custom tool events
        internal event EventHandler<HierarchyNodeEventArgs> OnCustomToolChanged
        {
            add { onCustomToolChanged += value; }
            remove { onCustomToolChanged -= value; }
        }

        internal event EventHandler<HierarchyNodeEventArgs> OnCustomToolNameSpaceChanged
        {
            add { onCustomToolNameSpaceChanged += value; }
            remove { onCustomToolNameSpaceChanged -= value; }
        }

        #endregion

        #region properties
        [SRCategoryAttribute(SR.Advanced)]
        [LocDisplayName(SR.CustomTool)]
        [SRDescriptionAttribute(SR.CustomToolDescription)]
        public virtual string CustomTool
        {
            get
            {
                return this.Node.ItemNode.GetMetadata(ProjectFileConstants.Generator);
            }
            set
            {
                if (CustomTool != value)
                {
                    this.Node.ItemNode.SetMetadata(ProjectFileConstants.Generator, value != string.Empty ? value : null);
                    HierarchyNodeEventArgs args = new HierarchyNodeEventArgs(this.Node);
                    if (onCustomToolChanged != null)
                    {
                        onCustomToolChanged(this.Node, args);
                    }
                }
            }
        }

        [SRCategoryAttribute(SR.Advanced)]
        [LocDisplayName(SR.CustomToolNamespace)]
        [SRDescriptionAttribute(SR.CustomToolNamespaceDescription)]
        public virtual string CustomToolNamespace
        {
            get
            {
                return this.Node.ItemNode.GetMetadata(ProjectFileConstants.CustomToolNamespace);
            }
            set
            {
                if (CustomToolNamespace != value)
                {
                    this.Node.ItemNode.SetMetadata(ProjectFileConstants.CustomToolNamespace, value != String.Empty ? value : null);
                    HierarchyNodeEventArgs args = new HierarchyNodeEventArgs(this.Node);
                    if (onCustomToolNameSpaceChanged != null)
                    {
                        onCustomToolNameSpaceChanged(this.Node, args);
                    }
                }
            }
        }
        #endregion

        #region ctors
        public MLSingleFileGeneratorNodeProperties(HierarchyNode node)
            : base(node)
        {
        }
        #endregion
    }*/
}
