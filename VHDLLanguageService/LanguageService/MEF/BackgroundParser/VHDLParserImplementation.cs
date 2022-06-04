using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using Antlr4.Runtime;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System.Diagnostics;

namespace MyCompany.LanguageServices.VHDL
{
	class VHDLParserErrorListener
		: IAntlrErrorListener<IToken>
	{
		public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
		{
			m_errors.Add(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, msg, offendingSymbol.GetSpan()));
		}

		private List<VHDLError> m_errors = new List<VHDL.VHDLError>();
		public IList<VHDLError> Errors
		{
			get
			{
				return m_errors;
			}
		}
	}

	class VHDLLexerErrorListener
		: IAntlrErrorListener<int>
	{
		public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
		{
			m_errors.Add(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, msg, new Span(offendingSymbol, 1)));
		}

		private List<VHDLError> m_errors = new List<VHDL.VHDLError>();
		public IList<VHDLError> Errors
		{
			get
			{
				return m_errors;
			}
		}
	}

	/*
	 * This is the logic part of the parser code. Since there is no multiple inheritance
	 * I did it like that to avoid code duplication.
	 */
	abstract class VHDLParserImplementation
		: IVHDLParser
	{
		protected VHDLDocumentTable DocumentTable { get; set; }
		public VHDLDocument Document { get; private set; }
		public ParseResult PResult { get; private set; }
		public AnalysisResult AResult { get; private set; }
		public DeepAnalysisResult DAResult { get; private set; }

		protected VHDLParserImplementation(VHDLDocument document)
		{
			UseDeepAnalysis = true;

			Document = document;
			DocumentTable = document.DocumentTable;

			// Not sure if DocumentAdded/Removed are necessary...
			VHDLProject proj = Document.Project;
			//Debug.WriteLine(string.Format("ParserImplementation created, document: {0}, project: {1}", document.Filepath, proj?.UnconfiguredProject?.FullPath ?? "null"));
			if (proj != null)
			{
				proj.LibraryChanged += OnLibraryChanged;
				proj.DocumentChanged += OnDocumentChanged;
			}
		}

		private HashSet<string> m_usedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		void OnLibraryChanged(object sender, VHDLLibraryChangedEventArgs e)
		{
			string fullName = e.LibraryName + "." + e.Package.UndecoratedName;
			if (m_usedPackages.Contains(fullName))
			{
				// packages cannot depend on themselves. Just a security though, it should should not be in the list to begin with
				if (e.Package.Document != Document)
					RequestDeepAnalysis(); // need update
			}
		}
		void OnDocumentChanged(object sender, VHDLDocumentEventArgs e)
		{
			//	When an analysis of an external document completes, the list of declared elements changes,
			//	thus we requires a new deep analysis on this document
			if (e.Document != Document && !(Document is VHDLLibraryDocument))
			{
				RequestDeepAnalysis();
			}
		}
		void UpdateUsedPackages(AnalysisResult result)
		{
			HashSet<string> usedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (Document.Project?.GetLibraryPackage("STD.STANDARD")?.Document != Document)
				usedPackages.Add("STD.STANDARD");
			foreach (var designUnit in result.DeclarationsByContext.Values.OfType<VHDLDesignUnit>())
			{
				foreach(string usedName in designUnit.UseClauses.Select(x => x.Name?.GetClassifiedText()?.Text))
				{
					if (usedName == null)
						continue;
					string[] parts = usedName.Split('.');
					if (parts.Length < 2)
						continue;
					string packageFullName = string.Join(".", parts.Take(2));
					usedPackages.Add(packageFullName);
				}
			}
			if (!m_usedPackages.SetEquals(usedPackages))
			{
				m_usedPackages = usedPackages;
				RequestDeepAnalysis();
			}
		}

		public event EventHandler<ParseResultEventArgs> ParseComplete;
		public event EventHandler<AnalysisResultEventArgs> AnalysisComplete;
		public event EventHandler<DeepAnalysisResultEventArgs> DeepAnalysisComplete;

		IEnumerable<Span> ParseComments(vhdlLexer lexer)
        {
			lexer.Reset();
			List<Span> comments = new List<Span>();

			IToken token = null;
			int startIndex = -1;
			int startLine = -1;
			int stopIndex = -1;
			int stopLine = -1;
			while (!lexer.HitEOF)
            {
				token = lexer.NextToken();
				if (lexer.HitEOF)
					break;
				if (vhdlLexer.DefaultVocabulary.GetSymbolicName(token.Type) == "COMMENT")
                {
					if (startIndex == -1)
					{
						startIndex = token.StartIndex;
						startLine = token.Line;
						stopIndex = token.StopIndex;
						stopLine = token.Line;
					}
					else
					{
						stopIndex = token.StopIndex;
						stopLine = token.Line;
					}
                }
				else
                {
					if (startIndex != -1)
					{
						int lineCount = stopLine + 1 - startLine;
						if (lineCount > 1)
							comments.Add(new Span(startIndex, stopIndex + 1 - startIndex));
						startIndex = -1;
						startLine = -1;
						stopIndex = -1;
						stopLine = -1;
					}
				}
			}

			return comments;

		}
		private ParseResult Parse(TextReader reader)
		{
			System.Diagnostics.Debug.WriteLine(string.Format("Parsing file {0}", Document.Filepath));
			Stopwatch sw = new Stopwatch();
			sw.Start();
			vhdlLexer lexer = new vhdlLexer(new Antlr4.Runtime.AntlrInputStream(reader));
			VHDLLexerErrorListener lexerErrorListener = new VHDLLexerErrorListener();
			lexer.AddErrorListener(lexerErrorListener);

			Antlr4.Runtime.CommonTokenStream tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
			vhdlParser parser = new vhdlParser(tokenStream);
			VHDLParserErrorListener parserErrorListener = new VHDLParserErrorListener();
			parser.AddErrorListener(parserErrorListener);

			vhdlParser.Design_fileContext context = parser.design_file();

			List<VHDLError> errors = new List<VHDL.VHDLError>();
			errors.AddRange(lexerErrorListener.Errors);
			errors.AddRange(parserErrorListener.Errors);

			//	Raise event
			ParseResult result = new ParseResult();
			result.Document = Document;
			result.Errors = errors;
			result.Tree = context;
			result.TokenStream = tokenStream;

			result.Comments = ParseComments(lexer);

			sw.Stop();
			Debug.WriteLine(string.Format("Parsing of file {0} finished in {1}ms", Document.Filepath, sw.ElapsedMilliseconds));
			return result;
		}

		private AnalysisResult Analyse(ParseResult parseResult)
		{
			Debug.WriteLine(string.Format("Analyzing file {0}", Document.Filepath));

			Stopwatch sw = new Stopwatch();
			sw.Start();

			AnalysisResult result = new AnalysisResult();
			VHDLDeclarationVisitor visitor = new VHDLDeclarationVisitor(result, (x) => result.Errors.Add(x));
			try
			{
				visitor.Visit(parseResult.Tree);
			}
			catch (Exception e)
			{
				VHDLLogger.LogException(e);
			}

			result.ParseResult = parseResult;
			result.Tree = parseResult.Tree;
			result.Snapshot = parseResult.Snapshot;
			result.DeclarationsByContext = visitor.DeclarationsByContext;
			result.SortedScopedDeclarations = visitor.SortedScopedDeclarations;

			// Build TreePath => Declaration map
			result.Declarations = new Dictionary<string, VHDLDeclaration>(StringComparer.OrdinalIgnoreCase);
			foreach (VHDLDeclaration d in result.DeclarationsByContext.Values)
			{
				result.Declarations[d.TreePath] = d;
			}

			sw.Stop();
			Debug.WriteLine(string.Format("Analysis of file {0} finished in {1}ms", Document.Filepath, sw.ElapsedMilliseconds));
			return result;
		}

		private DeepAnalysisResult DeepAnalyse(AnalysisResult analysisResult)
		{
			Debug.WriteLine(string.Format("Deep analyzing file {0}", Document.Filepath));
			//if (analysisResult.Snapshot == null) // DeepAnalysis doesn't really makes sense if document is not displayed 
			//	return null;

			Stopwatch sw = new Stopwatch();
			sw.Start();

			// We need the boolean type declaration so we can return that type for relational operation evaluation in deep analysis
			try
			{
				VHDLDeclaration booleanDecl = Document.Project?.GetLibraryPackage("STD.STANDARD")?.AnalysisResult?.Declarations?["STANDARD@declaration.BOOLEAN"];
				if (booleanDecl != null && booleanDecl is VHDLTypeDeclaration)
					analysisResult.BooleanType = new VHDLReferenceType(booleanDecl);
			}
			catch (Exception e)
			{
				VHDLLogger.LogException(e);
			}
			try
			{
				VHDLDeclaration timeDecl = Document.Project?.GetLibraryPackage("STD.STANDARD")?.AnalysisResult?.Declarations?["STANDARD@declaration.TIME"];
				if (timeDecl != null && timeDecl is VHDLTypeDeclaration)
					analysisResult.TimeType = new VHDLReferenceType(timeDecl);
			}
			catch (Exception e)
			{
				VHDLLogger.LogException(e);
			}

			DeepAnalysisResult result = new DeepAnalysisResult();
			result.AnalysisResult = analysisResult;
			result.Tree = analysisResult.Tree;
			result.Snapshot = analysisResult.Snapshot;


			// init base references
			foreach (VHDLDeclaration decl in analysisResult.DeclarationsByContext.Values)
			{
				if (decl.NameContext != null)
					result.SortedReferences.Add(decl.NameContext.Start.StartIndex, new VHDLNameReference(decl.UndecoratedName, decl.NameContext.GetSpan(), decl));
			}


			ConcurrentBag<VHDLError> errors = new ConcurrentBag<VHDLError>();
			List<Task> tasks = new List<Task>();
			// Resolve stuff that needs to be resolved (means looking into other documents)
			foreach (IVHDLToResolve r in analysisResult.ToResolve)
			{
				try
				{
					r.Resolve(result, (x) => errors.Add(x));
				}
				catch (Exception e)
				{
					VHDLLogger.LogException(e);
				}
			}

			// Wait to resolve because it's necessary for next steps
			// /!\ Deep analysis should not be synchronously awaited upon because of this. But that should never happen
			Task.WaitAll(tasks.ToArray());
			tasks.Clear();
			foreach (VHDLStatement statement in analysisResult.StatementsByContext.Values)
			{
				tasks.Add(Task.Run(() =>
				{
					try
					{
						statement.Check(x => errors.Add(x));
					}
					catch (Exception e)
					{
						VHDLLogger.LogException(e);
					}
				}));
			}

			foreach (VHDLDeclaration decl in analysisResult.DeclarationsByContext.Values)
			{
				tasks.Add(Task.Run(() =>
				{
					try
					{
						decl.Check(result, x => errors.Add(x));
					}
					catch (Exception e)
					{
						VHDLLogger.LogException(e);
					}
				}));

			}

			Task.WaitAll(tasks.ToArray());
			result.Errors.AddRange(errors);

			sw.Stop();
			Debug.WriteLine(string.Format("Deep analysis of file {0} finished in {1}ms", Document.Filepath, sw.ElapsedMilliseconds));

			return result;
		}



		protected abstract ITextSnapshot GetSnapshot();
		protected abstract string GetText();

		protected abstract void MarkDirty();

		private bool m_parseDirty = true;
		private bool m_analysisDirty = true;
		private bool m_deepAnalysisDirty = true;
		protected void RequestParse()
		{
			//System.Diagnostics.Debug.WriteLine(string.Format("RequestParse {0}", Document.Filepath));
			m_parseDirty = true;
			MarkDirty();
		}

		protected void RequestAnalysis()
		{
			//System.Diagnostics.Debug.WriteLine(string.Format("RequestAnalysis {0}", Document.Filepath));
			m_analysisDirty = true;
			MarkDirty();
		}

		protected void RequestDeepAnalysis()
		{
			//System.Diagnostics.Debug.WriteLine(string.Format("RequestDeepAnalysis {0}", Document.Filepath));
			m_deepAnalysisDirty = true;
			MarkDirty();
		}

		private UInt64 m_version = 0;
		protected void ReParseImpl()
		{
			UInt64 version = ++m_version;

			//	Run analysis if its dirty
			bool parseDirty = m_parseDirty;
			if (parseDirty)
			{
				ITextSnapshot snapshot = GetSnapshot();

				m_parseDirty = false;
				m_analysisDirty = true;

				ParseResult presult = null;
				if (snapshot != null)
					presult = Parse(new TextSnapshotToTextReader(snapshot));
				else
					presult = Parse(new StringReader(GetText()));

				presult.Snapshot = snapshot;
				presult.Version = version;
				PResult = presult;
				ParseComplete?.Invoke(this, new ParseResultEventArgs(this, presult));
			}

			//	Run analysis if its dirty
			bool analysisDirty = m_analysisDirty;
			if (analysisDirty)
			{
				m_analysisDirty = false;
				m_deepAnalysisDirty = true;

				AnalysisResult aresult = Analyse(PResult);
				aresult.Version = version;
				AResult = aresult;
				//System.Diagnostics.Debug.WriteLine(string.Format("AnalysisComplete.Invoke {0}", Document.Filepath));
				//System.Diagnostics.Debug.WriteLine("{");
				AnalysisComplete?.Invoke(this, new AnalysisResultEventArgs(this, aresult));
				//System.Diagnostics.Debug.WriteLine("}");
				UpdateUsedPackages(aresult);
			}

			//	Run deep analysis if its dirty
			bool deepAnalysisDirty = m_deepAnalysisDirty;
			if (deepAnalysisDirty && UseDeepAnalysis)
			{
				m_deepAnalysisDirty = false;

				DeepAnalysisResult daresult = DeepAnalyse(AResult);
				daresult.Version = version;
				DAResult = daresult;

				//System.Diagnostics.Debug.WriteLine(string.Format("DeepAnalysisComplete.Invoke {0}", Document.Filepath));
				//System.Diagnostics.Debug.WriteLine("{");
				DeepAnalysisComplete?.Invoke(this, new DeepAnalysisResultEventArgs(this, daresult));
				//System.Diagnostics.Debug.WriteLine("}");
			}
		}

		public bool UseDeepAnalysis { get; set; }

		private bool m_disposed = false;
		public virtual void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
			}

			m_disposed = true;
		}
	}
}
