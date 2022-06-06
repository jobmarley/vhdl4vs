using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs
{
	class VHDLIndentVisitor
	: vhdlBaseVisitor<int>
	{
		private int m_position;
		private ITextSnapshot m_snapshot;

		public VHDLIndentVisitor(
			int position,
			ITextSnapshot snapshot)
		{
			m_position = position;
			m_snapshot = snapshot;
		}

		protected override int DefaultResult
		{
			get { return 0; }
		}
		protected override int AggregateResult(int aggregate, int nextResult)
		{
			return aggregate + nextResult;
		}
		protected override bool ShouldVisitNextChild(IRuleNode node, int currentResult)
		{
			return base.ShouldVisitNextChild(node, currentResult);
		}

		public override int VisitEntity_declaration([NotNull] vhdlParser.Entity_declarationContext context)
		{
			try
			{
				if (m_position >= context.Start.StartIndex && m_position < context.Stop.StopIndex)
				{
					return base.VisitEntity_declaration(context) + 1;
				}
				else
				{
					return 0;
				}
			}
			catch (Exception e)
			{
				return 0;
			}
		}

		public override int VisitIf_statement([NotNull] vhdlParser.If_statementContext context)
		{
			try
			{
				if (m_position >= context.Start.StartIndex && m_position < context.Stop.StopIndex)
				{
					return base.VisitIf_statement(context) + 1;
				}
				else
				{
					return 0;
				}
			}
			catch (Exception e)
			{
				return 0;
			}
		}

		public override int VisitArchitecture_body([NotNull] vhdlParser.Architecture_bodyContext context)
		{
			try
			{
				if (m_position >= context.Start.StartIndex && m_position < context.Stop.StopIndex)
				{
					return base.VisitArchitecture_body(context) + 1;
				}
				else
				{
					return 0;
				}
			}
			catch (Exception e)
			{
				return 0;
			}
		}

		public override int VisitProcess_statement([NotNull] vhdlParser.Process_statementContext context)
		{
			try
			{
				if (m_position >= context.Start.StartIndex && m_position < context.Stop.StopIndex)
				{
					return base.VisitProcess_statement(context) + 1;
				}
				else
				{
					return 0;
				}
			}
			catch (Exception e)
			{
				return 0;
			}
		}

		public override int VisitPackage_body([NotNull] vhdlParser.Package_bodyContext context)
		{
			try
			{
				if (m_position >= context.Start.StartIndex && m_position < context.Stop.StopIndex)
				{
					return base.VisitPackage_body(context) + 1;
				}
				else
				{
					return 0;
				}
			}
			catch (Exception e)
			{
				return 0;
			}
		}

		public override int VisitPackage_declaration([NotNull] vhdlParser.Package_declarationContext context)
		{
			try
			{
				if (m_position >= context.Start.StartIndex && m_position < context.Stop.StopIndex)
				{
					return base.VisitPackage_declaration(context) + 1;
				}
				else
				{
					return 0;
				}
			}
			catch (Exception e)
			{
				return 0;
			}
		}

		public override int VisitSubprogram_body([NotNull] vhdlParser.Subprogram_bodyContext context)
		{
			try
			{
				if (m_position >= context.Start.StartIndex && m_position < context.Stop.StopIndex)
				{
					return base.VisitSubprogram_body(context) + 1;
				}
				else
				{
					return 0;
				}
			}
			catch (Exception e)
			{
				return 0;
			}
		}
	}
}
