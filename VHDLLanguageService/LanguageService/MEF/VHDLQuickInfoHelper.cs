using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;

namespace vhdl4vs
{
	public class VHDLQuickInfoHelper
	{
		static private IClassificationFormatMapService m_classificationFormatMapService = null;
		static private IClassificationTypeRegistryService m_classificationTypeRegistryService = null;
		static private IGlyphService m_glyphService = null;
		static private bool m_isInitialized = false;

		// Check C:\Program Files\Microsoft Visual Studio\2022\Community\VSSDK\VisualStudioIntegration\Tools\Bin\ImageLibraryViewer\ImageLibraryViewer.exe
		// or download image library, and use the html file to have KnownMoniker
		static public readonly ImageElement KeywordImageElement = new ImageElement(new ImageId(new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"), Microsoft.VisualStudio.Imaging.KnownImageIds.KeywordSnippet));
		static public readonly ImageElement VariableImageElement = new ImageElement(new ImageId(new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"), Microsoft.VisualStudio.Imaging.KnownImageIds.Field));
		static public readonly ImageElement MethodImageElement = new ImageElement(new ImageId(new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"), Microsoft.VisualStudio.Imaging.KnownImageIds.Method));
		static public readonly ImageElement ClassImageElement = new ImageElement(new ImageId(new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"), Microsoft.VisualStudio.Imaging.KnownImageIds.Class));
		static public readonly ImageElement ConstantImageElement = new ImageElement(new ImageId(new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"), Microsoft.VisualStudio.Imaging.KnownImageIds.Constant));
		static public readonly ImageElement PackageImageElement = new ImageElement(new ImageId(new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"), Microsoft.VisualStudio.Imaging.KnownImageIds.Namespace));

		static private IClassificationFormatMap m_tooltipClassificationFormatMap = null;
		/*public VHDLQuickInfoHelper()
		{

		}
		public VHDLQuickInfoHelper(IClassificationFormatMapService classificationFormatMapService,
			IClassificationTypeRegistryService classificationTypeRegistryService,
			IGlyphService glyphService)
		{
		}*/

		// Must be called by MEF component that use it
		static public void Initialize(IClassificationFormatMapService classificationFormatMapService,
			IClassificationTypeRegistryService classificationTypeRegistryService,
			IGlyphService glyphService)
		{
			if (!m_isInitialized)
			{
				m_classificationFormatMapService = classificationFormatMapService;
				m_classificationTypeRegistryService = classificationTypeRegistryService;
				m_glyphService = glyphService;
				Reload();
				m_isInitialized = true;
			}
		}

		static public void Reload()
		{
			m_tooltipClassificationFormatMap = m_classificationFormatMapService.GetClassificationFormatMap("tooltip");
			m_keywordFormat = m_tooltipClassificationFormatMap.GetTextProperties(m_classificationTypeRegistryService.GetClassificationType("keyword"));
			m_symbolFormat = m_tooltipClassificationFormatMap.GetTextProperties(m_classificationTypeRegistryService.GetClassificationType("symbol definition"));
			m_textFormat = m_tooltipClassificationFormatMap.GetTextProperties(m_classificationTypeRegistryService.GetClassificationType("text"));
			m_stringFormat = m_tooltipClassificationFormatMap.GetTextProperties(m_classificationTypeRegistryService.GetClassificationType("string"));
			m_constantFormat = m_tooltipClassificationFormatMap.GetTextProperties(m_classificationTypeRegistryService.GetClassificationType("vhdl.constant"));
			m_signalFormat = m_tooltipClassificationFormatMap.GetTextProperties(m_classificationTypeRegistryService.GetClassificationType("vhdl.signal"));
			m_typeFormat = m_tooltipClassificationFormatMap.GetTextProperties(m_classificationTypeRegistryService.GetClassificationType("vhdl.type"));

			m_glyphVariable = m_glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);
			m_glyphMethod = m_glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
			m_glyphClass = m_glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic);
			m_glyphConstant = m_glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupConstant, StandardGlyphItem.GlyphItemPublic);
			m_glyphPackage = m_glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupNamespace, StandardGlyphItem.GlyphItemPublic);
		}

		static private TextFormattingRunProperties m_textFormat = null;
		static private TextFormattingRunProperties m_keywordFormat = null;
		static private TextFormattingRunProperties m_constantFormat = null;
		static private TextFormattingRunProperties m_symbolFormat = null;
		static private TextFormattingRunProperties m_stringFormat = null;
		static private TextFormattingRunProperties m_signalFormat = null;
		static private TextFormattingRunProperties m_typeFormat = null;

		static private ImageSource m_glyphVariable = null;
		static private ImageSource m_glyphMethod = null;
		static private ImageSource m_glyphClass = null;
		static private ImageSource m_glyphConstant = null;
		static private ImageSource m_glyphPackage = null;

		static private Run RunWithFormat(string text, TextFormattingRunProperties format)
		{
			Run run = new Run(text);
			run.SetTextProperties(format);
			return run;
		}
		static public Run keyword(string text)
		{
			return RunWithFormat(text, m_keywordFormat);
		}
		static public Run text(string text)
		{
			return RunWithFormat(text, m_textFormat);
		}
		static public Run symbol(string text)
		{
			return RunWithFormat(text, m_symbolFormat);
		}
		static public Run constant(string text)
		{
			return RunWithFormat(text, m_constantFormat);
		}
		static public Run signal(string text)
		{
			return RunWithFormat(text, m_signalFormat);
		}
		static public Run str(string text)
		{
			return RunWithFormat(text, m_stringFormat);
		}
		static public Run type(string text)
		{
			return RunWithFormat(text, m_typeFormat);
		}
		static public Run RunFromClassificationType(string text, string classificationType)
		{
			IClassificationType type = m_classificationTypeRegistryService.GetClassificationType(classificationType);
			if (type == null)
				type = m_classificationTypeRegistryService.GetClassificationType("text");
			TextFormattingRunProperties format = m_tooltipClassificationFormatMap.GetTextProperties(type);
			return RunWithFormat(text, format);
		}
		static private InlineUIContainer InlineGlyph(ImageSource img)
		{
			Image i = new Image();
			i.Stretch = Stretch.None;
			i.Source = img;
			InlineUIContainer uiContainer = new InlineUIContainer(i);
			uiContainer.BaselineAlignment = BaselineAlignment.Top;
			return uiContainer;
		}
		static public ImageSource glyphVariableImage()
		{
			return m_glyphVariable;
		}
		static public InlineUIContainer glyphVariable()
		{
			return InlineGlyph(m_glyphVariable);
		}
		static public ImageSource glyphClassImage()
		{
			return m_glyphClass;
		}
		static public InlineUIContainer glyphClass()
		{
			return InlineGlyph(m_glyphClass);
		}
		static public ImageSource glyphMethodImage()
		{
			return m_glyphMethod;
		}
		static public InlineUIContainer glyphMethod()
		{
			return InlineGlyph(m_glyphMethod);
		}
		static public ImageSource glyphConstantImage()
		{
			return m_glyphConstant;
		}
		static public InlineUIContainer glyphConstant()
		{
			return InlineGlyph(m_glyphConstant);
		}
		static public ImageSource glyphPackageImage()
		{
			return m_glyphPackage;
		}
		static public InlineUIContainer glyphPackage()
		{
			return InlineGlyph(m_glyphPackage);
		}
	}
}