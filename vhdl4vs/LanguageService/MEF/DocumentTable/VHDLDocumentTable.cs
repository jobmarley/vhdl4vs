﻿/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
//using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using stdole;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
//using System.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using System.Collections.Concurrent;

using SVsServiceProvider = Microsoft.VisualStudio.Shell.SVsServiceProvider;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace vhdl4vs
{
	[Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
	[AppliesTo(VHDLProjectWatcher.UniqueCapability)]
	internal class VHDLProjectWatcher
		: IProjectDynamicLoadComponent
	{
		public const string UniqueCapability = "VHDLProjectType";
		[Import]
		public UnconfiguredProject UnconfiguredProject { get; set; }

		[Import]
		public VHDLDocumentTable DocumentTable { get; set; }

		public async Task LoadAsync()
		{
			await DocumentTable.NotifyProjectLoadedAsync(UnconfiguredProject);
		}

		public async Task UnloadAsync()
		{
			await DocumentTable.NotifyProjectUnloadedAsync(UnconfiguredProject);
		}
	}

	
	[Export(typeof(VHDLDocumentTable))]
	class VHDLDocumentTable
		: IDisposable
	{
		private IProjectServiceAccessor			m_projectServiceAccessor = null;
		private SVsServiceProvider				m_serviceProvider = null;
		private ITextDocumentFactoryService		m_textDocumentFactoryService = null;
		private IContentTypeRegistryService		m_contentTypeRegistryService = null;
		private IVsEditorAdaptersFactoryService m_editorAdaptersFactoryService = null;
		private ITextBufferFactoryService		m_textBufferFactoryService = null;
		private IProjectService					m_projectService = null;
		private IVsRunningDocumentTable4		m_runningDocumentTable = null;
		private IVHDLSettings					m_vhdlSettings = null;

		uint m_rdtEventCookie = 0;
		
		private ConcurrentDictionary<UnconfiguredProject, VHDLProject> m_projects = new ConcurrentDictionary<UnconfiguredProject, VHDLProject>();
		private ConcurrentDictionary<string, VHDLDocument> m_orphanDocuments = new ConcurrentDictionary<string, VHDLDocument>(StringComparer.OrdinalIgnoreCase);

		public IVHDLSettings Settings => m_vhdlSettings;
		public async Task NotifyProjectLoadedAsync(UnconfiguredProject project)
		{
			VHDLProject proj = new VHDLProject(project, this);
			if (!m_projects.TryAdd(project, proj))
				proj.Dispose();
		}
		public async Task NotifyProjectUnloadedAsync(UnconfiguredProject project)
		{
			VHDLProject proj = null;
			if (m_projects.TryRemove(project, out proj))
				proj.Dispose();
		}

		async Task InitAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			m_runningDocumentTable = m_serviceProvider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable4;
			m_vhdlSettings = m_serviceProvider.GetService(typeof(SVHDLSettings)) as IVHDLSettings;

			m_textDocumentFactoryService.TextDocumentDisposed += OnTextDocumentDisposed;
		}
		[ImportingConstructor]
		public VHDLDocumentTable(IProjectServiceAccessor projectServiceAccessor,
			SVsServiceProvider serviceProvider,
			ITextDocumentFactoryService textDocumentFactoryService,
			IContentTypeRegistryService contentTypeRegistryService,
			IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
			ITextBufferFactoryService textBufferFactoryService)
		{
			m_projectServiceAccessor = projectServiceAccessor;
			m_serviceProvider = serviceProvider;
			m_textDocumentFactoryService = textDocumentFactoryService;
			m_contentTypeRegistryService = contentTypeRegistryService;
			m_editorAdaptersFactoryService = editorAdaptersFactoryService;
			m_textBufferFactoryService = textBufferFactoryService;
			
			m_projectService = m_projectServiceAccessor.GetProjectService();

			InitAsync();
		}
		
		public event EventHandler<VHDLDocumentEventArgs> DocumentAdded;
		public event EventHandler<VHDLDocumentEventArgs> DocumentRemoved;
		public event EventHandler<VHDLDocumentEventArgs> DocumentOpened;
		public event EventHandler<VHDLDocumentEventArgs> DocumentClosed;

		private void OnTextDocumentDisposed(object sender, TextDocumentEventArgs e)
		{
			VHDLDocument doc = GetDocument(e.TextDocument.FilePath);
			if (doc == null)
				return;

			doc.TextDocument = null;
			doc.Parser.SwitchToSimpleParser(doc);
			DocumentClosed?.Invoke(this, new VHDLDocumentEventArgs(doc));
		}

		public VHDLDocument GetDocument(ITextBuffer buffer)
		{
			ITextDocument doc = null;
			if (!m_textDocumentFactoryService.TryGetTextDocument(buffer, out doc))
				return null;

			return GetDocument(doc.FilePath);
		}
		public VHDLDocument GetDocument(string filepath)
		{
			VHDLDocument document = null;
			foreach (VHDLProject proj in m_projects.Values)
			{
				document = proj.GetDocument(filepath);
				if (document != null)
					return document;
			}

			m_orphanDocuments.TryGetValue(filepath, out document);
			return document;
		}

		// This should never return null, since this is required for a lot of components
		// This can be called before OnTextDocumentCreated is called
		public VHDLDocument GetOrAddDocument(ITextBuffer buffer)
		{
			ITextDocument doc = null;
			if (!m_textDocumentFactoryService.TryGetTextDocument(buffer, out doc))
				return null;

			VHDLDocument vhdldoc = GetDocument(doc.FilePath);
			if (vhdldoc != null)
			{
				if (!(vhdldoc.Parser.Parser is VHDLBackgroundParser))
				{
					vhdldoc.Parser.SwitchToBackgroundParser(vhdldoc, buffer);
					DocumentOpened?.Invoke(this, new VHDLDocumentEventArgs(vhdldoc));
				}
				return vhdldoc;
			}

			// Just add a new one. We should never return null
			vhdldoc = new VHDLDocument(this);
			vhdldoc.Filepath = doc.FilePath;
			vhdldoc.TextDocument = doc;
			vhdldoc.Project = m_projects.Values.FirstOrDefault(); // Add first project so libraries work kind of
			vhdldoc.Parser.SwitchToBackgroundParser(vhdldoc, buffer);

			VHDLDocument addedDocument = m_orphanDocuments.GetOrAdd(doc.FilePath.ToLower(), vhdldoc);
			if (addedDocument == vhdldoc)
				DocumentAdded?.Invoke(this, new VHDLDocumentEventArgs(vhdldoc));
			return addedDocument;
		}

		/// <summary>
		/// Enumerate documents in the same project as the given one
		/// </summary>
		/// <param name="document">Document whose siblings must be returned</param>
		/// <returns></returns>
		public IEnumerable<VHDLDocument> EnumerateSiblings(VHDLDocument document)
		{
			if(document?.Project == null)
				return Enumerable.Empty<VHDLDocument>();

			VHDLProject proj = null;
			if(m_projects.TryGetValue(document.Project.UnconfiguredProject, out proj))
				return proj.Documents;
			return Enumerable.Empty<VHDLDocument>();
		}
		public IEnumerable<VHDLDocument> EnumerateDocuments(UnconfiguredProject project)
		{
			if (project == null)
				return Enumerable.Empty<VHDLDocument>();

			VHDLProject proj = null;
			if (m_projects.TryGetValue(project, out proj))
				return proj.Documents;
			return Enumerable.Empty<VHDLDocument>();
		}
		void IDisposable.Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private bool m_disposed = false;
		private void Dispose(bool disposing)
		{
			if (!this.m_disposed)
			{
				// If disposing equals true, dispose all managed
				// and unmanaged resources.
				if (disposing)
				{
					foreach (VHDLProject p in m_projects.Values)
						p.Dispose();
					//foreach (var hier in m_hierarchyEventHandlerDict.Values)
					//	hier.Dispose();

					//TextDocumentFactoryService.TextDocumentCreated -= OnTextDocumentCreated;
					//TextDocumentFactoryService.TextDocumentDisposed -= OnTextDocumentDisposed;
				}

				// Note disposing has been done.
				m_disposed = true;

			}
		}

	}
}
