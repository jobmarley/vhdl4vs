//------------------------------------------------------------------------------
// <copyright file="MLClassifier.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Linq;
using System.Text;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Antlr4.Runtime.Misc;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Windows;
using Microsoft.VisualStudio.Debugger.Interop;

namespace vhdl4vs
{
	/// <summary>
	/// Classifier that classifies all text as an instance of the "MLClassifier" classification type.
	/// </summary>
	internal class VHDLClassifier
		: IClassifier
    {
		private IStandardClassificationService m_standardClassificationService = null;
        private IClassificationTypeRegistryService m_classificationService = null;
        //private ITextBuffer m_buffer = null;
		private VHDLDocumentTable m_vhdlDocumentTable = null;

		private ITextSnapshot m_snapshot = null;

		//	name of the "class" and the list of span in which it is defined
		private AnalysisResult m_analysisResult = null;

		private VHDLDocument m_document = null;
		private WeakReference<ITextBuffer> m_textBuffer = null;

		//private FastLexer m_lexer = null;
		/// <summary>
		/// Initializes a new instance of the <see cref="MLClassifier"/> class.
		/// </summary>
		/// <param name="registry">Classification registry.</param>
		internal VHDLClassifier(ITextBuffer buffer, IClassificationTypeRegistryService classificationRegistryService, IStandardClassificationService standardClassificationService, ITextDocumentFactoryService documentService, SVsServiceProvider serviceProvider, VHDLDocumentTable vhdlDocumentTable)
        {
            m_classificationService = classificationRegistryService;
            m_standardClassificationService = standardClassificationService;
			m_vhdlDocumentTable = vhdlDocumentTable;

			m_textBuffer = new WeakReference<ITextBuffer>(buffer);
			//m_lexer = new FastLexer();

			m_document = m_vhdlDocumentTable.GetOrAddDocument(buffer);
			m_document.Parser.DeepAnalysisComplete += OnDeepAnalysisComplete;

			AnalysisResult result = m_document.Parser.AResult;
			if (result != null)
				m_analysisResult = result;
        }
		
        private void OnDeepAnalysisComplete(object sender, DeepAnalysisResultEventArgs e)
		{
			AnalysisResult aresult = e.Result.AnalysisResult;
			m_analysisResult = aresult;

			// Should detect changes
			ITextSnapshot snapshot = aresult.Snapshot;
			ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
		}

        private void OnTextChanged(object sender, TextContentChangedEventArgs e)
        {
            //	Maybe could check for "/*" or "*/" to raise ClassificationChanged.
            //	But thats complicated cause I need to check if this is not a string... etc...

            /*foreach (ITextChange tc in e.Changes)
            {
                if (tc.LineCountDelta > 0)
                {
                    //	lines were added.
                    //	get the line where text was added, and adds lexer states for the added lines.
                    //	Added states are the same as the original line. This way, the new line will be analysed
                    //	and if a change in state is detected, other lines are re-lexed. Otherwise all good.
                    int linePos = e.After.GetLineNumberFromPosition(tc.OldPosition);
                    MSLexerState oldState = m_lineStates[linePos];

                    for (int i = 0; i < tc.LineCountDelta; ++i)
                        m_lineStates.Insert(linePos + 1, oldState);
                }
            }*/
        }
        #region IClassifier

#pragma warning disable 67

        /// <summary>
        /// An event that occurs when the classification of a span of text has changed.
        /// </summary>
        /// <remarks>
        /// This event gets raised if a non-text change would affect the classification in some way,
        /// for example typing /* would cause the classification to change in C# without directly
        /// affecting the span.
        /// </remarks>
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

#pragma warning restore 67

        //	This is the state of the lexer at the end of each line. So we can resume parsing from one line to another.
        //List<MSLexerState> m_lineStates = null;

		class ScopeHelper
		{
			private VHDLDocument m_document = null;
			private AnalysisResult m_aresult = null;
			private int m_hint = -1;
			public ScopeHelper(VHDLDocument document, AnalysisResult aresult)
			{
				m_document = document;
				m_aresult = aresult;
			}
			public VHDLDeclaration GetParentDeclaration(int index)
			{
				if (m_aresult == null)
					return null;

				if (m_hint == -1)
				{
					// First time, need to lookup
					m_hint = m_aresult.SortedScopedDeclarations.LowerBoundIndex(index);
					return (m_hint > -1 && m_hint < m_aresult.SortedScopedDeclarations.Values.Count) ? m_aresult.SortedScopedDeclarations.Values[m_hint] : null;
				}
				else
				{
					// Next, we just increment the index, and check if we got out of declaration
					if (m_hint + 1 < m_aresult.SortedScopedDeclarations.Keys.Count && m_aresult.SortedScopedDeclarations.Keys[m_hint + 1] <= index)
					{
						// entered next declaration
						++m_hint;
					}
					return m_aresult.SortedScopedDeclarations.Values[m_hint];
				}
			}

			// Find declaration in stuff imported from external libraries
			public VHDLDeclaration GetExternalDeclaration(VHDLDeclaration parentDeclaration, string text)
			{
				if (!(parentDeclaration is VHDLDesignUnit))
					return null;

				foreach (string useClause in (parentDeclaration as VHDLDesignUnit).UseClauses.Select(x => x.Name?.GetClassifiedText()?.Text).Prepend("STD.STANDARD.ALL"))
				{
					if (useClause == null)
						continue;

					string[] parts = useClause.Split('.');
					if (parts.Length < 3)
						continue;
					string libraryName = parts[0];
					string packageName = parts[1];
					string declarationName = parts[2];

					if (declarationName.ToLower() == "all" || string.Compare(declarationName, text, true) == 0)
					{
						// search for declaration if it was imported
						try
						{
							VHDLDocument doc = m_document.Project?.GetLibraryPackage(libraryName + "." + packageName)?.Document;
							string path = packageName + "@declaration." + text;
							if (doc?.Parser?.AResult?.Declarations.ContainsKey(path) == true)
								return doc.Parser.AResult.Declarations[path];

							path = packageName + "@body." + text;
							if (doc?.Parser?.AResult?.Declarations.ContainsKey(path) == true)
								return doc.Parser.AResult.Declarations[path];
						}
						catch (Exception)
						{

						}
					}

				}

				return null;
			}
		}
		/// <summary>
		/// Gets all the <see cref="ClassificationSpan"/> objects that intersect with the given range of text.
		/// </summary>
		/// <remarks>
		/// This method scans the given SnapshotSpan for potential matches for this classification.
		/// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
		/// </remarks>
		/// <param name="span">The span currently being classified.</param>
		/// <returns>A list of ClassificationSpans that represent spans identified to be of this classification.</returns>
		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            List<ClassificationSpan> result = new List<ClassificationSpan>();

			int lineStartIndex = span.Start.GetContainingLine().Start;
			int lineEndIndex = span.End.GetContainingLine().End;

			vhdlLexer lexer = new vhdlLexer(new Antlr4.Runtime.AntlrInputStream(span.Snapshot.GetText(lineStartIndex, lineEndIndex - lineStartIndex)));
			IList<Antlr4.Runtime.IToken> tokens = lexer.GetAllTokens();
			//m_lexer.UpdateTokens(span.Snapshot);

			AnalysisResult aresult = m_analysisResult;

			ScopeHelper scopeHelper = new ScopeHelper(m_document, aresult);

			foreach (Antlr4.Runtime.IToken token in tokens)
            {
				Span tokenSpan = new Span(token.StartIndex + lineStartIndex, token.Length());
				VHDLDeclaration parentDeclaration = scopeHelper.GetParentDeclaration(tokenSpan.Start);

                //Antlr4.Runtime.IToken token = tokens[i];
				SnapshotSpan tokenSnapshotSpan = new SnapshotSpan(span.Snapshot, tokenSpan);
				SnapshotSpan? classificationSpan = span.Intersection(tokenSpan);

                if (classificationSpan.HasValue)
                {
                    //System.Diagnostics.Debug.WriteLine("token text : \"" + token.Text + "\", type = " + lexer.Vocabulary.GetSymbolicName(token.Type));
                    if (vhdlLexer.DefaultVocabulary.GetSymbolicName(token.Type) == "COMMENT")
                    {
                        result.Add(new ClassificationSpan(classificationSpan.Value, m_standardClassificationService.Comment));
                    }
                    else if (vhdlLexer.DefaultVocabulary.GetSymbolicName(token.Type) == "BASIC_IDENTIFIER")
                    {
						VHDLDeclaration decl = null;
						try
						{
							decl = VHDLDeclarationUtilities.FindName(parentDeclaration, token.Text);
						}
						catch (Exception)
						{
						}

						if (decl is VHDLEntityDeclaration)
						{
							result.Add(new ClassificationSpan(classificationSpan.Value, m_classificationService.GetClassificationType("vhdl.entity")));
						}
						else if (decl is VHDLArchitectureDeclaration)
						{
							result.Add(new ClassificationSpan(classificationSpan.Value, m_classificationService.GetClassificationType("vhdl.architecture")));
						}
                        else if (decl is VHDLTypeDeclaration || decl is VHDLSubTypeDeclaration)
                        {
                            result.Add(new ClassificationSpan(classificationSpan.Value, m_classificationService.GetClassificationType("vhdl.type")));
                        }
                        else if (decl is VHDLConstantDeclaration)
                        {
                            result.Add(new ClassificationSpan(classificationSpan.Value, m_classificationService.GetClassificationType("vhdl.constant")));
                        }
						else if(decl is VHDLVariableDeclaration)
						{
							result.Add(new ClassificationSpan(classificationSpan.Value, m_classificationService.GetClassificationType("vhdl.variable")));
						}
                        else if (decl is VHDLSignalDeclaration)
                        {
                            result.Add(new ClassificationSpan(classificationSpan.Value, m_classificationService.GetClassificationType("vhdl.signal")));
                        }
						else if (decl is VHDLPortDeclaration)
						{
							result.Add(new ClassificationSpan(classificationSpan.Value, m_classificationService.GetClassificationType("vhdl.port")));
						}
                    }
                    else if(vhdlLexer.DefaultVocabulary.GetSymbolicName(token.Type) == "STRING_LITERAL" || vhdlLexer.DefaultVocabulary.GetSymbolicName(token.Type) == "CHARACTER_LITERAL")
                    {
                        result.Add(new ClassificationSpan(classificationSpan.Value, m_standardClassificationService.StringLiteral));
                    }
                    else if (vhdlLexer.DefaultVocabulary.GetSymbolicName(token.Type) == "BIT_STRING_LITERAL")
                    {
                        SnapshotSpan string_span = new SnapshotSpan(classificationSpan.Value.Start + 1, classificationSpan.Value.Length - 1);
                        result.Add(new ClassificationSpan(string_span, m_standardClassificationService.StringLiteral));
                    }
					else if (VHDLLanguageUtils.Keywords.Contains(tokenSnapshotSpan.GetText()))
					{
						result.Add(new ClassificationSpan(classificationSpan.Value, m_standardClassificationService.Keyword));
					}
				}
            }
            return result;
        }

		#endregion
	}
}