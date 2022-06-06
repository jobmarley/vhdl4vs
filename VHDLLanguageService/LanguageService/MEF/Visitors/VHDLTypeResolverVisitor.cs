using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vhdl4vs.TypeVisitors
{
	class VHDLTypeResolverVisitor
		: vhdlBaseVisitor<VHDLType>
	{
		private AnalysisResult m_analysisResult = null;
		private Action<VHDLError> m_errorListener = null;
		public VHDLTypeResolverVisitor(AnalysisResult analysisResult, Action<VHDLError> errorListener)
		{
			m_analysisResult = analysisResult;
			m_errorListener = errorListener;
		}
		public override VHDLType VisitType_definition([NotNull] vhdlParser.Type_definitionContext context)
		{
			if (context.scalar_type_definition() != null)
			{
				VHDLScalarTypeResolverVisitor visitor = new VHDLScalarTypeResolverVisitor(m_analysisResult, m_errorListener);
				return visitor.Visit(context.scalar_type_definition());
			}
			else if (context.composite_type_definition() != null)
			{
				VHDLCompositeTypeResolverVisitor visitor = new VHDLCompositeTypeResolverVisitor(m_analysisResult, m_errorListener);
				return visitor.Visit(context.composite_type_definition());
			}
			else if (context.access_type_definition() != null)
			{
				return null;
			}
			else if (context.file_type_definition() != null)
			{
				return null;
			}
			return null;
		}
		public override VHDLType VisitDiscrete_range([NotNull] vhdlParser.Discrete_rangeContext context)
		{
			if (context.range_decl() != null)
			{
				return Visit(context.range_decl());
			}
			else if (context.subtype_indication() != null)
			{
				VHDLTypeResolverVisitor indexTypeVisitor = new VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
				return indexTypeVisitor.Visit(context.subtype_indication());
			}
			return null;
		}
		public override VHDLType VisitRange_decl([NotNull] vhdlParser.Range_declContext context)
		{
			// only range, we must parse it, then deduce type from the operands
			VHDLRangeResolverVisitor rangeVisitor = new VHDLRangeResolverVisitor(m_analysisResult, m_errorListener);
			VHDLScalarType scalarType = new VHDLScalarType(false);
			scalarType.Range = rangeVisitor.Visit(context);
			return scalarType;
		}

		public override VHDLType VisitSubtype_indication([NotNull] vhdlParser.Subtype_indicationContext context)
		{
			// This is type referencing with constraints
			VHDLReferenceType type = new VHDLReferenceType();
			// if several names, context.selected_name()[0] is a resolution function 
			type.Expression = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener).Visit(context.selected_name().Last()) as VHDLReferenceExpression;

			if (context.constraint() != null)
			{
				if (context.constraint().index_constraint() != null)
				{
					List<VHDLType> indexTypes = new List<VHDLType>();
					foreach (var indexConstraint in context.constraint()?.index_constraint()?.discrete_range() ?? Array.Empty<vhdlParser.Discrete_rangeContext>())
					{
						indexTypes.Add(Visit(indexConstraint));
					}
					return new VHDLIndexConstrainedType(type, indexTypes);
				}
				else if (context.constraint().range_constraint() != null)
				{
					VHDLScalarType scalarType = new VHDLScalarType(false);
					scalarType.Type = type;
					VHDLRangeResolverVisitor visitor = new VHDLRangeResolverVisitor(m_analysisResult, m_errorListener);
					scalarType.Range = visitor.Visit(context.constraint().range_constraint());
					return scalarType;
				}
			}

			return type;
		}

		public override VHDLType VisitSubnature_indication([NotNull] vhdlParser.Subnature_indicationContext context)
		{
			// This is type referencing with constraints
			VHDLReferenceType type = new VHDLReferenceType(new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener).Visit(context.name()) as VHDLReferenceExpression);

			if (context.index_constraint() != null)
			{
				List<VHDLType> indexTypes = new List<VHDLType>();
				foreach (var indexConstraint in context.index_constraint()?.discrete_range() ?? Array.Empty<vhdlParser.Discrete_rangeContext>())
				{
					indexTypes.Add(Visit(indexConstraint));
				}
				return new VHDLIndexConstrainedType(type, indexTypes);
			}

			return type;
		}

	}


	class VHDLCompositeTypeResolverVisitor
		: vhdlBaseVisitor<VHDLType>
	{
		private AnalysisResult m_analysisResult = null;
		private Action<VHDLError> m_errorListener = null;
		public VHDLCompositeTypeResolverVisitor(AnalysisResult analysisResult, Action<VHDLError> errorListener)
		{
			m_analysisResult = analysisResult;
			m_errorListener = errorListener;
		}
		protected override bool ShouldVisitNextChild(IRuleNode node, VHDLType currentResult)
		{
			return false;
		}
		protected override VHDLType AggregateResult(VHDLType aggregate, VHDLType nextResult)
		{
			return null;
		}
		public override VHDLType VisitComposite_type_definition([NotNull] vhdlParser.Composite_type_definitionContext context)
		{
			if (context.array_type_definition() != null)
				return VisitArray_type_definition(context.array_type_definition());
			else if (context.record_type_definition() != null)
				return VisitRecord_type_definition(context.record_type_definition());
			return null;
		}
		public override VHDLType VisitArray_type_definition([NotNull] vhdlParser.Array_type_definitionContext context)
		{
			VHDLArrayTypeResolverVisitor visitor = new VHDLArrayTypeResolverVisitor(m_analysisResult, m_errorListener);
			return visitor.Visit(context);
		}
		public override VHDLType VisitRecord_type_definition([NotNull] vhdlParser.Record_type_definitionContext context)
		{
			VHDLRecordType type = new VHDLRecordType();
			return type;
		}

	}
	class VHDLArrayTypeResolverVisitor
		: vhdlBaseVisitor<VHDLArrayType>
	{
		private AnalysisResult m_analysisResult = null;
		private Action<VHDLError> m_errorListener = null;
		public VHDLArrayTypeResolverVisitor(AnalysisResult analysisResult, Action<VHDLError> errorListener)
		{
			m_analysisResult =	analysisResult;
			m_errorListener = errorListener;
		}
		protected override bool ShouldVisitNextChild(IRuleNode node, VHDLArrayType currentResult)
		{
			return false;
		}
		protected override VHDLArrayType AggregateResult(VHDLArrayType aggregate, VHDLArrayType nextResult)
		{
			return null;
		}
		public override VHDLArrayType VisitArray_type_definition([NotNull] vhdlParser.Array_type_definitionContext context)
		{
			if (context.constrained_array_definition() != null)
				return VisitConstrained_array_definition(context.constrained_array_definition());
			else if (context.unconstrained_array_definition() != null)
				return VisitUnconstrained_array_definition(context.unconstrained_array_definition());
			return null;
		}
		public override VHDLArrayType VisitConstrained_array_definition([NotNull] vhdlParser.Constrained_array_definitionContext context)
		{
			VHDLTypeResolverVisitor visitor = new VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
			VHDLType elemType = visitor.Visit(context.subtype_indication());

			List<VHDLType> indexTypes = new List<VHDLType>();
			foreach (var indexConstraint in context.index_constraint()?.discrete_range() ?? Array.Empty<vhdlParser.Discrete_rangeContext>())
			{
				indexTypes.Add(visitor.Visit(indexConstraint));
			}
			return new VHDLArrayType(elemType, indexTypes); 
		}
		public override VHDLArrayType VisitUnconstrained_array_definition([NotNull] vhdlParser.Unconstrained_array_definitionContext context)
		{
			VHDLTypeResolverVisitor visitor = new VHDLTypeResolverVisitor(m_analysisResult, m_errorListener);
			VHDLType elemType = visitor.Visit(context.subtype_indication());
			List<VHDLType> indexTypes = new List<VHDLType>();
			foreach (var x in context.index_subtype_definition())
			{
				// hmmm weird, are they all unconstrained? according to vhdl.g4, yes.
				VHDLUnconstrainedType indexType = new VHDLUnconstrainedType();
				indexType.Type = new VHDLReferenceType(new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener).Visit(x.name()) as VHDLReferenceExpression);
				indexTypes.Add(indexType);
			}

			return new VHDLArrayType(elemType, indexTypes);
		}
	}
	class VHDLScalarTypeResolverVisitor
		: vhdlBaseVisitor<VHDLType>
	{
		private AnalysisResult m_analysisResult = null;
		private Action<VHDLError> m_errorListener = null;
		public VHDLScalarTypeResolverVisitor(AnalysisResult analysisResult, Action<VHDLError> errorListener)
		{
			m_analysisResult = analysisResult;
			m_errorListener = errorListener;
		}
		protected override bool ShouldVisitNextChild(IRuleNode node, VHDLType currentResult)
		{
			return false;
		}
		protected override VHDLType AggregateResult(VHDLType aggregate, VHDLType nextResult)
		{
			return null;
		}
		public override VHDLType VisitScalar_type_definition([NotNull] vhdlParser.Scalar_type_definitionContext context)
		{
			if (context.enumeration_type_definition() != null)
			{
				VHDLEnumerationResolverVisitor visitor = new VHDLEnumerationResolverVisitor(m_analysisResult, m_errorListener);
				return visitor.Visit(context.enumeration_type_definition());
			}
			else if(context.range_constraint() != null)
			{
				VHDLScalarType type = new VHDLScalarType(true);
				VHDLRangeResolverVisitor visitor = new VHDLRangeResolverVisitor(m_analysisResult, m_errorListener);
				type.Range = visitor.Visit(context.range_constraint());
				return type;
			}
			else if(context.physical_type_definition() != null)
			{
				VHDLScalarType type = new VHDLScalarType(true);
				VHDLRangeResolverVisitor visitor = new VHDLRangeResolverVisitor(m_analysisResult, m_errorListener);
				type.Range = visitor.Visit(context.physical_type_definition().range_constraint());

				var unitIdContexts = context.physical_type_definition()?.secondary_unit_declaration()?.Select(x => x.identifier()) ?? Array.Empty<vhdlParser.IdentifierContext>();
				unitIdContexts = unitIdContexts.Prepend(context.physical_type_definition()?.base_unit_declaration()?.identifier());
				foreach (var identifierContext in unitIdContexts)
					if (identifierContext != null)
						type.Units.Add(identifierContext.GetText());
				return type;
			}
			return null;
		}
	}

	class VHDLEnumerationResolverVisitor
		: vhdlBaseVisitor<VHDLEnumerationType>
	{
		private AnalysisResult m_analysisResult = null;
		private Action<VHDLError> m_errorListener = null;
		public VHDLEnumerationResolverVisitor(AnalysisResult analysisResult, Action<VHDLError> errorListener)
		{
			m_analysisResult = analysisResult;
			m_errorListener = errorListener;
		}
		protected override bool ShouldVisitNextChild(IRuleNode node, VHDLEnumerationType currentResult)
		{
			return false;
		}
		protected override VHDLEnumerationType AggregateResult(VHDLEnumerationType aggregate, VHDLEnumerationType nextResult)
		{
			return null;
		}
		// enumeration
		public override VHDLEnumerationType VisitEnumeration_type_definition([NotNull] vhdlParser.Enumeration_type_definitionContext context)
		{
			VHDLEnumerationType type = new VHDLEnumerationType();
			foreach(var x in context.enumeration_literal())
			{
				if (x.CHARACTER_LITERAL() != null)
				{
					type.Values.Add(new VHDLCharEnumerationValue(type, new VHDLCharacterLiteral(m_analysisResult, x.CHARACTER_LITERAL().Symbol.GetSpan(), x.CHARACTER_LITERAL().GetText()[1])));
				}
				else if (x.identifier() != null)
				{
					type.Values.Add(new VHDLNameEnumerationValue(type, new VHDLEnumerationValueDeclaration(m_analysisResult, x.identifier(), x.identifier().GetText(), type)));
				}
			}
			return type;
		}

	}
	class VHDLRangeResolverVisitor
		: vhdlBaseVisitor<VHDLRange>
	{
		private AnalysisResult m_analysisResult = null;
		private Action<VHDLError> m_errorListener = null;
		public VHDLRangeResolverVisitor(AnalysisResult analysisResult, Action<VHDLError> errorListener)
		{
			m_analysisResult = analysisResult;
			m_errorListener = errorListener;
		}
		protected override bool ShouldVisitNextChild(IRuleNode node, VHDLRange currentResult)
		{
			return false;
		}
		protected override VHDLRange AggregateResult(VHDLRange aggregate, VHDLRange nextResult)
		{
			return null;
		}

		public override VHDLRange VisitRange_constraint([NotNull] vhdlParser.Range_constraintContext context)
		{
			if (context.range_decl() != null)
				return VisitRange_decl(context.range_decl());
			return null;
		}
		public override VHDLRange VisitRange_decl([NotNull] vhdlParser.Range_declContext context)
		{
			if (context.explicit_range() != null)
				return VisitExplicit_range(context.explicit_range());
			else if (context.name() != null)
				return null; // IDK what that is
							 // I think this is only the first part of (x DOWNTO y)
							 // maybe, (0 to name()) idk ?
							 // doesnt make sense, maybe just a name that already have a range?
			return null;
		}
		public override VHDLRange VisitExplicit_range([NotNull] vhdlParser.Explicit_rangeContext context)
		{
			// Not supported yet, just return empty expression
			VHDLRange range = new VHDLRange();
			ExpressionVisitors.VHDLExpressionVisitor visitor = new ExpressionVisitors.VHDLExpressionVisitor(m_analysisResult, m_errorListener);
			range.Start = visitor.Visit(context.simple_expression()[0]);
			if (context.simple_expression().Length > 1)
			{
				range.Direction = context.direction()?.DOWNTO() == null ? VHDLRangeDirection.To : VHDLRangeDirection.DownTo;
				range.End = visitor.Visit(context.simple_expression()[1]);
			}
			return range;
		}

		public override VHDLRange VisitDiscrete_range([NotNull] vhdlParser.Discrete_rangeContext context)
		{
			if (context.range_decl() != null)
				return VisitRange_decl(context.range_decl());
			else if (context.subtype_indication() != null)
			{
				return null;
				//VHDLTypeResolverVisitor visitor = new VHDLTypeResolverVisitor();
				//return visitor.Visit(context.subtype_indication());
			}
			return null;
		}
	}
}
