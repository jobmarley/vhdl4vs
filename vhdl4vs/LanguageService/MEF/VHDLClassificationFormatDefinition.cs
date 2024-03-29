﻿/*
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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace vhdl4vs
{
    class VHDLClassificationFormatDefinition
    {
        [Export]
        [Name("vhdl.constant")]
        [BaseDefinition("formal language")]
        internal static ClassificationTypeDefinition VHDLConstantDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "vhdl.constant")]
        [Name("VHDL Constants")]
        [DisplayName("VHDL Constants")]
        [UserVisible(true)]
        sealed class VHDLConstantClassificationFormat
            : ClassificationFormatDefinition
        {
            public VHDLConstantClassificationFormat() { this.ForegroundColor = System.Windows.Media.Color.FromArgb(255, 111, 0, 138); }
		}

		[Export]
		[Name("vhdl.variable")]
		[BaseDefinition("formal language")]
		internal static ClassificationTypeDefinition VHDLVariableDefinition;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "vhdl.variable")]
		[Name("VHDL Variables")]
		[DisplayName("VHDL Variables")]
		[UserVisible(true)]
		sealed class VHDLVariableClassificationFormat
			: ClassificationFormatDefinition
		{
			public VHDLVariableClassificationFormat() { this.ForegroundColor = System.Windows.Media.Colors.Navy; }
		}

		[Export]
		[Name("vhdl.signal")]
		[BaseDefinition("formal language")]
		internal static ClassificationTypeDefinition VHDLSignalDefinition;

		[Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "vhdl.signal")]
        [Name("VHDL Signals")]
        [DisplayName("VHDL Signals")]
        [UserVisible(true)]
        sealed class VHDLSignalClassificationFormat
           : ClassificationFormatDefinition
        {
            public VHDLSignalClassificationFormat() { this.ForegroundColor = System.Windows.Media.Colors.Navy; }
        }


		[Export]
		[Name("vhdl.port")]
		[BaseDefinition("formal language")]
		internal static ClassificationTypeDefinition VHDLPortDefinition;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "vhdl.port")]
		[Name("VHDL Ports")]
		[DisplayName("VHDL Ports")]
		[UserVisible(true)]
		sealed class VHDLPortClassificationFormat
		   : ClassificationFormatDefinition
		{
			public VHDLPortClassificationFormat() { this.ForegroundColor = System.Windows.Media.Color.FromArgb(255, 128, 128, 128); }
		}

		[Export]
		[Name("vhdl.architecture")]
		[BaseDefinition("formal language")]
		internal static ClassificationTypeDefinition VHDLArchitectureDefinition;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "vhdl.architecture")]
		[Name("VHDL Architectures")]
		[DisplayName("VHDL Architectures")]
		[UserVisible(true)]
		sealed class VHDLArchitectureClassificationFormat
		   : ClassificationFormatDefinition
		{
			public VHDLArchitectureClassificationFormat() { this.ForegroundColor = System.Windows.Media.Color.FromArgb(255, 43, 145, 175); }
		}

		[Export]
		[Name("vhdl.entity")]
		[BaseDefinition("formal language")]
		internal static ClassificationTypeDefinition VHDLEntityDefinition;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "vhdl.entity")]
		[Name("VHDL Entities")]
		[DisplayName("VHDL Entities")]
		[UserVisible(true)]
		sealed class VHDLEntityClassificationFormat
		   : ClassificationFormatDefinition
		{
			public VHDLEntityClassificationFormat() { this.ForegroundColor = System.Windows.Media.Color.FromArgb(255, 43, 145, 175); }
		}

		[Export]
		[Name("vhdl.type")]
		[BaseDefinition("formal language")]
		internal static ClassificationTypeDefinition VHDLTypeDefinition;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "vhdl.type")]
		[Name("VHDL Types")]
		[DisplayName("VHDL Types")]
		[UserVisible(true)]
		sealed class VHDLTypeClassificationFormat
		   : ClassificationFormatDefinition
		{
			public VHDLTypeClassificationFormat() { this.ForegroundColor = System.Windows.Media.Color.FromArgb(255, 43, 145, 175); }
		}

		[Export]
		[Name("vhdl.alias")]
		[BaseDefinition("formal language")]
		internal static ClassificationTypeDefinition VHDLAliasDefinition;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "vhdl.alias")]
		[Name("VHDL Aliases")]
		[DisplayName("VHDL Aliases")]
		[UserVisible(true)]
		sealed class VHDLAliasClassificationFormat
			: ClassificationFormatDefinition
		{
			public VHDLAliasClassificationFormat() { this.ForegroundColor = System.Windows.Media.Colors.Navy; }
		}
	}
}
