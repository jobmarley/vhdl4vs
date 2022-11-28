/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

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
		private DeepAnalysisResult m_deepAnalysisResult = null;

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

			DeepAnalysisResult result = m_document.Parser.DAResult;
			if (result != null)
				m_deepAnalysisResult = result;
        }
		
        private void OnDeepAnalysisComplete(object sender, DeepAnalysisResultEventArgs e)
		{
			DeepAnalysisResult daresult = e.Result;
			m_deepAnalysisResult = daresult;

			// Should detect changes
			ITextSnapshot snapshot = daresult.Snapshot;
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

		// this is used to quickly find declaration for token in a stream (one after another)
		class TokenStreamHelper
		{
			private VHDLDocument m_document = null;
			private DeepAnalysisResult m_daresult = null;
			private int m_hint = -1;
			public TokenStreamHelper(VHDLDocument document, DeepAnalysisResult daresult)
			{
				m_document = document;
				m_daresult = daresult;
			}
			public VHDLDeclaration GetDeclaration(SnapshotSpan span)
			{
				if (m_daresult == null)
					return null;

				SnapshotSpan oldSpan = span.TranslateTo(m_daresult.Snapshot, SpanTrackingMode.EdgeInclusive);

				if (m_hint == -1)
				{
					m_hint = m_daresult.SortedReferences.UpperBoundIndex(oldSpan.Start);
					if (m_hint == -1)
						m_hint = m_daresult.SortedReferences.Count;
				}

				while (m_hint < m_daresult.SortedReferences.Count)
				{
					var r = m_daresult.SortedReferences.Values[m_hint];
					if (r.Span.Start > oldSpan.Span.Start)
						break;

					if (r.Span == oldSpan.Span)
						return r.Declaration;

					++m_hint;
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

			DeepAnalysisResult daresult = m_deepAnalysisResult;

			TokenStreamHelper helper = new TokenStreamHelper(m_document, daresult);
			foreach (Antlr4.Runtime.IToken token in tokens)
            {
				Span tokenSpan = new Span(token.StartIndex + lineStartIndex, token.Length());

				SnapshotSpan tokenSnapshotSpan = new SnapshotSpan(span.Snapshot, tokenSpan);
				SnapshotSpan? classificationSpan = span.Intersection(tokenSpan);

                if (classificationSpan.HasValue)
                {
                    if (vhdlLexer.DefaultVocabulary.GetSymbolicName(token.Type) == "COMMENT")
                    {
                        result.Add(new ClassificationSpan(classificationSpan.Value, m_standardClassificationService.Comment));
                    }
                    else if (vhdlLexer.DefaultVocabulary.GetSymbolicName(token.Type) == "BASIC_IDENTIFIER")
                    {
						VHDLDeclaration decl = helper.GetDeclaration(new SnapshotSpan(span.Snapshot, tokenSpan));

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