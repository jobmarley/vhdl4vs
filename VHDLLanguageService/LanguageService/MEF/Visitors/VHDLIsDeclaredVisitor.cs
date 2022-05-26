using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime;

namespace MyCompany.LanguageServices.VHDL
{
	//class VHDLIsDeclaredVisitor
	//   : vhdlBaseVisitor<bool>
	//{
	//	private AnalysisResult m_analysisResult = null;
	//	private List<VHDLError> m_errors = new List<VHDLError>();

	//	public List<VHDLError> Errors { get { return m_errors; } }
	//	private List<vhdlParser.Selected_nameContext> m_usedNames = new List<vhdlParser.Selected_nameContext>();

	//	private VHDLDocument m_document = null;
	//	public VHDLIsDeclaredVisitor(VHDLDocument document, AnalysisResult analysisResult)
	//	{
	//		m_document = document;
	//		m_analysisResult = analysisResult;
	//	}

	//	public override bool VisitEntity_declaration([NotNull] vhdlParser.Entity_declarationContext context)
	//	{
	//		if (context.identifier().Length >= 2)
	//		{
	//			vhdlParser.IdentifierContext idContext1 = context.identifier()?[0];
	//			vhdlParser.IdentifierContext idContext2 = context.identifier()?[1];
	//			if (idContext1 != null && idContext2 != null &&
	//				idContext1.GetText() != idContext2.GetText())
	//			{
	//				int start = idContext2.Start.StartIndex;
	//				int stop = idContext2.Stop.StopIndex;
	//				m_errors.Add(new VHDLError(0, "The name '" + idContext2.GetText() + "' does not match the name of the entity", start, stop));
	//			}
	//		}
	//		return base.VisitEntity_declaration(context);
	//	}
	//	public override bool VisitArchitecture_body([NotNull] vhdlParser.Architecture_bodyContext context)
	//	{
	//		if (context.identifier().Length >= 3)
	//		{
	//			vhdlParser.IdentifierContext idContext1 = context.identifier()?[0];
	//			vhdlParser.IdentifierContext idContext2 = context.identifier()?[2];
	//			if (idContext1 != null && idContext2 != null &&
	//				idContext1.GetText() != idContext2.GetText())
	//			{
	//				int start = idContext2.Start.StartIndex;
	//				int stop = idContext2.Stop.StopIndex;
	//				m_errors.Add(new VHDLError(0, "The name '" + idContext2.GetText() + "' does not match the name of the architecture", start, stop));
	//			}
	//		}
	//		return base.VisitArchitecture_body(context);
	//	}
	//	public override bool VisitSubprogram_body([NotNull] vhdlParser.Subprogram_bodyContext context)
	//	{
	//		vhdlParser.DesignatorContext designatorContext1 = context.subprogram_specification()?.procedure_specification()?.designator();
	//		vhdlParser.DesignatorContext designatorContext2 = context.subprogram_specification()?.function_specification()?.designator();
	//		if (designatorContext1 != null && context.designator() != null)
	//		{
	//			if(designatorContext1.GetText() != context.designator().GetText())
	//			{
	//				int start = context.designator().Start.StartIndex;
	//				int stop = context.designator().Stop.StopIndex;
	//				m_errors.Add(new VHDLError(0, "The name '" + context.designator().GetText() + "' does not match the name of the procedure", start, stop));
	//			}
	//		}
	//		else if(designatorContext2 != null && context.designator() != null)
	//		{
	//			if (designatorContext2.GetText() != context.designator().GetText())
	//			{
	//				int start = context.designator().Start.StartIndex;
	//				int stop = context.designator().Stop.StopIndex;
	//				m_errors.Add(new VHDLError(0, "The name '" + context.designator().GetText() + "' does not match the name of the function", start, stop));
	//			}
	//		}
	//		return base.VisitSubprogram_body(context);
	//	}
	//	public override bool VisitSubtype_indication([NotNull] vhdlParser.Subtype_indicationContext context)
	//	{
	//		// If there are 2, 1st is the resolution function (must be a function)
	//		// and 2nd must be a type name
	//		vhdlParser.Selected_nameContext functionNameContext = context.selected_name().Length > 1 ? context.selected_name(0) : null;
	//		vhdlParser.Selected_nameContext typeNameContext = context.selected_name().Length > 0 ? (context.selected_name().Length > 1 ? context.selected_name(1) : context.selected_name(0)) : null;
	//		if (functionNameContext != null)
	//		{
	//			SnapshotPoint point = new SnapshotPoint(m_analysisResult.Snapshot, functionNameContext.Start.StartIndex);
	//			VHDLDeclaration decl = VHDLDeclarationUtilities.GetDeclaration(m_document, point);
	//			if (decl == null)
	//			{
	//				int start = functionNameContext.Start.StartIndex;
	//				int stop = functionNameContext.Stop.StopIndex;
	//				m_errors.Add(new VHDLError(0, "The name '" + functionNameContext.GetText() + "' does not exist in the current context", start, stop));
	//			}
	//			else if(!(decl is VHDLFunctionDeclaration))
	//			{
	//				int start = functionNameContext.Start.StartIndex;
	//				int stop = functionNameContext.Stop.StopIndex;
	//				m_errors.Add(new VHDLError(0, "The name '" + functionNameContext.GetText() + "' must be a function", start, stop));
	//			}
	//		}
	//		if (typeNameContext != null)
	//		{
	//			//SnapshotPoint point = new SnapshotPoint(m_analysisResult.Snapshot, typeNameContext.Start.StartIndex);
	//			/*VHDLDeclarationLocation decl = VHDLDeclarationUtilities.GetDeclaration(m_document, point);
	//			if (decl == null)
	//			{
	//				int start = typeNameContext.Start.StartIndex;
	//				int stop = typeNameContext.Stop.StopIndex;
	//				m_errors.Add(new ParseError(0, "The name '" + typeNameContext.GetText() + "' does not exist in the current context", start, stop));
	//			}*/
	//		}
	//		return base.VisitSubtype_indication(context);
	//	}
	//	/*public override bool VisitSelected_name([NotNull] vhdlParser.Selected_nameContext context)
	//	{
	//		string name = context.identifier()?.GetText();
	//		if (!m_analysisResult.ConstantList.ContainsKey(name) &&
	//			!m_analysisResult.ArchitectureList.ContainsKey(name) &&
	//			!m_analysisResult.EntityList.ContainsKey(name) &&
	//			!m_analysisResult.PortList.ContainsKey(name) &&
	//			!m_analysisResult.SignalList.ContainsKey(name) &&
	//			!m_analysisResult.TypeList.ContainsKey(name) &&
	//			!m_analysisResult.VariableList.ContainsKey(name))
	//		{
	//			int start = context.Start.StartIndex;
	//			int stop = context.Stop.StopIndex;
	//			m_errors.Add(new ParseError(0, "The name '" + name + "' does not exist in the current context", start, stop));
	//		}
	//		return base.VisitSelected_name(context);
	//	}*/
	//	/*public override bool VisitName([NotNull] vhdlParser.NameContext context)
	//	{
	//		vhdlParser.Selected_nameContext selectedNameContext = context.selected_name();
	//		if (selectedNameContext != null)
	//		{
	//			string name = selectedNameContext.identifier()?.GetText();
	//			if (!m_analysisResult.ConstantList.ContainsKey(name) &&
	//				!m_analysisResult.ArchitectureList.ContainsKey(name) &&
	//				!m_analysisResult.EntityList.ContainsKey(name) &&
	//				!m_analysisResult.PortList.ContainsKey(name) &&
	//				!m_analysisResult.SignalList.ContainsKey(name) &&
	//				!m_analysisResult.TypeList.ContainsKey(name) &&
	//				!m_analysisResult.VariableList.ContainsKey(name))
	//			{
	//				int start = selectedNameContext.Start.StartIndex;
	//				int stop = selectedNameContext.Stop.StopIndex;
	//				m_errors.Add(new ParseError(0, "The name '" + name + "' does not exist in the current context", start, stop));
	//			}
	//		}
	//		return base.VisitName(context);
	//	}*/
	//	/*public override bool VisitConcurrent_signal_assignment_statement([NotNull] vhdlParser.Concurrent_signal_assignment_statementContext context)
	//	{
	//		vhdlParser.IdentifierContext identifierContext = context.conditional_signal_assignment()?.target()?.name()?.selected_name()?.identifier();
	//		if (identifierContext != null)
	//		{
	//			string name = identifierContext.GetText();
	//			int start = identifierContext.Start.StartIndex;
	//			int stop = identifierContext.Stop.StopIndex;

	//			if (!m_analysisResult.SignalList.ContainsKey(name) ||
	//				m_analysisResult.SignalList[name].FirstOrDefault(v => v.Span.Contains(start)) == null)
	//			{
	//				m_errors.Add(new ParseError(0, "The name '" + name + "' does not exist in the current context", start, stop));
	//			}
	//		}
	//		return base.VisitConcurrent_signal_assignment_statement(context);
	//	}
	//	public override bool VisitSignal_assignment_statement([NotNull] vhdlParser.Signal_assignment_statementContext context)
	//	{
	//		vhdlParser.IdentifierContext identifierContext = context.target()?.name()?.selected_name()?.identifier();
	//		if (identifierContext != null)
	//		{
	//			string name = identifierContext.GetText();
	//			int start = identifierContext.Start.StartIndex;
	//			int stop = identifierContext.Stop.StopIndex;

	//			if (!m_analysisResult.SignalList.ContainsKey(name) ||
	//				m_analysisResult.SignalList[name].FirstOrDefault(v => v.Span.Contains(start)) == null)
	//			{
	//				m_errors.Add(new ParseError(0, "The name '" + name + "' does not exist in the current context", start, stop));
	//			}
	//		}
	//		return base.VisitSignal_assignment_statement(context);
	//	}*/
	//}
}
