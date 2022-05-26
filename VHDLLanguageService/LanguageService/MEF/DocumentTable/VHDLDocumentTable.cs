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

namespace MyCompany.LanguageServices.VHDL
{
	[Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
	[AppliesTo(Project.VHDL.MyUnconfiguredProject.UniqueCapability)]
	internal class VHDLProjectWatcher
		: IProjectDynamicLoadComponent
	{
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
		
		uint m_rdtEventCookie = 0;
		
		private ConcurrentDictionary<UnconfiguredProject, VHDLProject> m_projects = new ConcurrentDictionary<UnconfiguredProject, VHDLProject>();
		private ConcurrentDictionary<string, VHDLDocument> m_orphanDocuments = new ConcurrentDictionary<string, VHDLDocument>(StringComparer.OrdinalIgnoreCase);

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

			m_textDocumentFactoryService.TextDocumentCreated += OnTextDocumentCreated;
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

		private async void OnTextDocumentCreated(object sender, TextDocumentEventArgs e)
		{
			if (e.TextDocument.TextBuffer.ContentType.TypeName == "inert")
				return;
			VHDLDocument doc = GetDocument(e.TextDocument.FilePath);
			if (doc == null)
			{
				//	New document
				doc = new VHDLDocument(this);
				doc.Filepath = e.TextDocument.FilePath;
				doc.TextDocument = e.TextDocument;
				doc.Project = m_projects.Values.FirstOrDefault(); // Add first project so libraries work kind of
				doc.Parser.Parser = new VHDLBackgroundParser(e.TextDocument.TextBuffer, TaskScheduler.Default, doc);
				if (m_orphanDocuments.TryAdd(doc.Filepath, doc))
				{
					DocumentAdded?.Invoke(this, new VHDLDocumentEventArgs(doc));
					DocumentOpened?.Invoke(this, new VHDLDocumentEventArgs(doc));
				}
			}
			else
			{
				//	Document found
				doc.TextDocument = e.TextDocument;
				doc.Parser.Parser = new VHDLBackgroundParser(e.TextDocument.TextBuffer, TaskScheduler.Default, doc);
				DocumentOpened?.Invoke(this, new VHDLDocumentEventArgs(doc));
			}
		}
		private async void OnTextDocumentDisposed(object sender, TextDocumentEventArgs e)
		{
			VHDLDocument doc = GetDocument(e.TextDocument.FilePath);
			if (doc == null)
				return;

			doc.TextDocument = null;
			if (!(doc.Parser.Parser is VHDLSimpleParser)/* && buffer != null*/)
			{
				doc.Parser.Parser = new VHDLSimpleParser(doc);
				DocumentClosed?.Invoke(this, new VHDLDocumentEventArgs(doc));
			}
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
					VHDLBackgroundParser parser = new VHDLBackgroundParser(buffer, TaskScheduler.Default, vhdldoc);
					vhdldoc.Parser.Parser = parser;
					DocumentOpened?.Invoke(this, new VHDLDocumentEventArgs(vhdldoc));
				}
				return vhdldoc;
			}

			// Just add a new one. We should never return null
			vhdldoc = new VHDLDocument(this);
			vhdldoc.Filepath = doc.FilePath;
			vhdldoc.TextDocument = doc;
			vhdldoc.Project = m_projects.Values.FirstOrDefault(); // Add first project so libraries work kind of
			vhdldoc.Parser.Parser = new VHDLBackgroundParser(buffer, TaskScheduler.Default, vhdldoc);

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
