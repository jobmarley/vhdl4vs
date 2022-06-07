/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace vhdl4vs
{
	/// <summary>
	/// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports the <see cref="IWpfTextViewCreationListener"/>
	/// that instantiates the adornment on the event of a <see cref="IWpfTextView"/>'s creation
	/// </summary>
	[Export(typeof(IWpfTextViewCreationListener))]
	[ContentType("VHDL")]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	internal sealed class VHDLColumnGuideAdornmentTextViewCreationListener : IWpfTextViewCreationListener
	{
		// Disable "Field is never assigned to..." and "Field is never used" compiler's warnings. Justification: the field is used by MEF.
#pragma warning disable 649, 169

		/// <summary>
		/// Defines the adornment layer for the scarlet adornment. This layer is ordered
		/// after the selection layer in the Z-order
		/// </summary>
		[Export(typeof(AdornmentLayerDefinition))]
		[Name("VHDLColumnGuideAdornment")]
		[Order(After = PredefinedAdornmentLayers.Text)]
		[TextViewRole(PredefinedTextViewRoles.Document)]
		private AdornmentLayerDefinition editorAdornmentLayer;

		[Import]
		VHDLDocumentTable vhdlDocumentTable;
#pragma warning restore 649, 169

		/// <summary>
		/// Instantiates a VHDLColumnGuideAdornment manager when a textView is created.
		/// </summary>
		/// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
		public void TextViewCreated(IWpfTextView textView)
		{
			// The adorment will get wired to the text view events
			new VHDLColumnGuideAdornment(textView, vhdlDocumentTable);
		}
	}
}
