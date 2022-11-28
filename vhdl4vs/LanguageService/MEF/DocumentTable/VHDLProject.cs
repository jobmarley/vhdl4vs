/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.VisualStudio.ProjectSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs
{
	class VHDLLibraryDocument
		: VHDLDocument
	{
		public VHDLLibraryDocument(VHDLDocumentTable documentTable)
			: base(documentTable)
		{

		}
	}
	enum VHDLLibraryChangedType
	{
		PackageAdded,
		PackageRemoved,
		PackageUpdated,
	}
	class VHDLLibraryChangedEventArgs
		: EventArgs
	{
		public VHDLLibraryChangedEventArgs(
			VHDLLibraryChangedType type,
			VHDLPackageDeclaration package,
			VHDLLibraryDeclaration library)
		{
			Type = type;
			Package = package;
			LibraryDeclaration = library;
			LibraryName = LibraryDeclaration.Name;
		}
		public VHDLLibraryChangedType Type { get; }
		public VHDLPackageDeclaration Package { get; }
		public string LibraryName { get; }
		public VHDLLibraryDeclaration LibraryDeclaration { get; }
	}
	class LibraryInfo
	{
		public string Name { get; }
		public string Path { get; }
		public ConcurrentBag<VHDLDocument> Documents { get; } = new ConcurrentBag<VHDLDocument>();
		public VHDLLibraryDeclaration Declaration { get; }

		public LibraryInfo(string name, string path)
		{
			Name = name;
			Path = path;
			Declaration = new VHDLLibraryDeclaration(name);
		}
	}
	class VHDLProject
		: IDisposable
	{
		private ConcurrentDictionary<string, VHDLDocument> m_documents = new ConcurrentDictionary<string, VHDLDocument>(StringComparer.OrdinalIgnoreCase);
		private List<IDisposable> m_disposables = new List<IDisposable>();
		private bool disposedValue;
		private VHDLDocumentTable m_documentTable = null;
		public IEnumerable<VHDLDocument> Documents => m_documents.Values;
		// What is needed:
		// Get list of libraries
		// Get list of packages in a library (as a Declaration for completion)
		// Get notified when a packaged is added/removed/changed
		private ConcurrentDictionary<string, LibraryInfo> m_libraries = new ConcurrentDictionary<string, LibraryInfo>(StringComparer.OrdinalIgnoreCase);

		public event EventHandler<VHDLDocumentEventArgs> DocumentAdded;
		public event EventHandler<VHDLDocumentEventArgs> DocumentRemoved;
		public event EventHandler<VHDLDocumentEventArgs> DocumentChanged;
		public event EventHandler<VHDLLibraryChangedEventArgs> LibraryChanged;
		public UnconfiguredProject UnconfiguredProject { get; }

		public VHDLProject(UnconfiguredProject unconfiguredProject, VHDLDocumentTable documentTable)
		{
			UnconfiguredProject = unconfiguredProject;
			m_documentTable = documentTable;
			m_disposables.Add(SubscribeToProjectData(unconfiguredProject, (IProjectVersionedValue<IProjectSubscriptionUpdate> update) => ProjectUpdateAsync(unconfiguredProject, update), "VHDLSource"));
			m_disposables.Add(SubscribeToProjectData(unconfiguredProject, (IProjectVersionedValue<IProjectSubscriptionUpdate> update) => ProjectConfigUpdateAsync(unconfiguredProject, update), "ConfigurationVHDL"));
		}
		private IDisposable SubscribeToProjectData(
			UnconfiguredProject unconfiguredProject,
			Func<IProjectVersionedValue<IProjectSubscriptionUpdate>,
			System.Threading.Tasks.Task> receiver,
			params string[] ruleNames)
		{
			var subscriptionService = unconfiguredProject.Services.ActiveConfiguredProjectSubscription;
			var receivingBlock = new System.Threading.Tasks.Dataflow.ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(receiver);
			return subscriptionService.JointRuleSource.SourceBlock.LinkTo(receivingBlock, ruleNames: ruleNames);
		}
		public VHDLDocument GetDocument(string filepath)
		{
			if (m_documents.TryGetValue(filepath, out VHDLDocument document))
				return document;
			return null;
		}
		public async Task ProjectUpdateAsync(UnconfiguredProject project, IProjectVersionedValue<IProjectSubscriptionUpdate> update)
		{
			//var typeScriptCompileItems = update.Value.CurrentState["TypeScriptCompile"].Items;
			var changes = update.Value.ProjectChanges["VHDLSource"].Difference;

			foreach (string filename in changes.AddedItems)
			{
				string absolutePath = filename;
				if (!System.IO.Path.IsPathRooted(absolutePath))
				{
					string projectDir = update.Value.ProjectChanges["VHDLSource"].After.Items[filename]["DefiningProjectDirectory"];
					absolutePath = System.IO.Path.Combine(projectDir, filename);
				}

				absolutePath = System.IO.Path.GetFullPath(absolutePath);

				// This can be called before the file already exist so...
				_ = Task.Run(async () =>
				{
					int i = 0;
					while (true)
					{
						if (System.IO.File.Exists(absolutePath))
						{
							VHDLDocument doc = new VHDLDocument(m_documentTable);
							doc.Filepath = absolutePath;
							doc.TextDocument = null;
							doc.Project = this;
							doc.Parser.SwitchToSimpleParser(doc);
							if (m_documents.TryAdd(doc.Filepath, doc))
							{
								doc.Parser.AnalysisComplete += OnDocumentAnalysisCompleted;
								DocumentAdded?.Invoke(this, new VHDLDocumentEventArgs(doc));
							}
							return;
						}
						else
						{
							if (++i >= 40)
								return;
							await Task.Delay(100);
						}
					}
				});
			}
			foreach (string filename in changes.RemovedItems)
			{
				string absolutePath = filename;
				if (!System.IO.Path.IsPathRooted(absolutePath))
				{
					string projectDir = update.Value.ProjectChanges["VHDLSource"].After.Items[filename]["DefiningProjectDirectory"];
					absolutePath = System.IO.Path.Combine(projectDir, filename);
				}
				VHDLDocument doc = null;
				if (m_documents.TryRemove(absolutePath, out doc))
				{
					DocumentRemoved?.Invoke(this, new VHDLDocumentEventArgs(doc));
					doc.Dispose();
				}
			}
			// Note that the first time this callback is invoked, all current items are presented as if they have just been added.
			// This allows you to always code in terms of the diff, and it automatically just works the first time.
			// If you don't like this behavior, you can turn it off by passing "initialDataAsNew: false" into the LinkTo method.
		}
		public async Task ProjectConfigUpdateAsync(UnconfiguredProject project, IProjectVersionedValue<IProjectSubscriptionUpdate> update)
		{
			lock (m_libraries)
			{
				if (update.Value.ProjectChanges["ConfigurationVHDL"].Difference.ChangedProperties.Contains("LibraryPaths"))
				{
					var libraryPaths = update.Value.ProjectChanges["ConfigurationVHDL"].After.Properties["LibraryPaths"].Split(';');
					Dictionary<string, string> libraryNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					foreach (string s in libraryPaths)
					{
						var v = s.Split('=');
						if (v.Length == 2)
							libraryNames[v[0]] = System.IO.Path.GetFullPath(v[1]);
					};

					//	Remove libraries that have been removed
					foreach (string name in m_libraries.Keys)
					{
						// changed as well, we had them back later
						if (!libraryNames.ContainsKey(name) || (string.Compare(libraryNames[name], m_libraries[name].Path, true) != 0))
						{
							//	If library name is not in new libraries, we remove it
							LibraryInfo lib = m_libraries[name];
							foreach (var package in lib?.Declaration?.Packages?.Values ?? Array.Empty<VHDLPackageDeclaration>())
								LibraryChanged?.Invoke(this, new VHDLLibraryChangedEventArgs(VHDLLibraryChangedType.PackageRemoved, package, lib.Declaration));
							m_libraries.TryRemove(name, out lib);
						}
					}
					//	Add new libraries
					foreach (var (name, path) in libraryNames.Where(x => !m_libraries.ContainsKey(x.Key)).Select(x => (x.Key, x.Value)))
					{
						if (!System.IO.Directory.Exists(path))
							continue;

						LibraryInfo lib = new LibraryInfo(name, path);
						foreach (string filepath in System.IO.Directory.GetFiles(path))
						{
							string fileFullPath = System.IO.Path.GetFullPath(filepath);
							string[] exts = { ".vhd", ".vhdl" };
							if (!exts.Contains(System.IO.Path.GetExtension(filepath), StringComparer.OrdinalIgnoreCase))
								continue;

							VHDLDocument doc = new VHDLLibraryDocument(m_documentTable);
							doc.Filepath = filepath;
							doc.TextDocument = null;
							doc.Project = this;
							doc.Parser.SwitchToSimpleParser(doc);

							lib.Documents.Add(doc);

							doc.Parser.AnalysisComplete += OnLibraryAnalysisCompleted;
						}
						m_libraries.TryAdd(name, lib);
					}
				}
			}
		}

		private void OnLibraryAnalysisCompleted(object sender, AnalysisResultEventArgs e)
		{
			try
			{
				//System.Diagnostics.Debug.WriteLine(string.Format("OnLibraryAnalysisCompleted {0}", e.Parser.Document.Filepath));
				//System.Diagnostics.Debug.WriteLine("{");
				// Get list of library names from the packages in that file
				// Then update them in the library list
				AnalysisResult aresult = e.Result;
				VHDLDocument doc = e.Parser.Document;

				string filepath = doc.Filepath.ToLower();
				string libname = m_libraries.First(x => filepath.StartsWith(x.Value.Path, StringComparison.OrdinalIgnoreCase)).Key;
				LibraryInfo libinfo = m_libraries[libname];

				Dictionary<string, VHDLPackageDeclaration> packageDeclarations = e.Result.Declarations.Values.OfType<VHDLPackageDeclaration>().ToDictionary(x => x.UndecoratedName, StringComparer.Ordinal);

				// List of packages that were in that document, but are not anymore
				var removedList = libinfo.Declaration.Packages.Where(x => !packageDeclarations.ContainsKey(x.Key) && x.Value.Document == doc);
				foreach (var kv in removedList)
				{
					VHDLPackageDeclaration decl;
					if (libinfo.Declaration.Packages.TryRemove(kv.Key, out decl))
					{
						// package removed
						LibraryChanged?.Invoke(this, new VHDLLibraryChangedEventArgs(VHDLLibraryChangedType.PackageRemoved, decl, libinfo.Declaration));
					}
				}
				// Add new packages
				foreach (var kv in packageDeclarations)
				{
					// If there are 2 packages with the same name in the same library
					// the 2nd would replace the 1st
					VHDLPackageDeclaration oldPackage = null;
					libinfo.Declaration.Packages.AddOrUpdate(kv.Key, kv.Value,
						(name, decl) =>
						{
							oldPackage = decl;
							return kv.Value;
						});
					if (oldPackage == null)
						LibraryChanged?.Invoke(this, new VHDLLibraryChangedEventArgs(VHDLLibraryChangedType.PackageAdded, kv.Value, libinfo.Declaration));
					else
						LibraryChanged?.Invoke(this, new VHDLLibraryChangedEventArgs(VHDLLibraryChangedType.PackageUpdated, kv.Value, libinfo.Declaration));
				}

			}
			catch (Exception ex)
			{ }
			//System.Diagnostics.Debug.WriteLine("}");
		}

		private void OnDocumentAnalysisCompleted(object sender, AnalysisResultEventArgs e)
		{
			try
			{
				// Get list of library names from the packages in that file
				// Then update them in the library list
				AnalysisResult aresult = e.Result;
				VHDLDocument doc = e.Parser.Document;

				DocumentChanged?.Invoke(this, new VHDLDocumentEventArgs(doc));
			}
			catch (Exception ex)
			{ }
		}
		// path is the in document path (like IEEE.std_logic_1164)
		public VHDLPackageDeclaration GetLibraryPackage(string name)
		{
			string[] parts = name.Split('.');
			if (parts.Length < 2)
				return null;

			string libName = parts[0];
			string packageName = parts[1];

			LibraryInfo libinfo;
			if (!m_libraries.TryGetValue(libName, out libinfo))
				return null;

			VHDLPackageDeclaration packageDecl;
			if (!libinfo.Declaration.Packages.TryGetValue(packageName, out packageDecl))
				return null;

			return packageDecl;
		}
		public IEnumerable<VHDLLibraryDeclaration> GetAllLibraries()
		{
			return m_libraries.Select(x => x.Value.Declaration);
		}
		public VHDLLibraryDeclaration GetLibrary(string name)
		{
			m_libraries.TryGetValue(name, out var declaration);
			return declaration?.Declaration;
		}

		#region IDisposable
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					m_disposables.ForEach(d => d.Dispose());
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
