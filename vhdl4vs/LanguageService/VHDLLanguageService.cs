using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;

namespace vhdl4vs
{
    /*
     * This is a legacy language service. Its only used for the language option page.
     * Other modern MEF language services seems to use it for options too (like c#).
     * */
    [Guid("9037BCED-AB42-4672-8264-D79F84595C21")]
    class VHDLLanguageService
        : LanguageService
    {
        private LanguagePreferences m_preferences = null;

        public VHDLLanguageService()
        {

        }


        public override IScanner GetScanner(IVsTextLines buffer)
        {
            return null;
        }

        public override AuthoringScope ParseSource(ParseRequest req)
        {
            return null;
        }

        public override string Name
        {
            get { return "VHDL"; }
        }

        public override string GetFormatFilterList()
        {
            return "VHDL files (*.vhd)|*.vhd";
        }

        public override LanguagePreferences GetLanguagePreferences()
        {
            if (m_preferences == null)
            {
                m_preferences = new LanguagePreferences(this.Site,
                                                        typeof(VHDLLanguageService).GUID,
                                                        this.Name);
                m_preferences.Init();
            }
            return m_preferences;
        }
    }
}
