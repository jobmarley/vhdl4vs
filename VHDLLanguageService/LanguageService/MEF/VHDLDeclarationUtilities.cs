using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.VisualStudio.Text;

namespace vhdl4vs
{
	class VHDLNameNotFoundException
		: VHDLCodeException
	{
		public VHDLNameNotFoundException(string message, Span span)
			: base(message, span)
		{
		}
	}
	class VHDLDeclarationUtilities
	{
		public static VHDLDeclaration GetBody(VHDLDeclaration d)
		{
			if (d is VHDLPackageBodyDeclaration || d is VHDLFunctionBodyDeclaration || d is VHDLProcedureBodyDeclaration)
				return d;

			if (d.TreePath.EndsWith("@declaration"))
			{
				var s = d.TreePath.Split('@');
				string bodyPath = string.Join("@", s.Take(s.Length - 1)) + "@body";
				if (d.AnalysisResult.Declarations.ContainsKey(bodyPath))
					return d.AnalysisResult.Declarations[bodyPath];
			}

			if (d is VHDLFunctionDeclaration fd && fd?.Parent is VHDLPackageDeclaration)
			{
				VHDLPackageDeclaration packageDecl = fd.Parent as VHDLPackageDeclaration;
				string basePath = packageDecl.Body.TreePath + "." + fd.UndecoratedName;
				for (int i = 1; true; ++i)
				{
					string path = basePath + "@" + i.ToString();
					if (!packageDecl.AnalysisResult.Declarations.ContainsKey(path))
						break;
					VHDLFunctionBodyDeclaration bodyDeclaration = packageDecl.AnalysisResult.Declarations[path] as VHDLFunctionBodyDeclaration;
					if (bodyDeclaration == null)
						continue;

					if (fd?.ReturnType?.Dereference() != bodyDeclaration?.ReturnType?.Dereference())
						continue;

					if (fd.Parameters.Zip(bodyDeclaration.Parameters, (x, y) => x.Type.Dereference() == y.Type.Dereference()).All(b => b))
						return bodyDeclaration;
				}
			}
			else if (d is VHDLProcedureDeclaration procedureDecl && procedureDecl?.Parent is VHDLPackageDeclaration)
			{
				VHDLPackageDeclaration packageDecl = procedureDecl.Parent as VHDLPackageDeclaration;
				string basePath = packageDecl.Body.TreePath + "." + procedureDecl.UndecoratedName;
				for (int i = 1; true; ++i)
				{
					string path = basePath + "@" + i.ToString();
					if (!packageDecl.AnalysisResult.Declarations.ContainsKey(path))
						break;
					VHDLProcedureBodyDeclaration bodyDeclaration = packageDecl.AnalysisResult.Declarations[path] as VHDLProcedureBodyDeclaration;
					if (bodyDeclaration == null)
						continue;

					if (procedureDecl.Parameters.Zip(bodyDeclaration.Parameters, (x, y) => x.Type.Dereference() == y.Type.Dereference()).All(b => b))
						return bodyDeclaration;
				}
			}
			return null;
		}
		// Return best match based on parameters
		public static SubprogramType GetBestMatch<SubprogramType>(IEnumerable<SubprogramType> functionList, params VHDLType[] argTypes)
			where SubprogramType : VHDLSubprogramDeclaration
		{
			foreach (var function in functionList)
			{
				if (function.Parameters.Count != argTypes.Count())
					continue;

				if (function.Parameters.Zip(argTypes, (x, y) => { var comp = VHDLType.AreCompatible(x.Type, y); return comp == VHDLCompatibilityResult.Yes || comp == VHDLCompatibilityResult.Unsure; }).All(b => b))
					return function;
			}

			return null;
		}

		public static IEnumerable<VHDLDeclaration> GetAllMemberDeclarations(VHDLDeclaration previousDecl, string name)
		{
			AnalysisResult analysisResult = previousDecl?.Document?.Parser?.AResult;
			if (analysisResult == null)
			{
				// bad perf but that happens with virtual declarations
				foreach (var decl in previousDecl.Children)
					if (string.Compare(decl.UndecoratedName, name, StringComparison.OrdinalIgnoreCase) == 0)
						yield return decl;
				yield break;
			}

			if (previousDecl is VHDLAbstractVariableDeclaration avd)
			{
				if (avd.Type.Dereference() is VHDLRecordType recordType)
				{
					foreach (var record in recordType.Fields.Where(x => string.Compare(x.Name, name, true) == 0))
						yield return record;
				}
			}

			string path = previousDecl.TreePath + "." + name;
			if (analysisResult.Declarations.ContainsKey(path))
			{
				yield return analysisResult.Declarations[path];
			}
			if (analysisResult.Declarations.ContainsKey(path + "@declaration"))
			{
				yield return analysisResult.Declarations[path + "@declaration"];
			}
			if (analysisResult.Declarations.ContainsKey(path + "@body"))
			{
				yield return analysisResult.Declarations[path + "@body"];
			}
			for (int i = 1; analysisResult.Declarations.ContainsKey(path + "@" + i.ToString()); ++i)
				yield return analysisResult.Declarations[path + "@" + i.ToString()];

			yield break;
		}
		public static VHDLDeclaration GetMemberDeclaration(VHDLDeclaration previousDecl, string name)
		{
			return GetAllMemberDeclarations(previousDecl, name).FirstOrDefault();
		}

		// Carefull that the point and analysisResult are from the same snapshot if there is one
		public static VHDLDeclaration GetEnclosingDeclaration(AnalysisResult analysisResult, int point)
		{
			if (analysisResult == null)
				return null;

			int i = analysisResult.SortedScopedDeclarations.LowerBoundIndex(point);
			if (i == -1)
				return null;
			return analysisResult.SortedScopedDeclarations.Values[i];
		}
		public static VHDLDeclaration GetExternalDeclaration(VHDLDeclaration parent, string name)
		{
			return GetAllExternalDeclarations(parent, name).FirstOrDefault();
		}
		public static IEnumerable<VHDLDeclaration> GetAllExternalDeclarations(VHDLDeclaration parent, string name)
		{
			if (!(parent is VHDLDesignUnit))
				yield break;

			VHDLDesignUnit designUnit = (VHDLDesignUnit)parent;
			foreach (string usedName in designUnit.UseClauses.Select(x => x.Name?.GetClassifiedText()?.Text).Prepend("STD.STANDARD.ALL"))
			{
				if (usedName == null)
					continue;
				string[] parts = usedName.Split('.');
				if (parts.Length < 3)
					continue;

				string libraryName = parts[0];
				string packageName = parts[1];
				string entityName = parts[2];

				VHDLDocument doc = null;
				if (string.Compare(libraryName, "work", true) == 0)
					doc = parent.Document.DocumentTable.EnumerateSiblings(parent.Document).FirstOrDefault(x => x?.Parser?.AResult?.Declarations?.ContainsKey(packageName + "@declaration") == true);
				else
					doc = parent.Document?.Project?.GetLibraryPackage(libraryName + "." + packageName)?.Document;

				AnalysisResult ar = doc?.Parser?.AResult;
				if (ar == null)
					continue;

				if (entityName.ToLower() != "all" && string.Compare(entityName, name, StringComparison.OrdinalIgnoreCase) != 0)
					continue;

				if (name == packageName && ar.Declarations.ContainsKey(packageName + "@declaration"))
					yield return ar.Declarations[packageName + "@declaration"];

				string path = packageName + "@declaration." + name;
				if (ar.Declarations.ContainsKey(path))
					yield return ar.Declarations[path];
				for (int i = 1; ar.Declarations.ContainsKey(path + "@" + i.ToString()); ++i)
					yield return ar.Declarations[path + "@" + i.ToString()];
			}
		}
		// This is less optimized than the name version
		public static IEnumerable<VHDLDeclaration> GetAllExternalDeclarations(VHDLDeclaration parent, Func<VHDLDeclaration, bool> predicate)
		{
			if (!(parent is VHDLDesignUnit))
				yield break;


			VHDLDesignUnit designUnit = (VHDLDesignUnit)parent;
			foreach (string usedName in designUnit.UseClauses.Select(x => x.Name?.GetClassifiedText()?.Text).Prepend("STD.STANDARD.ALL"))
			{
				if (usedName == null)
					continue;
				string[] parts = usedName.Split('.');
				if (parts.Length < 3)
					continue;

				string libraryName = parts[0];
				string packageName = parts[1];
				string entityName = parts[2];

				VHDLDocument doc = null;
				if (string.Compare(libraryName, "work", true) == 0)
					doc = parent.Document.DocumentTable.EnumerateSiblings(parent.Document).FirstOrDefault(x => x?.Parser?.AResult?.Declarations?.ContainsKey(packageName + "@declaration") == true);
				else
					doc = parent.Document?.Project?.GetLibraryPackage(libraryName + "." + packageName)?.Document;

				AnalysisResult ar = doc?.Parser?.AResult;
				if (ar == null)
					continue;

				if (entityName.ToLower() == "all")
				{
					foreach (var v in ar.Declarations[packageName + "@declaration"].Children.Where(x => predicate(x)))
						yield return v;
				}
				else
				{
					string path = packageName + "@declaration." + entityName;
					if (ar.Declarations.ContainsKey(path) && predicate(ar.Declarations[path]))
						yield return ar.Declarations[path];
				}
			}
			// Try to look in associated entity if we are in an architecture
			if (parent is VHDLArchitectureDeclaration)
			{
				VHDLArchitectureDeclaration archDecl = parent as VHDLArchitectureDeclaration;
				foreach (var v in GetAllExternalDeclarations(archDecl.EntityDeclaration, predicate))
					yield return v;
			}
			// Try to look in package decl if we are in a package body
			if (parent is VHDLPackageBodyDeclaration)
			{
				string declarationPath = parent.TreePath.Substring(0, parent.TreePath.Length - "@body".Length) + "@declaration";
				if (parent.AnalysisResult.Declarations.ContainsKey(declarationPath))
				{
					foreach (var v in GetAllExternalDeclarations(parent.AnalysisResult.Declarations[declarationPath], predicate))
						yield return v;
				}
			}
		}
		public static VHDLDeclaration GetExternalDeclaration(VHDLDeclaration parent, Func<VHDLDeclaration, bool> predicate)
		{
			return GetAllExternalDeclarations(parent, predicate).FirstOrDefault();
		}


		// Return the declaration that matches the predicate
		public static VHDLDeclaration Find(VHDLDeclaration enclosingDeclaration, Func<VHDLDeclaration, bool> predicate)
		{
			// Can do that because FindAllNames uses yield
			return FindAll(enclosingDeclaration, predicate).FirstOrDefault();
		}
		// Find all declarations that match the predicate, this is less optimized than the name version
		private static IEnumerable<VHDLDeclaration> FindAllImpl(VHDLDeclaration enclosingDeclaration, Func<VHDLDeclaration, bool> predicate, HashSet<VHDLDeclaration> visited)
		{
			AnalysisResult analysisResult = enclosingDeclaration?.AnalysisResult;
			if (analysisResult == null)
				yield break;


			foreach (var v in enclosingDeclaration.Children.Where(x => predicate(x)))
				yield return v;

			if (predicate(enclosingDeclaration))
				yield return enclosingDeclaration;

			if (enclosingDeclaration is VHDLArchitectureDeclaration archDecl && archDecl.EntityDeclaration != null)
			{
				foreach (var v in FindAllImpl(archDecl.EntityDeclaration, predicate, visited))
					yield return v;
			}
			if (enclosingDeclaration is VHDLPackageBodyDeclaration packageBodyDecl && packageBodyDecl.Declaration != null)
			{
				foreach (var v in FindAllImpl(packageBodyDecl.Declaration, predicate, visited))
					yield return v;
			}

			foreach (var v in GetAllExternalDeclarations(enclosingDeclaration, predicate))
				yield return v;
			foreach (var v in FindAllImpl(enclosingDeclaration.Parent, predicate, visited))
				yield return v;

			if (enclosingDeclaration.Parent is VHDLFileDeclaration)
			{
				// Check if name exist in same project documents
				// we do this here not to repeat it for each parent scope
				var siblingARs = enclosingDeclaration.Document.DocumentTable.EnumerateSiblings(enclosingDeclaration.Document).Where(x => x != enclosingDeclaration.Document).Select(x => x?.Parser?.AResult).Where(x => x != null);
				// x.Declarations.Values.First() is the file declaration
				foreach (var v in siblingARs.Select(x => x.Declarations.Values.FirstOrDefault()).Where(x => x != null).SelectMany(x => x.Children).Where(x => predicate(x)))
					yield return v;
			}
		}

		public static IEnumerable<VHDLDeclaration> FindAll(VHDLDeclaration enclosingDeclaration, Func<VHDLDeclaration, bool> predicate)
		{
			return FindAllImpl(enclosingDeclaration, predicate, new HashSet<VHDLDeclaration>());
		}
		// Find a simple name (not stuff like <package>.<entity>.<name>, just <name>)
		public static VHDLDeclaration FindName(VHDLDeclaration enclosingDeclaration, string name)
		{
			// Can do that because FindAllNames uses yield
			return FindAllNames(enclosingDeclaration, name).FirstOrDefault();
		}

		private static IEnumerable<VHDLDeclaration> FindAllNamesImpl(VHDLDeclaration enclosingDeclaration, string name, HashSet<VHDLDeclaration> visited)
		{
			AnalysisResult analysisResult = enclosingDeclaration?.AnalysisResult;
			if (analysisResult == null || visited.Contains(enclosingDeclaration))
				yield break;

			visited.Add(enclosingDeclaration);

			if (!(enclosingDeclaration is VHDLFileDeclaration))
			{
				if (string.Compare(enclosingDeclaration.UndecoratedName, name, true) == 0)
					yield return enclosingDeclaration;

				string currentPath = enclosingDeclaration.TreePath + "." + name;
				if (analysisResult.Declarations.ContainsKey(currentPath))
					yield return analysisResult.Declarations[currentPath];

				// look for name@declaration and name@body and name@i
				if (analysisResult.Declarations.ContainsKey(currentPath + "@declaration"))
					yield return analysisResult.Declarations[currentPath + "@declaration"];
				if (analysisResult.Declarations.ContainsKey(currentPath + "@body"))
					yield return analysisResult.Declarations[currentPath + "@body"];
				for (int i = 1; analysisResult.Declarations.ContainsKey(currentPath + "@" + i.ToString()); ++i)
					yield return analysisResult.Declarations[currentPath + "@" + i.ToString()];

				if (enclosingDeclaration is VHDLArchitectureDeclaration archDecl && archDecl.EntityDeclaration != null)
				{
					foreach (var v in FindAllNamesImpl(archDecl.EntityDeclaration, name, visited))
						yield return v;
				}
				if (enclosingDeclaration is VHDLPackageBodyDeclaration packageBodyDecl && packageBodyDecl.Declaration != null)
				{
					foreach (var v in FindAllNamesImpl(packageBodyDecl.Declaration, name, visited))
						yield return v;
				}

				foreach (var v in GetAllExternalDeclarations(enclosingDeclaration, name))
					yield return v;
				foreach (var v in FindAllNamesImpl(enclosingDeclaration.Parent, name, visited))
					yield return v;
			}
			else
			{
				string currentPath = name;
				if (analysisResult.Declarations.ContainsKey(currentPath))
					yield return analysisResult.Declarations[currentPath];
				if (analysisResult.Declarations.ContainsKey(currentPath + "@declaration"))
					yield return analysisResult.Declarations[currentPath + "@declaration"];
				if (analysisResult.Declarations.ContainsKey(currentPath + "@body"))
					yield return analysisResult.Declarations[currentPath + "@body"];

				// Check if name exist in same project documents
				// we do this here not to repeat it for each parent scope
				var siblingARs = enclosingDeclaration.Document.DocumentTable.EnumerateSiblings(enclosingDeclaration.Document).Where(x => x != enclosingDeclaration.Document).Select(x => x?.Parser?.AResult).Where(x => x != null);
				var ars = siblingARs.Where(x => x.Declarations.ContainsKey(name));
				foreach (var v in ars.Select(x => x.Declarations[name]))
					yield return v;
			}
		}

		// Find a simple name (not stuff like <package>.<entity>.<name>, just <name>)
		// Use yield for performances
		public static IEnumerable<VHDLDeclaration> FindAllNames(VHDLDeclaration enclosingDeclaration, string name)
		{
			return FindAllNamesImpl(enclosingDeclaration, name, new HashSet<VHDLDeclaration>());
		}
		// Get the declaration for the name at point
		public static VHDLDeclaration GetDeclaration(VHDLDocument document, SnapshotPoint point)
		{
			DeepAnalysisResult dar = document?.Parser?.DAResult;
			if (dar == null || dar.Snapshot == null)
				return null;

			int position = point.TranslateTo(dar.Snapshot, PointTrackingMode.Positive).Position;
			VHDLNameReference r = dar.SortedReferences.LowerBoundOrDefault(position);
			if (r?.Span.Contains(position) == true)
				return r.Declaration;

			return null;
		}

		//	Populate the TextBlock with correctly formated (spaced) and colored text(keyword, etc)
		public static string FormatTree(IParseTree tree, System.Windows.Controls.TextBlock textBlock = null, string suffix = "")
		{
			if (tree == null)
				return null;
			VHDLQuickFormatVisitor visitor = new VHDLQuickFormatVisitor(textBlock);
			visitor.Visit(tree);

			if (!string.IsNullOrEmpty(suffix))
			{
				textBlock?.Inlines.Add(VHDLQuickInfoHelper.text(suffix));
				return visitor.Text + suffix;
			}

			return visitor.Text;
		}
	}
}
