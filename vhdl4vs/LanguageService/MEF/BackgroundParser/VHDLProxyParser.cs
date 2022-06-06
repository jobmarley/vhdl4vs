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
        public IVHDLParser Parser
        {
            get
            {
                return m_parser;
            }
            set
            {
                lock (m_lock)
                {
                    IVHDLParser p = m_parser;
                    if (p != null)
                    {
                        p.ParseComplete -= OnParseComplete;
                        p.AnalysisComplete -= OnAnalysisComplete;
                        p.DeepAnalysisComplete -= OnDeepAnalysisComplete;
                    }
                    if (value != null)
                    {
                        value.ParseComplete += OnParseComplete;
                        value.AnalysisComplete += OnAnalysisComplete;
                        value.DeepAnalysisComplete += OnDeepAnalysisComplete;
                    }
                    m_parser = value;
                }
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
