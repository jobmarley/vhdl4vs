/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
//using Microsoft.VisualStudio.XmlEditor;
using EnvDTE;
//using tom;

using ISysServiceProvider = System.IServiceProvider;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using VSStd97CmdID = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;

namespace vhdl4vs.VivadoIntegration
{
	/// <summary>
	/// This control hosts the editor and is responsible for
	/// handling the commands targeted to the editor 
	/// </summary>

	[ComVisible(true)]
	public sealed class HDLXmlEditorPane
		: WindowPane, IOleComponent, IVsDeferredDocView, IVsLinkedUndoClient
	{
		#region Fields
		//private VsTemplateDesignerPackage _thisPackage;
		private string _fileName = string.Empty;
		private HDLXmlEditorControl _control;
		private IVsTextLines _textBuffer;
		private uint _componentId;
		private IOleUndoManager _undoManager;
		//private XmlStore _store;
		//private XmlModel _model;
		#endregion

		#region "Window.Pane Overrides"
		/// <summary>
		/// Constructor that calls the Microsoft.VisualStudio.Shell.WindowPane constructor then
		/// our initialization functions.
		/// </summary>
		/// <param name="package">Our Package instance.</param>
		public HDLXmlEditorPane(string fileName, IVsTextLines textBuffer)
			: base(null)
		{
			_fileName = fileName;
			_textBuffer = textBuffer;
		}
		/// <summary>
		/// The shell call this function to know if a menu item should be visible and
		/// if it should be enabled/disabled.
		/// Note that this function will only be called when an instance of this editor is open.
		/// </summary>
		/// <param name="pguidCmdGroup">Guid describing which set of command the current command(s) belong to.</param>
		/// <param name="cCmds">Number of command which status are being asked for.</param>
		/// <param name="prgCmds">Information for each command.</param>
		/// <param name="pCmdText">Used to dynamically change the command text.</param>
		/// <returns>S_OK if the method succeeds.</returns> 
		//public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		//{
		//	// validate parameters
		//	if (prgCmds == null || cCmds != 1)
		//	{
		//		return VSConstants.E_INVALIDARG;
		//	}

		//	OLECMDF cmdf = OLECMDF.OLECMDF_SUPPORTED;

		//	if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
		//	{
		//		// Process standard Commands
		//		switch (prgCmds[0].cmdID)
		//		{
		//			case (uint)VSConstants.VSStd97CmdID.SelectAll:
		//				{
		//					// Always enabled
		//					cmdf = OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;
		//					break;
		//				}
		//			case (uint)VSConstants.VSStd97CmdID.Copy:
		//			case (uint)VSConstants.VSStd97CmdID.Cut:
		//				{
		//					// Enable if something is selected
		//					//if (editorControl.SelectionLength > 0)
		//					//{
		//						cmdf |= OLECMDF.OLECMDF_ENABLED;
		//					//}
		//					break;
		//				}
		//			case (uint)VSConstants.VSStd97CmdID.Paste:
		//				{
		//					// Enable if clipboard has content we can paste

		//					//if (editorControl.CanPaste(DataFormats.GetFormat(DataFormats.Text)))
		//					//{
		//						cmdf |= OLECMDF.OLECMDF_ENABLED;
		//					//}
		//					break;
		//				}
		//			case (uint)VSConstants.VSStd97CmdID.Redo:
		//				{
		//					// Enable if actions that have occurred within the RichTextBox 
		//					// can be reapplied
		//					//if (editorControl.CanRedo)
		//					//{
		//						cmdf |= OLECMDF.OLECMDF_ENABLED;
		//					//}
		//					break;
		//				}
		//			case (uint)VSConstants.VSStd97CmdID.Undo:
		//				{
		//					//if (editorControl.CanUndo)
		//					//{
		//						cmdf |= OLECMDF.OLECMDF_ENABLED;
		//					//}
		//					break;
		//				}
		//			default:
		//				{
		//					return (int)(Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED);
		//				}
		//		}
		//	}
		//	/*else if (pguidCmdGroup == GuidList.guidEditorCmdSet)
		//	{
		//		// Process our Commands
		//		switch (prgCmds[0].cmdID)
		//		{
		//			// if we had commands specific to our editor, they would be processed here
		//			default:
		//				{
		//					return (int)(Constants.OLECMDERR_E_NOTSUPPORTED);
		//				}
		//		}
		//	}*/
		//	else
		//	{
		//		return (int)(Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED); ;
		//	}

		//	prgCmds[0].cmdf = (uint)cmdf;
		//	return VSConstants.S_OK;
		//}

		///// <summary>
		///// Execute a specified command.
		///// </summary>
		///// <param name="pguidCmdGroup">Guid describing which set of command the current command(s) belong to.</param>
		///// <param name="nCmdID">Command that should be executed.</param>
		///// <param name="nCmdexecopt">Options for the command.</param>
		///// <param name="pvaIn">Pointer to input arguments.</param>
		///// <param name="pvaOut">Pointer to command output.</param>
		///// <returns>S_OK if the method succeeds or OLECMDERR_E_NOTSUPPORTED on unsupported command.</returns> 
		///// <remarks>Typically, only the first 2 arguments are used (to identify which command should be run).</remarks>
		//public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		//{
		//	Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Exec() of: {0}", ToString()));

		//	if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
		//	{
		//		// Process standard Visual Studio Commands
		//		switch (nCmdID)
		//		{
		//			case (uint)VSConstants.VSStd97CmdID.Copy:
		//				{
		//					//editorControl.Copy();
		//					break;
		//				}
		//			case (uint)VSConstants.VSStd97CmdID.Cut:
		//				{
		//					//editorControl.Cut();
		//					break;
		//				}
		//			case (uint)VSConstants.VSStd97CmdID.Paste:
		//				{
		//					//editorControl.Paste();
		//					break;
		//				}
		//			case (uint)VSConstants.VSStd97CmdID.Redo:
		//				{
		//					//editorControl.Redo();
		//					break;
		//				}
		//			case (uint)VSConstants.VSStd97CmdID.Undo:
		//				{
		//					//editorControl.Undo();
		//					break;
		//				}
		//			case (uint)VSConstants.VSStd97CmdID.SelectAll:
		//				{
		//					//editorControl.SelectAll();
		//					break;
		//				}
		//			default:
		//				{
		//					return (int)(Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED);
		//				}
		//		}
		//	}
		//	/*else if (pguidCmdGroup == GuidList.guidEditorCmdSet)
		//	{
		//		switch (nCmdID)
		//		{
		//			// if we had commands specific to our editor, they would be processed here
		//			default:
		//				{
		//					return (int)(Constants.OLECMDERR_E_NOTSUPPORTED);
		//				}
		//		}
		//	}*/
		//	else
		//	{
		//		return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP;
		//	}

		//	return VSConstants.S_OK;
		//}
		protected override void OnClose()
		{
			// unhook from Undo related services
			if (_undoManager != null)
			{
				IVsLinkCapableUndoManager linkCapableUndoMgr = (IVsLinkCapableUndoManager)_undoManager;
				if (linkCapableUndoMgr != null)
				{
					linkCapableUndoMgr.UnadviseLinkedUndoClient();
				}

				// Throw away the undo stack etc.
				// It is important to â€œzombifyâ€ the undo manager when the owning object is shutting down.
				// This is done by calling IVsLifetimeControlledObject.SeverReferencesToOwner on the undoManager.
				// This call will clear the undo and redo stacks. This is particularly important to do if
				// your undo units hold references back to your object. It is also important if you use
				// "mdtStrict" linked undo transactions as this sample does (see IVsLinkedUndoTransactionManager). 
				// When one object involved in linked undo transactions clears its undo/redo stacks, then 
				// the stacks of the other documents involved in the linked transaction will also be cleared. 
				IVsLifetimeControlledObject lco = (IVsLifetimeControlledObject)_undoManager;
				lco.SeverReferencesToOwner();
				_undoManager = null;
			}

			IOleComponentManager mgr = GetService(typeof(SOleComponentManager)) as IOleComponentManager;
			mgr.FRevokeComponent(_componentId);

			Dispose(true);

			base.OnClose();
		}
		#endregion

		/// <summary>
		/// Called after the WindowPane has been sited with an IServiceProvider from the environment
		/// 
		protected override void Initialize()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			base.Initialize();

			// Create and initialize the editor
			#region Register with IOleComponentManager
			IOleComponentManager componentManager = (IOleComponentManager)GetService(typeof(SOleComponentManager));
			if (this._componentId == 0 && componentManager != null)
			{
				OLECRINFO[] crinfo = new OLECRINFO[1];
				crinfo[0].cbSize = (uint)Marshal.SizeOf(typeof(OLECRINFO));
				crinfo[0].grfcrf = (uint)_OLECRF.olecrfNeedIdleTime | (uint)_OLECRF.olecrfNeedPeriodicIdleTime;
				crinfo[0].grfcadvf = (uint)_OLECADVF.olecadvfModal | (uint)_OLECADVF.olecadvfRedrawOff | (uint)_OLECADVF.olecadvfWarningsOff;
				crinfo[0].uIdleTimeInterval = 100;
				int hr = componentManager.FRegisterComponent(this, crinfo, out this._componentId);
				ErrorHandler.Succeeded(hr);
			}
			#endregion

			ComponentResourceManager resources = new ComponentResourceManager(typeof(HDLXmlEditorPane));

			#region Hook Undo Manager
			// Attach an IOleUndoManager to our WindowFrame. Merely calling QueryService 
			// for the IOleUndoManager on the site of our IVsWindowPane causes an IOleUndoManager
			// to be created and attached to the IVsWindowFrame. The WindowFrame automaticall 
			// manages to route the undo related commands to the IOleUndoManager object.
			// Thus, our only responsibilty after this point is to add IOleUndoUnits to the 
			// IOleUndoManager (aka undo stack).
			_undoManager = (IOleUndoManager)GetService(typeof(SOleUndoManager));

			// In order to use the IVsLinkedUndoTransactionManager, it is required that you
			// advise for IVsLinkedUndoClient notifications. This gives you a callback at 
			// a point when there are intervening undos that are blocking a linked undo.
			// You are expected to activate your document window that has the intervening undos.
			if (_undoManager != null)
			{
				IVsLinkCapableUndoManager linkCapableUndoMgr = (IVsLinkCapableUndoManager)_undoManager;
				if (linkCapableUndoMgr != null)
				{
					linkCapableUndoMgr.AdviseLinkedUndoClient(this);
				}
			}
			#endregion

			// hook up our 
			//XmlEditorService es = GetService(typeof(XmlEditorService)) as XmlEditorService;
			//_store = es.CreateXmlStore();
			//_store.UndoManager = _undoManager;

			//_model = _store.OpenXmlModel(new Uri(_fileName));

			// This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
			// we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
			// the object returned by the Content property.

			IMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as IMenuCommandService;
			_control = new HDLXmlEditorControl(mcs);
			_control.LoadFile(FileName);
			Content = _control;

			RegisterIndependentView(true);

			if (null != mcs)
			{
				// Now create one object derived from MenuCommnad for each command defined in
				// the CTC file and add it to the command service.

				// For each command we have to define its id that is a unique Guid/integer pair, then
				// create the OleMenuCommand object for this command. The EventHandler object is the
				// function that will be called when the user will select the command. Then we add the 
				// OleMenuCommand to the menu service.  The addCommand helper function does all this for us.
				AddCommand(mcs, VSConstants.GUID_VSStandardCommandSet97, (int)VSStd97CmdID.NewWindow,
								new EventHandler(OnNewWindow), new EventHandler(OnQueryNewWindow));
				AddCommand(mcs, VSConstants.GUID_VSStandardCommandSet97, (int)VSStd97CmdID.ViewCode,
								new EventHandler(OnViewCode), new EventHandler(OnQueryViewCode));
			}
		}

		/// <summary>
		/// returns the name of the file currently loaded
		/// </summary>
		public string FileName
		{
			get { return _fileName; }
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				RegisterIndependentView(false);

				/*using (_model)
				{
					_model = null;
				}
				using (_store)
				{
					_store = null;
				}*/
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Gets an instance of the RunningDocumentTable (RDT) service which manages the set of currently open 
		/// documents in the environment and then notifies the client that an open document has changed
		/// </summary>
		private void NotifyDocChanged()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Make sure that we have a file name
			if (_fileName.Length == 0)
				return;

			// Get a reference to the Running Document Table
			IVsRunningDocumentTable runningDocTable = (IVsRunningDocumentTable)GetService(typeof(SVsRunningDocumentTable));

			// Lock the document
			uint docCookie;
			IVsHierarchy hierarchy;
			uint itemID;
			IntPtr docData;
			int hr = runningDocTable.FindAndLockDocument(
				(uint)_VSRDTFLAGS.RDT_ReadLock,
				_fileName,
				out hierarchy,
				out itemID,
				out docData,
				out docCookie
			);
			ErrorHandler.ThrowOnFailure(hr);

			// Send the notification
			hr = runningDocTable.NotifyDocumentChanged(docCookie, (uint)__VSRDTATTRIB.RDTA_DocDataReloaded);

			// Unlock the document.
			// Note that we have to unlock the document even if the previous call failed.
			ErrorHandler.ThrowOnFailure(runningDocTable.UnlockDocument((uint)_VSRDTFLAGS.RDT_ReadLock, docCookie));

			// Check ff the call to NotifyDocChanged failed.
			ErrorHandler.ThrowOnFailure(hr);
		}

		/// <summary>
		/// Helper function used to add commands using IMenuCommandService
		/// </summary>
		/// <param name="mcs"> The IMenuCommandService interface.</param>
		/// <param name="menuGroup"> This guid represents the menu group of the command.</param>
		/// <param name="cmdID"> The command ID of the command.</param>
		/// <param name="commandEvent"> An EventHandler which will be called whenever the command is invoked.</param>
		/// <param name="queryEvent"> An EventHandler which will be called whenever we want to query the status of
		/// the command.  If null is passed in here then no EventHandler will be added.</param>
		private static void AddCommand(IMenuCommandService mcs, Guid menuGroup, int cmdID,
									   EventHandler commandEvent, EventHandler queryEvent)
		{
			// Create the OleMenuCommand from the menu group, command ID, and command event
			CommandID menuCommandID = new CommandID(menuGroup, cmdID);
			OleMenuCommand command = new OleMenuCommand(commandEvent, menuCommandID);

			// Add an event handler to BeforeQueryStatus if one was passed in
			if (null != queryEvent)
			{
				command.BeforeQueryStatus += queryEvent;
			}

			// Add the command using our IMenuCommandService instance
			mcs.AddCommand(command);
		}

		/// <summary>
		/// Registers an independent view with the IVsTextManager so that it knows
		/// the user is working with a view over the text buffer. This will trigger
		/// the text buffer to prompt the user whether to reload the file if it is
		/// edited outside of the environment.
		/// </summary>
		/// <param name="subscribe">True to subscribe, false to unsubscribe</param>
		void RegisterIndependentView(bool subscribe)
		{
			IVsTextManager textManager = (IVsTextManager)GetService(typeof(SVsTextManager));

			if (textManager != null)
			{
				if (subscribe)
				{
					textManager.RegisterIndependentView(this, _textBuffer);
				}
				else
				{
					textManager.UnregisterIndependentView(this, _textBuffer);
				}
			}
		}

		/// <summary>
		/// This method loads a localized string based on the specified resource.
		/// </summary>
		/// <param name="resourceName">Resource to load</param>
		/// <returns>String loaded for the specified resource</returns>
		//internal string GetResourceString(string resourceName)
		//{
		//	string resourceValue;
		//	IVsResourceManager resourceManager = (IVsResourceManager)GetService(typeof(SVsResourceManager));
		//	if (resourceManager == null)
		//	{
		//		throw new InvalidOperationException("Could not get SVsResourceManager service. Make sure the package is Sited before calling this method");
		//	}
		//	Guid packageGuid = _thisPackage.GetType().GUID;
		//	int hr = resourceManager.LoadResourceString(ref packageGuid, -1, resourceName, out resourceValue);
		//	ErrorHandler.ThrowOnFailure(hr);
		//	return resourceValue;
		//}

		#region Commands

		private void OnQueryNewWindow(object sender, EventArgs e)
		{
			OleMenuCommand command = (OleMenuCommand)sender;
			command.Enabled = true;
		}

		private void OnNewWindow(object sender, EventArgs e)
		{
			NewWindow();
		}

		private void OnQueryViewCode(object sender, EventArgs e)
		{
			OleMenuCommand command = (OleMenuCommand)sender;
			command.Enabled = true;
		}

		private void OnViewCode(object sender, EventArgs e)
		{
			ViewCode();
		}

		private void NewWindow()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			int hr = VSConstants.S_OK;

			IVsUIShellOpenDocument uishellOpenDocument = (IVsUIShellOpenDocument)GetService(typeof(SVsUIShellOpenDocument));
			if (uishellOpenDocument != null)
			{
				IVsWindowFrame windowFrameOrig = (IVsWindowFrame)GetService(typeof(SVsWindowFrame));
				if (windowFrameOrig != null)
				{
					IVsWindowFrame windowFrameNew;
					Guid LOGVIEWID_Primary = Guid.Empty;
					hr = uishellOpenDocument.OpenCopyOfStandardEditor(windowFrameOrig, ref LOGVIEWID_Primary, out windowFrameNew);
					if (windowFrameNew != null)
						hr = windowFrameNew.Show();
					ErrorHandler.ThrowOnFailure(hr);
				}
			}
		}

		private void ViewCode()
		{
			Guid XmlTextEditorGuid = new Guid("FA3CD31E-987B-443A-9B81-186104E8DAC1");

			// Open the referenced document using our editor.
			IVsWindowFrame frame;
			IVsUIHierarchy hierarchy;
			uint itemid;
			VsShellUtilities.OpenDocumentWithSpecificEditor(this, _fileName,
				XmlTextEditorGuid, VSConstants.LOGVIEWID_Primary, out hierarchy, out itemid, out frame);
			ErrorHandler.ThrowOnFailure(frame.Show());
		}

		#endregion

		#region IVsLinkedUndoClient

		public int OnInterveningUnitBlockingLinkedUndo()
		{
			return VSConstants.E_FAIL;
		}

		#endregion

		#region IVsDeferredDocView

		/// <summary>
		/// Assigns out parameter with the Guid of the EditorFactory.
		/// </summary>
		/// <param name="pGuidCmdId">The output parameter that receives a value of the Guid of the EditorFactory.</param>
		/// <returns>S_OK if Marshal operations completed successfully.</returns>
		int IVsDeferredDocView.get_CmdUIGuid(out Guid pGuidCmdId)
		{
			pGuidCmdId = new Guid("2E903EF5-5D9F-4B6E-A118-19FE8C7A676A");
			return VSConstants.S_OK;
		}

		/// <summary>
		/// Assigns out parameter with the document view being implemented.
		/// </summary>
		/// <param name="ppUnkDocView">The parameter that receives a reference to current view.</param>
		/// <returns>S_OK if Marshal operations completed successfully.</returns>
		[EnvironmentPermission(SecurityAction.Demand)]
		int IVsDeferredDocView.get_DocView(out IntPtr ppUnkDocView)
		{
			ppUnkDocView = Marshal.GetIUnknownForObject(this);
			return VSConstants.S_OK;
		}

		#endregion

		#region IOleComponent

		int IOleComponent.FContinueMessageLoop(uint uReason, IntPtr pvLoopData, MSG[] pMsgPeeked)
		{
			return VSConstants.S_OK;
		}

		int IOleComponent.FDoIdle(uint grfidlef)
		{
			if (_control != null)
			{
				//_control.DoIdle();
			}
			return VSConstants.S_OK;
		}

		int IOleComponent.FPreTranslateMessage(MSG[] pMsg)
		{
			return VSConstants.S_OK;
		}

		int IOleComponent.FQueryTerminate(int fPromptUser)
		{
			return 1; //true
		}

		int IOleComponent.FReserved1(uint dwReserved, uint message, IntPtr wParam, IntPtr lParam)
		{
			return VSConstants.S_OK;
		}

		IntPtr IOleComponent.HwndGetWindow(uint dwWhich, uint dwReserved)
		{
			return IntPtr.Zero;
		}

		void IOleComponent.OnActivationChange(IOleComponent pic, int fSameComponent, OLECRINFO[] pcrinfo, int fHostIsActivating, OLECHOSTINFO[] pchostinfo, uint dwReserved) { }
		void IOleComponent.OnAppActivate(int fActive, uint dwOtherThreadID) { }
		void IOleComponent.OnEnterState(uint uStateID, int fEnter) { }
		void IOleComponent.OnLoseActivation() { }
		void IOleComponent.Terminate() { }

		#endregion
	}
}
