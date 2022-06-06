using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

/*
 * Defines content type for MyScript.
 * This is used by other MEF component to identify the content they are used for.
 * */
namespace vhdl4vs
{
    internal static class VHDLFileAndContentTypeDefinition
    {
        [Export]
        [Name("VHDL")]
        [BaseDefinition("code")]
        //[BaseDefinition("projection")]
        internal static ContentTypeDefinition VHDLContentTypeDefinition;

        [Export]
        [FileExtension(".vhd;.vhdl")]
        [ContentType("VHDL")]
        internal static FileExtensionToContentTypeDefinition VHDLFileExtensionDefinition;
    }
}
