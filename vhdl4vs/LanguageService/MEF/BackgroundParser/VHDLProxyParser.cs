/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs
{
    class VHDLProxyParser
        : IVHDLParser
    {
        private IVHDLParser m_parser = null;
        private object m_lock = new object();
        private void OnParseComplete(object sender, ParseResultEventArgs e)
        {
            ParseComplete?.Invoke(sender, e);
        }
        private void OnAnalysisComplete(object sender, AnalysisResultEventArgs e)
        {
            AnalysisComplete?.Invoke(sender, e);
        }
        private void OnDeepAnalysisComplete(object sender, DeepAnalysisResultEventArgs e)
        {
            DeepAnalysisComplete?.Invoke(sender, e);
        }
        public void SwitchToBackgroundParser(VHDLDocument doc, ITextBuffer buffer)
        {
            lock (m_lock)
            {
                if (m_parser is VHDLBackgroundParser)
                    return;

                if (m_parser != null)
                {
                    m_parser.ParseComplete -= OnParseComplete;
                    m_parser.AnalysisComplete -= OnAnalysisComplete;
                    m_parser.DeepAnalysisComplete -= OnDeepAnalysisComplete;
                    m_parser.Dispose();
                }

                m_parser = new VHDLBackgroundParser(buffer, TaskScheduler.Default, doc);
                m_parser.ParseComplete += OnParseComplete;
                m_parser.AnalysisComplete += OnAnalysisComplete;
                m_parser.DeepAnalysisComplete += OnDeepAnalysisComplete;
            }
        }
        public void SwitchToSimpleParser(VHDLDocument doc)
        {
            lock (m_lock)
            {
                if (m_parser is VHDLSimpleParser)
                    return;

                if (m_parser != null)
                {
                    m_parser.ParseComplete -= OnParseComplete;
                    m_parser.AnalysisComplete -= OnAnalysisComplete;
                    m_parser.DeepAnalysisComplete -= OnDeepAnalysisComplete;
                    m_parser.Dispose();
                }

                m_parser = new VHDLSimpleParser(doc);
                m_parser.ParseComplete += OnParseComplete;
                m_parser.AnalysisComplete += OnAnalysisComplete;
                m_parser.DeepAnalysisComplete += OnDeepAnalysisComplete;
            }
        }
        public IVHDLParser Parser
        {
            get
            {
                return m_parser;
            }
        }

        public VHDLProxyParser()
        {

        }

        public ParseResult PResult
        {
            get
            {
                return Parser?.PResult;
            }
        }

        public AnalysisResult AResult
        {
            get
            {
                return Parser?.AResult;
            }
        }

        public DeepAnalysisResult DAResult
        {
            get
            {
                return Parser?.DAResult;
            }
        }

        public VHDLDocument Document
        {
            get
            {
                return Parser?.Document;
            }
        }

        public event EventHandler<ParseResultEventArgs> ParseComplete;
        public event EventHandler<AnalysisResultEventArgs> AnalysisComplete;
        public event EventHandler<DeepAnalysisResultEventArgs> DeepAnalysisComplete;

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
                Parser.Dispose();
            }

            m_disposed = true;
        }
    }
}
