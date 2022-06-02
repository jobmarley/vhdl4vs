using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;

using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Adornments;
using System.Collections.Concurrent;

namespace MyCompany.LanguageServices.VHDL
{
	class VHDLEvaluationContext
	{
		public Dictionary<VHDLDeclaration, VHDLExpression> DeclarationValues { get; } = new Dictionary<VHDLDeclaration, VHDLExpression>();
	}

	abstract class VHDLDeclaration
	{
		public VHDLDeclaration(AnalysisResult analysisResult, VHDLDeclaration parent)
		{
			Parent = parent;
			AnalysisResult = analysisResult;
		}
		public VHDLDocument Document { get { return AnalysisResult?.Document; } }
		public AnalysisResult AnalysisResult { get; set; } = null;
		public string Name { get; protected set; } = "";
		public string UndecoratedName
		{
			get
			{
				return Name.Split('@').First();
			}
		}
		public ParserRuleContext Context { get; protected set; } = null;
		public ParserRuleContext NameContext { get; protected set; } = null;
		public string Comment { get; protected set; } = null;


		public virtual IEnumerable<VHDLDeclaration> Children { get; protected set; }

		public Span Span
		{
			get
			{
				return new Span(Context.Start.StartIndex, Context.Stop.StopIndex - Context.Start.StartIndex + 1);
			}
		}

		public string TreePath
		{
			get
			{
				if (Parent == null || Parent is VHDLFileDeclaration)
					return Name;
				return Parent.TreePath + "." + Name;
			}
		}

		public VHDLDeclaration Parent { get; protected set; } = null;

		// Build a QuickInfo content, used for QuickInfo and Completion
		public virtual async Task<object> BuildQuickInfoAsync()
		{
			return null;
		}

		// Build a completion item for VHDLCompletionSource
		public virtual CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			return null;
		}
		// Build a signature for VHDLSignatureHelperSource, mostly used for functions
		public virtual VHDLSignature BuildSignature(ITextBuffer textBuffer, ITrackingSpan applicableToSpan)
		{
			return null;
		}

		public virtual VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			return new VHDLClassifiedText();
		}
		public virtual void Check(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
		}
	}

	abstract class VHDLModifiableDeclaration
		: VHDLDeclaration
	{
		protected VHDLModifiableDeclaration(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			base.Children = Children;
		}

		public new ICollection<VHDLDeclaration> Children { get; } = new List<VHDLDeclaration>();
	}
	// Just so its easier to deal with Libraries
	abstract class VHDLDesignUnit
		: VHDLModifiableDeclaration
	{
		public VHDLDesignUnit(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{ }
		public IEnumerable<VHDLUseClause> UseClauses { get; protected set; }

		public override void Check(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
			foreach (VHDLUseClause clause in UseClauses)
			{
				try
				{
					clause.Check(errorListener);
				}
				catch (Exception) { }
			}
		}
	}

	// function or procedure
	abstract class VHDLFunctionnalDeclaration
		: VHDLModifiableDeclaration
	{
		public VHDLFunctionnalDeclaration(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Parameters = new List<VHDLAbstractVariableDeclaration>();
		}
		public IList<VHDLAbstractVariableDeclaration> Parameters { get; protected set; }
	}

	// signal, variable, constant or port
	abstract class VHDLAbstractVariableDeclaration
		: VHDLModifiableDeclaration
	{
		protected VHDLAbstractVariableDeclaration(AnalysisResult analysisResult, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
		}

		// Only relevant for parameters
		public VHDLSignalMode Mode { get; set; } = VHDLSignalMode.In;
		public VHDLExpression InitializationExpression { get; set; } = null;
		public VHDLType Type { get; set; } = null;

		public bool IsParameter { get { return Parent is VHDLFunctionnalDeclaration fd && fd.Parameters.Contains(this); } }
		private VHDLType GetSimpleType(VHDLType t)
		{
			if (t is VHDLScalarType)
			{
				VHDLScalarType scalarType = t as VHDLScalarType;
				if (scalarType.Type != null)
					return scalarType.Type;
			}
			if (t is VHDLUnconstrainedType)
			{
				return (t as VHDLUnconstrainedType).Type;
			}

			return null;
		}

		// Get the range associated with the given type
		private VHDLRange GetRange(VHDLType t)
		{
			if (t is VHDLScalarType)
			{
				VHDLScalarType scalarType = (VHDLScalarType)t;
				if (scalarType.Range != null)
					return scalarType.Range;
				if (scalarType.Type != null)
					return GetRange(scalarType.Type.Dereference());
			}
			return null;
		}
		private Tuple<VHDLType, VHDLRange> GetIndexInfo(VHDLType baseIndexType, VHDLType t)
		{
			VHDLType elemType = GetSimpleType(t);
			if (elemType == null && baseIndexType != null)
				elemType = GetSimpleType(baseIndexType);
			return Tuple.Create(elemType, GetRange(t.Dereference()));
		}
		public override VHDLSignature BuildSignature(ITextBuffer textBuffer, ITrackingSpan applicableToSpan)
		{
			try
			{
				VHDLType type = Type?.Dereference();
				if (type == null)
					return null;

				if (type is VHDLIndexConstrainedType)
				{
					VHDLIndexConstrainedType indexConstrainedType = (VHDLIndexConstrainedType)type;

					VHDLSignature signature = new VHDLSignature(textBuffer);
					List<Tuple<Span, VHDLType, VHDLRange>> parameterSpans = new List<Tuple<Span, VHDLType, VHDLRange>>();
					if (this is VHDLSignalDeclaration)
					{
						signature.ClassifiedText.Add("signal ", "keyword");
						signature.ClassifiedText.Add(UndecoratedName, "vhdl.signal");
					}
					else if (this is VHDLConstantDeclaration)
					{
						signature.ClassifiedText.Add("constant  ", "keyword");
						signature.ClassifiedText.Add(UndecoratedName, "vhdl.constant");
					}
					else if (this is VHDLVariableDeclaration)
					{
						signature.ClassifiedText.Add("variable  ", "keyword");
						signature.ClassifiedText.Add(UndecoratedName, "vhdl.variable");
					}
					else if (this is VHDLPortDeclaration)
					{
						signature.ClassifiedText.Add("port  ", "keyword");
						signature.ClassifiedText.Add(UndecoratedName, "vhdl.port");
					}
					else if (this is VHDLAliasDeclaration)
					{
						signature.ClassifiedText.Add("alias  ", "keyword");
						signature.ClassifiedText.Add(UndecoratedName, "vhdl.alias");
					}
					signature.ClassifiedText.Add("(");
					int i = 0;
					foreach (VHDLType indexType in indexConstrainedType.IndexTypes)
					{
						if (parameterSpans.Count > 0)
							signature.ClassifiedText.Add(", ");
						VHDLClassifiedText text = new VHDLClassifiedText();
						text.Add("index_" + (++i).ToString() + " : ");
						var indexInfo = GetIndexInfo((indexConstrainedType.ArrayType.Dereference() as VHDLArrayType).IndexTypes?.ElementAtOrDefault(i - 1), indexType);
						text.Add(indexInfo.Item1.GetClassifiedText());
						parameterSpans.Add(Tuple.Create(new Span(signature.ClassifiedText.Text.Length, text.Text.Length), indexInfo.Item1, indexInfo.Item2));
						signature.ClassifiedText.Add(text);
					}
					signature.ClassifiedText.Add(") : ");
					signature.ClassifiedText.Add("array of ", "keyword");
					signature.ClassifiedText.Add((indexConstrainedType.ArrayType.Dereference() as VHDLArrayType).ElementType.GetClassifiedText());

					List<IParameter> parameters = new List<IParameter>();
					i = 0;
					foreach (Tuple<Span, VHDLType, VHDLRange> p in parameterSpans)
					{
						string[] number = new string[] { "1st", "2nd", "3rd" };
						string desc = "Array " + (i > 2 ? (i.ToString() + "th") : number[i]) + " index";
						if (p.Item3 != null)
							desc += string.Format(", index must be between {0} and {1}", p?.Item3?.Start?.GetClassifiedText()?.Text, p?.Item3?.End?.GetClassifiedText()?.Text);

						parameters.Add(new VHDLParameter(desc, p.Item1, "index_" + (++i).ToString(), signature));
					}

					signature.ApplicableToSpan = applicableToSpan;
					signature.Documentation = Comment;
					signature.Parameters = new System.Collections.ObjectModel.ReadOnlyCollection<IParameter>(parameters);
					return signature;
				}
				else if (type is VHDLArrayType)
				{
					VHDLArrayType arrayType = (VHDLArrayType)type;

					VHDLSignature signature = new VHDLSignature(textBuffer);
					List<Tuple<Span, VHDLType, VHDLRange>> parameterSpans = new List<Tuple<Span, VHDLType, VHDLRange>>();
					if (this is VHDLSignalDeclaration)
					{
						signature.ClassifiedText.Add("signal ", "keyword");
						signature.ClassifiedText.Add(UndecoratedName, "vhdl.signal");
					}
					else if (this is VHDLConstantDeclaration)
					{
						signature.ClassifiedText.Add("constant  ", "keyword");
						signature.ClassifiedText.Add(UndecoratedName, "vhdl.constant");
					}
					else if (this is VHDLVariableDeclaration)
					{
						signature.ClassifiedText.Add("variable  ", "keyword");
						signature.ClassifiedText.Add(UndecoratedName, "vhdl.variable");
					}
					else if (this is VHDLPortDeclaration)
					{
						signature.ClassifiedText.Add("port  ", "keyword");
						signature.ClassifiedText.Add(UndecoratedName, "vhdl.port");
					}
					else if (this is VHDLAliasDeclaration)
					{
						signature.ClassifiedText.Add("alias  ", "keyword");
						signature.ClassifiedText.Add(UndecoratedName, "vhdl.alias");
					}
					signature.ClassifiedText.Add("(");
					int i = 0;
					foreach (VHDLType indexType in arrayType.IndexTypes)
					{
						if (parameterSpans.Count > 0)
							signature.ClassifiedText.Add(", ");
						VHDLClassifiedText text = new VHDLClassifiedText();
						text.Add("index_" + (++i).ToString() + " : ");
						var indexInfo = GetIndexInfo(null, indexType);
						text.Add(indexInfo.Item1.GetClassifiedText());
						parameterSpans.Add(Tuple.Create(new Span(signature.ClassifiedText.Text.Length, text.Text.Length), indexInfo.Item1, indexInfo.Item2));
						signature.ClassifiedText.Add(text);
					}
					signature.ClassifiedText.Add(") : ");
					signature.ClassifiedText.Add("array of ", "keyword");
					signature.ClassifiedText.Add(arrayType.ElementType.GetClassifiedText());

					List<IParameter> parameters = new List<IParameter>();
					i = 0;
					foreach (Tuple<Span, VHDLType, VHDLRange> p in parameterSpans)
					{
						string[] number = new string[] { "1st", "2nd", "3rd" };
						string desc = "Array " + (i > 2 ? (i.ToString() + "th") : number[i]) + " index";
						if (p.Item3 != null)
							desc += string.Format(", index must be between {0} and {1}", p?.Item3?.Start?.GetClassifiedText()?.Text, p?.Item3?.End?.GetClassifiedText()?.Text);

						parameters.Add(new VHDLParameter(desc, p.Item1, "index_" + (++i).ToString(), signature));
					}

					signature.ApplicableToSpan = applicableToSpan;
					signature.Documentation = Comment;
					signature.Parameters = new System.Collections.ObjectModel.ReadOnlyCollection<IParameter>(parameters);
					return signature;
				}
			}
			catch (Exception e)
			{
			}
			return null;
		}

		public override void Check(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
			if (Type != null && InitializationExpression != null)
				VHDLStatementUtilities.CheckExpressionType(InitializationExpression, Type, x => errorListener(x));
		}
	}

	class VHDLFileDeclaration
		: VHDLModifiableDeclaration
	{
		public VHDLFileDeclaration(AnalysisResult analysisResult)
			: base(analysisResult, null)
		{
		}
	}
	class VHDLPackageDeclaration
		: VHDLDesignUnit
	{
		public VHDLPackageDeclaration(AnalysisResult analysisResult, ParserRuleContext context, ParserRuleContext nameContext, string name, IEnumerable<VHDLUseClause> useClauses, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
			UseClauses = useClauses;
		}

		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("package ", "keyword");
			declText.Add(GetClassifiedName(true));

			return declText;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphPackage());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}

		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.PackageImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(text.Text + "." + UndecoratedName);
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName);
		}
	}
	class VHDLPackageBodyDeclaration
		: VHDLDesignUnit
	{
		public VHDLPackageBodyDeclaration(AnalysisResult analysisResult, ParserRuleContext context, ParserRuleContext nameContext, string name, IEnumerable<VHDLUseClause> useClauses, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
			UseClauses = useClauses;
		}


		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("package ");
			declText.Add(GetClassifiedName(true));

			return declText;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphPackage());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.PackageImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add("." + UndecoratedName);
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName);
		}
	}
	class VHDLEntityDeclaration
		: VHDLDesignUnit
	{
		public IEnumerable<VHDLPortDeclaration> Ports => Children.OfType<VHDLPortDeclaration>();
		public List<VHDLStatement> Statements { get; set; } = new List<VHDLStatement>();
		public VHDLEntityDeclaration(AnalysisResult analysisResult, ParserRuleContext context, ParserRuleContext nameContext, string name, IEnumerable<VHDLUseClause> useClauses, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
			UseClauses = useClauses;
		}

		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("entity ", "keyword");
			declText.Add(GetClassifiedName(true));

			return declText;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphClass());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.ClassImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName, "vhdl.entity");
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName, "vhdl.entity");
		}
	}
	class VHDLArchitectureDeclaration
		: VHDLDesignUnit,
		IVHDLToResolve
	{
		public List<VHDLStatement> Statements { get; set; } = new List<VHDLStatement>();
		public VHDLArchitectureDeclaration(AnalysisResult analysisResult, ParserRuleContext context, ParserRuleContext nameContext, string name, IEnumerable<VHDLUseClause> useClauses, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
			UseClauses = useClauses;
		}
		public VHDLDeclaration EntityDeclaration { get; set; } = null;

		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("architecture ", "keyword");
			declText.Add(GetClassifiedName(true));
			declText.Add(" of ", "keyword");
			declText.Add(EntityDeclaration.GetClassifiedName());

			return declText;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphClass());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.ClassImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName, "vhdl.architecture");
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName, "vhdl.architecture");
		}

		public void Resolve(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
			vhdlParser.Architecture_bodyContext archContext = Context as vhdlParser.Architecture_bodyContext;
			try
			{
				EntityDeclaration = VHDLDeclarationUtilities.FindName(Parent, archContext.identifier()[1].GetText());
			}
			catch (Exception e)
			{
			}
			if (EntityDeclaration == null)
			{
				errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError,
					string.Format("The name '{0}' does not exist in the current context", archContext.identifier()[1].GetText()),
					archContext.identifier()[1].GetSpan()));
			}
		}
	}
	class VHDLEnumerationValueDeclaration
		: VHDLConstantDeclaration
	{
		public VHDLEnumerationValueDeclaration(AnalysisResult analysisResult, ParserRuleContext context, string name, VHDLTypeDeclaration enumerationDeclaration)
			: base(analysisResult, context, context, name, enumerationDeclaration.Parent)
		{
			EnumerationDeclaration = enumerationDeclaration;
			Type = new VHDLReferenceType(enumerationDeclaration);
		}
		public VHDLTypeDeclaration EnumerationDeclaration { get; set; } = null;
	}
	class VHDLSignalDeclaration
		: VHDLAbstractVariableDeclaration
	{
		public VHDLSignalDeclaration(AnalysisResult analysisResult, ParserRuleContext context, ParserRuleContext nameContext, string name, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
		}

		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("signal ", "keyword");
			declText.Add(GetClassifiedName(true));
			declText.Add(" : ");
			try
			{
				declText.Add(Type.GetClassifiedText());
			}
			catch (Exception)
			{
				declText.Add("<error type>");
			}

			return declText;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphVariable());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.VariableImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName, "vhdl.signal");
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName, "vhdl.signal");
		}
		public override void Check(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
			base.Check(deepAnalysisResult, errorListener);

			if (!IsParameter && Type?.Dereference() is VHDLAbstractArrayType aat && !aat.IsConstrained)
				errorListener(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Signal type cannot be unconstrained", Span));

			if (IsParameter && (Mode == VHDLSignalMode.Out || Mode == VHDLSignalMode.Inout) && (Parent is VHDLFunctionDeclaration || Parent is VHDLFunctionBodyDeclaration))
				errorListener(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Out signals are not allowed as function parameters", Span));
		}
	}

	enum VHDLSignalMode
	{
		In,
		Out,
		Inout,
		Buffer,
		Linkage
	}
	class VHDLPortDeclaration
	   : VHDLAbstractVariableDeclaration
	{
		public VHDLPortDeclaration(AnalysisResult analysisResult, ParserRuleContext context, ParserRuleContext nameContext, string name, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
		}

		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("port ", "keyword");
			declText.Add(GetClassifiedName(true));
			declText.Add(" : ");
			switch (Mode)
			{
				case VHDLSignalMode.Buffer:
					declText.Add("buffer ", "keyword");
					break;
				case VHDLSignalMode.In:
					declText.Add("in ", "keyword");
					break;
				case VHDLSignalMode.Inout:
					declText.Add("inout ", "keyword");
					break;
				case VHDLSignalMode.Linkage:
					declText.Add("linkage ", "keyword");
					break;
				case VHDLSignalMode.Out:
					declText.Add("out ", "keyword");
					break;
			}
			try
			{
				declText.Add(Type.GetClassifiedText());
			}
			catch (Exception)
			{
				declText.Add("<error type>");
			}

			return declText;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphVariable());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			//string description = "port " + UndecoratedName + (string.IsNullOrEmpty(Comment) ? "" : "\n" + Comment);
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.VariableImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName, "vhdl.port");
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName, "vhdl.port");
		}
	}

	class VHDLVariableDeclaration
		: VHDLAbstractVariableDeclaration
	{
		public VHDLVariableDeclaration(AnalysisResult analysisResult, ParserRuleContext context, ParserRuleContext nameContext, string name, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
		}

		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("variable ", "keyword");
			declText.Add(GetClassifiedName(true));
			declText.Add(" : ");
			try
			{
				declText.Add(Type.GetClassifiedText());
			}
			catch (Exception)
			{
				declText.Add("<error type>");
			}

			return declText;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphVariable());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			//string description = "variable " + UndecoratedName + (string.IsNullOrEmpty(Comment) ? "" : "\n" + Comment);
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.VariableImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName, "vhdl.variable");
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName, "vhdl.variable");
		}
		public override void Check(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
			base.Check(deepAnalysisResult, errorListener);

			if (Type?.Dereference() is VHDLAbstractArrayType aat2 && !aat2.IsConstrained && !(Parent is VHDLProcedureDeclaration) && !(Parent is VHDLProcedureBodyDeclaration))
				errorListener(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Variable type cannot be unconstrained", Span));

			if (IsParameter && (Parent is VHDLFunctionDeclaration || Parent is VHDLFunctionBodyDeclaration))
				errorListener(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Variables are not allowed as function parameters", Span));
		}
	}

	class VHDLConstantDeclaration
		: VHDLAbstractVariableDeclaration
	{
		public VHDLConstantDeclaration(AnalysisResult analysisResult, ParserRuleContext context, ParserRuleContext nameContext, string name, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
		}
		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("constant ", "keyword");
			declText.Add(GetClassifiedName(true));
			declText.Add(" : ");
			try
			{
				declText.Add(Type.GetClassifiedText());
			}
			catch (Exception)
			{
				declText.Add("<error type>");
			}

			return declText;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphConstant());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}

		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			//string description = "constant " + UndecoratedName + (string.IsNullOrEmpty(Comment) ? "" : "\n" + Comment);
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.ConstantImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName, "vhdl.constant");
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName, "vhdl.constant");
		}
	}
	// Types are not compatible with each other, but they are compatible with their own subtypes
	class VHDLTypeDeclaration
		: VHDLModifiableDeclaration
	{
		public VHDLType Type { get; set; } = null;
		public VHDLTypeDeclaration(AnalysisResult analysisResult, ParserRuleContext context, ParserRuleContext nameContext, string name, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
		}

		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("type ", "keyword");
			declText.Add(GetClassifiedName(true));
			declText.Add(" is ", "keyword");
			declText.Add(Type.GetClassifiedText());

			return declText;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphClass());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			//string description = "type " + UndecoratedName + (string.IsNullOrEmpty(Comment) ? "" : "\n" + Comment);
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.ClassImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName, "vhdl.type");
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName, "vhdl.type");
		}
	}

	// Subtypes are compatible if they are from the same type
	class VHDLSubTypeDeclaration
		: VHDLModifiableDeclaration
	{
		public VHDLType Type { get; set; } = null;
		public VHDLSubTypeDeclaration(AnalysisResult analysisResult, ParserRuleContext context, ParserRuleContext nameContext, string name, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
		}

		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("subtype ", "keyword");
			declText.Add(GetClassifiedName(true));
			declText.Add(" is ", "keyword");
			declText.Add(Type.GetClassifiedText());

			return declText;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphClass());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			//string description = "type " + UndecoratedName + (string.IsNullOrEmpty(Comment) ? "" : "\n" + Comment);
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.ClassImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName, "vhdl.type");
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName, "vhdl.type");
		}
	}
	class VHDLProcessDeclaration
		: VHDLModifiableDeclaration
	{
		public List<VHDLExpression> SensitivityList { get; set; } = new List<VHDLExpression>();
		public List<VHDLStatement> Statements { get; set; } = new List<VHDLStatement>();
		public VHDLProcessDeclaration(AnalysisResult analysisResult, ParserRuleContext context, ParserRuleContext nameContext, string name, VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
		}

		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add(GetClassifiedName(true));
			declText.Add(" : ");
			declText.Add("process ", "keyword");
			declText.Add("(");

			int i = 0;
			foreach (VHDLExpression e in SensitivityList)
			{
				if (i > 0)
					declText.Add(", ");

				VHDLClassifiedText paramText = e.GetClassifiedText();
				declText.Add(paramText);
				++i;
			}

			declText.Add(")");

			return declText;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphClass());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.ClassImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add("." + UndecoratedName);
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName);
		}


		bool ExpressionContainsReference(VHDLExpression e, VHDLDeclaration d)
		{
			try
			{
				if (e is VHDLReferenceExpression r && r.Declaration == d)
					return true;

				return e.Children.Any(x => ExpressionContainsReference(x, d));
			}
			catch (Exception ex)
			{
				return false;
			}
		}
		IEnumerable<VHDLReferenceExpression> CollectAllReferences(VHDLExpression e)
		{
			if (e is VHDLReferenceExpression r)
				yield return r;

			foreach (var y in e.Children.SelectMany(x => CollectAllReferences(x)))
				yield return y;
		}
		IEnumerable<VHDLExpression> CollectAllExpressions(VHDLStatement s)
		{
			var children = s.Children;
			return children.OfType<VHDLExpression>().Concat(children.OfType<VHDLStatement>().SelectMany(x => CollectAllExpressions(x)));
		}
		IEnumerable<VHDLStatement> CollectAllStatements(VHDLStatement s)
		{
			var children = s.Children;
			return children.OfType<VHDLStatement>().Concat(children.OfType<VHDLStatement>().SelectMany(x => CollectAllStatements(x)));
		}
		public override void Check(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
			HashSet<VHDLReferenceExpression> sensitivity = new HashSet<VHDLReferenceExpression>();
			foreach (VHDLReferenceExpression expression in SensitivityList.OfType<VHDLReferenceExpression>())
			{
				if (expression.Declaration == null)
					continue;

				if (expression.Declaration is VHDLSignalDeclaration || expression.Declaration is VHDLPortDeclaration)
					sensitivity.Add(expression);
				else
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, "Sensitivity items must be signals", expression.Span));
			}

			var allExpressions = Statements.SelectMany(x => CollectAllExpressions(x)).ToArray();
			foreach (var r in sensitivity)
				if (allExpressions.All(x => !ExpressionContainsReference(x, r.Declaration)))
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, "Signal in sensitivity list is not used in the process", r.Span));

			var allStatements = Statements.SelectMany(x => CollectAllStatements(x)).Concat(Statements).ToArray();
			foreach (VHDLStatement s in allStatements)
			{
				List<VHDLExpression> expressionList = new List<VHDLExpression>();
				if (s is VHDLSignalAssignmentStatement sas)
				{
					expressionList.AddRange(sas.Values.Select(x => x.ConditionExpression).Concat(sas.Values.Select(x => x.ValueExpression)));
				}
				else if (s is VHDLVariableAssignmentStatement vas)
				{
					expressionList.AddRange(vas.Values.Select(x => x.ConditionExpression).Concat(vas.Values.Select(x => x.ValueExpression)));
				}
				else if (s is VHDLIfStatement ifs)
				{
					expressionList.AddRange(ifs.ElseIfStatements.Select(x => x.Item1).Prepend(ifs.Condition));
				}
				else if (s is VHDLWhileStatement ws)
				{
					expressionList.Add(ws.Condition);
				}
				else if (s is VHDLForStatement fs)
				{
					expressionList.Add(fs.Range?.Range?.Start);
					expressionList.Add(fs.Range?.Range?.End);
				}
				else if (s is VHDLCaseStatement cs)
				{
					expressionList.AddRange(cs.Alternatives.SelectMany(x => x.Conditions).Prepend(cs.Expression));
				}
				var allReferences = expressionList.Where(x => x != null).SelectMany(x => CollectAllReferences(x));
				foreach (var r in allReferences)
				{
					if (r?.Declaration is VHDLSignalDeclaration && sensitivity.All(x => x.Declaration != r.Declaration))
					{
						errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, String.Format("Signal '{0}' in is not in the sensitivity list", r.Declaration.Name), r.Span));
					}
				}
			}
		}
	}

	class VHDLFunctionDeclaration
		: VHDLFunctionnalDeclaration
	{
		public VHDLType ReturnType { get; set; } = null;
		public VHDLFunctionDeclaration(AnalysisResult analysisResult,
			ParserRuleContext context,
			ParserRuleContext nameContext,
			string name,
			VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.MethodImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			List<Span> parameterSpans = null;
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration(out parameterSpans).ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphMethod());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		private VHDLClassifiedText GetClassifiedDeclaration(out List<Span> parameterSpans)
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("function ", "keyword");
			declText.Add(GetClassifiedName(true));
			declText.Add("(");

			parameterSpans = new List<Span>();
			foreach (VHDLDeclaration p in Parameters)
			{
				if (parameterSpans.Count > 0)
					declText.Add(", ");

				VHDLClassifiedText paramText = p.GetClassifiedName(false);
				paramText.Add(" : ");
				if (p is VHDLVariableDeclaration)
					paramText.Add((p as VHDLVariableDeclaration).Type.GetClassifiedText());
				else if (p is VHDLConstantDeclaration)
					paramText.Add((p as VHDLConstantDeclaration).Type.GetClassifiedText());
				else if (p is VHDLSignalDeclaration)
					paramText.Add((p as VHDLSignalDeclaration).Type.GetClassifiedText());
				else if (p is VHDLPortDeclaration)
					paramText.Add((p as VHDLPortDeclaration).Type.GetClassifiedText());

				parameterSpans.Add(new Span(declText.Text.Length, paramText.Text.Length));
				declText.Add(paramText);
			}

			declText.Add(") ");
			declText.Add("return ", "keyword");
			declText.Add(ReturnType.GetClassifiedText());

			return declText;
		}
		public override VHDLSignature BuildSignature(ITextBuffer textBuffer, ITrackingSpan applicableToSpan)
		{
			vhdlParser.Function_specificationContext functionSpecificationContext = Context as vhdlParser.Function_specificationContext;

			VHDLSignature signature = new VHDLSignature(textBuffer);

			List<Span> parameterSpans = null;
			signature.ClassifiedText.Add(GetClassifiedDeclaration(out parameterSpans));

			List<IParameter> parameters = new List<IParameter>();
			foreach (Tuple<VHDLDeclaration, Span> p in Parameters.Zip(parameterSpans, (x, y) => new Tuple<VHDLDeclaration, Span>(x, y)))
				parameters.Add(new VHDLParameter(p.Item1.Comment, p.Item2, p.Item1.UndecoratedName, signature));

			signature.ApplicableToSpan = applicableToSpan;
			signature.Documentation = Comment;
			signature.Parameters = new System.Collections.ObjectModel.ReadOnlyCollection<IParameter>(parameters);
			return signature;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName, "vhdl.function");
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName, "vhdl.function");
		}

		// This is required because the result type can be dependant on constants passed as parameters
		/*public VHDLType GetResultType(VHDLEvaluationContext evaluationContext)
		{
			ReturnType.
		}*/
	}

	class EvaluationContext
	{
		private Stack<Dictionary<VHDLDeclaration, VHDLEvaluatedExpression>> m_scopes = new Stack<Dictionary<VHDLDeclaration, VHDLEvaluatedExpression>>();
		const int MAX_LEVEL = 10;
		public EvaluationContext()
		{

		}
		public bool Contains(VHDLDeclaration declaration)
		{
			if (m_scopes.Count == 0)
				return false;
			return m_scopes.Peek().ContainsKey(declaration);
		}

		public VHDLEvaluatedExpression this[VHDLDeclaration d]
		{
			get => m_scopes.Peek()[d];
			set => m_scopes.Peek()[d] = value;
		}

		public void Push()
		{
			if (m_scopes.Count >= MAX_LEVEL)
				throw new Exception();
			m_scopes.Push(new Dictionary<VHDLDeclaration, VHDLEvaluatedExpression>());
		}

		public void Pop()
		{
			m_scopes.Pop();
		}

	}
	class VHDLFunctionBodyDeclaration
		: VHDLFunctionnalDeclaration
	{
		public List<VHDLStatement> Statements { get; set; } = new List<VHDLStatement>();
		public VHDLType ReturnType { get; set; } = null;
		public VHDLFunctionBodyDeclaration(AnalysisResult analysisResult,
			vhdlParser.Subprogram_bodyContext context,
			ParserRuleContext nameContext,
			string name,
			VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.MethodImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			List<Span> parameterSpans = null;
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration(out parameterSpans).ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphMethod());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		private VHDLClassifiedText GetClassifiedDeclaration(out List<Span> parameterSpans)
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("function ", "keyword");
			declText.Add(GetClassifiedName(true));
			declText.Add("(");

			parameterSpans = new List<Span>();
			foreach (VHDLDeclaration p in Parameters)
			{
				if (parameterSpans.Count > 0)
					declText.Add(", ");

				VHDLClassifiedText paramText = p.GetClassifiedName(false);
				paramText.Add(" : ");
				if (p is VHDLVariableDeclaration)
					paramText.Add((p as VHDLVariableDeclaration).Type.GetClassifiedText());
				else if (p is VHDLConstantDeclaration)
					paramText.Add((p as VHDLConstantDeclaration).Type.GetClassifiedText());
				else if (p is VHDLSignalDeclaration)
					paramText.Add((p as VHDLSignalDeclaration).Type.GetClassifiedText());
				else if (p is VHDLPortDeclaration)
					paramText.Add((p as VHDLPortDeclaration).Type.GetClassifiedText());

				parameterSpans.Add(new Span(declText.Text.Length, paramText.Text.Length));
				declText.Add(paramText);
			}

			declText.Add(") ");
			declText.Add("return ", "keyword");
			declText.Add(ReturnType.GetClassifiedText());

			return declText;
		}
		public override VHDLSignature BuildSignature(ITextBuffer textBuffer, ITrackingSpan applicableToSpan)
		{
			vhdlParser.Function_specificationContext functionSpecificationContext = Context as vhdlParser.Function_specificationContext;

			VHDLSignature signature = new VHDLSignature(textBuffer);

			List<Span> parameterSpans = null;
			signature.ClassifiedText.Add(GetClassifiedDeclaration(out parameterSpans));

			List<IParameter> parameters = new List<IParameter>();
			foreach (Tuple<VHDLDeclaration, Span> p in Parameters.Zip(parameterSpans, (x, y) => new Tuple<VHDLDeclaration, Span>(x, y)))
				parameters.Add(new VHDLParameter(p.Item1.Comment, p.Item2, p.Item1.UndecoratedName, signature));

			signature.ApplicableToSpan = applicableToSpan;
			signature.Documentation = Comment;
			signature.Parameters = new System.Collections.ObjectModel.ReadOnlyCollection<IParameter>(parameters);
			return signature;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName, "vhdl.function");
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName, "vhdl.function");
		}

		bool ExecuteStatement(VHDLStatement statement, EvaluationContext evaluationContext, out VHDLEvaluatedExpression returnedValue, ref ulong performanceCounter)
		{
			returnedValue = null;
			if (performanceCounter == 0)
				throw new Exception("Performance counter reached 0");

			--performanceCounter;

			if (statement is VHDLReturnStatement rs)
			{
				returnedValue = rs.Expression.Evaluate(evaluationContext);
				return true;
			}
			else if (statement is VHDLSignalAssignmentStatement sas)
			{
				if (sas.NameExpression is VHDLNameExpression ne)
				{
					foreach (var v in sas.Values)
					{
						bool condition = true;
						if (v.ConditionExpression != null)
						{
							VHDLEvaluatedExpression ee = v.ConditionExpression.Evaluate(evaluationContext);
							if (ee?.Result is VHDLBooleanLiteral l)
							{
								condition = l.Value;
							}
							else
								throw new Exception("Unable to evaluate assignment condition");
						}
						if (condition)
						{
							evaluationContext[ne.Declaration] = v.ValueExpression.Evaluate(evaluationContext);
							break;
						}
					}
				}
				else
					throw new Exception("Unable to evaluate assignment name");

			}
			else if (statement is VHDLVariableAssignmentStatement vas)
			{
				if (vas.NameExpression is VHDLNameExpression ne)
				{
					foreach (var v in vas.Values)
					{
						bool condition = true;
						if (v.ConditionExpression != null)
						{
							VHDLEvaluatedExpression ee = v.ConditionExpression.Evaluate(evaluationContext);
							if (ee?.Result is VHDLBooleanLiteral l)
							{
								condition = l.Value;
							}
							else
								throw new Exception("Unable to evaluate assignment condition");
						}
						if (condition)
						{
							evaluationContext[ne.Declaration] = v.ValueExpression.Evaluate(evaluationContext);
							break;
						}
					}
				}
				else
					throw new Exception("Unable to evaluate assignment name");
			}
			else if (statement is VHDLIfStatement ifs)
			{
				VHDLEvaluatedExpression ee = ifs.Condition.Evaluate(evaluationContext);
				if (ee?.Result is VHDLBooleanLiteral l)
				{
					if (l.Value == true)
					{
						foreach (VHDLStatement s in ifs.Statements)
						{
							if (ExecuteStatement(s, evaluationContext, out returnedValue, ref performanceCounter))
								return true;
						}
					}
				}
				else
					throw new Exception("Unable to evaluate if condition");
			}
			else if (statement is VHDLWhileStatement ws)
			{
				while (true)
				{
					// make sure its not an empty loop
					--performanceCounter;
					if (performanceCounter == 0)
						throw new Exception("Performance counter reached 0");

					VHDLEvaluatedExpression ee = ws.Condition.Evaluate(evaluationContext);
					if (ee?.Result is VHDLBooleanLiteral l)
					{
						if (l.Value == true)
						{
							foreach (VHDLStatement s in ws.Statements)
							{
								if (ExecuteStatement(s, evaluationContext, out returnedValue, ref performanceCounter))
									return true;
							}
						}
						else
							break;
					}
					else
						throw new Exception("Unable to evaluate if condition");
				}
			}
			else if (statement is VHDLForStatement fs)
			{

				//while (true)
				//{
				//	VHDLEvaluatedExpression eeStart = fs.Range?.Range?.Start?.Evaluate(evaluationContext);
				//	VHDLEvaluatedExpression eeEnd = fs.Range?.Range?.End?.Evaluate(evaluationContext);
				//	if (ee?.Result is VHDLBooleanLiteral l)
				//	{
				//		if (l.Value == true)
				//		{
				//			foreach (VHDLStatement s in ws.Statements)
				//			{
				//				if (ExecuteStatement(s, evaluationContext, out returnedValue, ref performanceCounter))
				//					return true;
				//			}
				//		}
				//		else
				//			break;
				//	}
				//	else
				//		throw new Exception("Unable to evaluate if condition");
				//}
				throw new Exception("Unable to evaluate for statement");
			}
			else if (statement is VHDLCaseStatement cs)
			{
				//VHDLEvaluatedExpression ee = cs.Expression.Evaluate(evaluationContext);
				//if (ee == null)
				//	throw new Exception("Unable to evaluate case expression");
				//foreach(var a in cs.Alternatives)
				//{
				//	VHDLIsEqualExpression ieq = new VHDLIsEqualExpression(cs.AnalysisResult, new Span(), cs.Expression, a.Conditions);
				//	a.Conditions
				//}
				throw new Exception("Unable to evaluate case statement");
			}
			else
			{
				throw new Exception("Unable to evaluate statement");
			}
			return false;
		}
		public VHDLEvaluatedExpression EvaluateCall(IEnumerable<VHDLEvaluatedExpression> args, EvaluationContext evaluationContext)
		{
			try
			{
				evaluationContext.Push();

				ulong performanceCounter = 100;
				foreach (var p in Parameters.Zip(args, (x, y) => Tuple.Create(x, y)))
				{
					if (p.Item1.Type.IsCompatible(p.Item2.Type) == VHDLCompatibilityResult.No)
						return null;
					evaluationContext[p.Item1] = p.Item2;
				}
				foreach (var d in Children.OfType<VHDLAbstractVariableDeclaration>())
				{
					if (!d.IsParameter)
						evaluationContext[d] = d.InitializationExpression?.Evaluate(evaluationContext);
				}
				foreach (VHDLStatement s in Statements)
				{
					VHDLEvaluatedExpression result = null;
					if (ExecuteStatement(s, evaluationContext, out result, ref performanceCounter))
						return result;
				}

			}
			catch (Exception ex)
			{

			}

			evaluationContext.Pop();
			return null;
		}
	}
	class VHDLProcedureDeclaration
		: VHDLFunctionnalDeclaration
	{
		public VHDLProcedureDeclaration(AnalysisResult analysisResult,
			ParserRuleContext context,
			ParserRuleContext nameContext,
			string name,
			VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.MethodImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			List<Span> parameterSpans = null;
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration(out parameterSpans).ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphMethod());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		private VHDLClassifiedText GetClassifiedDeclaration(out List<Span> parameterSpans)
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("procedure ", "keyword");
			declText.Add(GetClassifiedName(true));
			declText.Add("(");

			parameterSpans = new List<Span>();
			foreach (VHDLDeclaration p in Parameters)
			{
				if (parameterSpans.Count > 0)
					declText.Add(", ");

				VHDLClassifiedText paramText = p.GetClassifiedName(false);
				paramText.Add(" : ");
				if (p is VHDLVariableDeclaration)
					paramText.Add((p as VHDLVariableDeclaration).Type.GetClassifiedText());
				else if (p is VHDLConstantDeclaration)
					paramText.Add((p as VHDLConstantDeclaration).Type.GetClassifiedText());
				else if (p is VHDLSignalDeclaration)
					paramText.Add((p as VHDLSignalDeclaration).Type.GetClassifiedText());
				else if (p is VHDLPortDeclaration)
					paramText.Add((p as VHDLPortDeclaration).Type.GetClassifiedText());

				parameterSpans.Add(new Span(declText.Text.Length, paramText.Text.Length));
				declText.Add(paramText);
			}

			declText.Add(") ");

			return declText;
		}
		public override VHDLSignature BuildSignature(ITextBuffer textBuffer, ITrackingSpan applicableToSpan)
		{
			vhdlParser.Function_specificationContext functionSpecificationContext = Context as vhdlParser.Function_specificationContext;

			VHDLSignature signature = new VHDLSignature(textBuffer);

			List<Span> parameterSpans = null;
			signature.ClassifiedText.Add(GetClassifiedDeclaration(out parameterSpans));

			List<IParameter> parameters = new List<IParameter>();
			foreach (Tuple<VHDLDeclaration, Span> p in Parameters.Zip(parameterSpans, (x, y) => new Tuple<VHDLDeclaration, Span>(x, y)))
				parameters.Add(new VHDLParameter(p.Item1.Comment, p.Item2, p.Item1.UndecoratedName, signature));

			signature.ApplicableToSpan = applicableToSpan;
			signature.Documentation = Comment;
			signature.Parameters = new System.Collections.ObjectModel.ReadOnlyCollection<IParameter>(parameters);
			return signature;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName, "vhdl.procedure");
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName, "vhdl.procedure");
		}
	}
	class VHDLProcedureBodyDeclaration
		: VHDLFunctionnalDeclaration
	{
		public List<VHDLStatement> Statements { get; set; } = new List<VHDLStatement>();
		public VHDLProcedureBodyDeclaration(AnalysisResult analysisResult,
			ParserRuleContext context,
			ParserRuleContext nameContext,
			string name,
			VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.MethodImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			List<Span> parameterSpans = null;
			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration(out parameterSpans).ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphMethod());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		private VHDLClassifiedText GetClassifiedDeclaration(out List<Span> parameterSpans)
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("procedure ", "keyword");
			declText.Add(GetClassifiedName(true));
			declText.Add("(");

			parameterSpans = new List<Span>();
			foreach (VHDLDeclaration p in Parameters)
			{
				if (parameterSpans.Count > 0)
					declText.Add(", ");

				VHDLClassifiedText paramText = p.GetClassifiedName(false);
				paramText.Add(" : ");
				if (p is VHDLVariableDeclaration)
					paramText.Add((p as VHDLVariableDeclaration).Type.GetClassifiedText());
				else if (p is VHDLConstantDeclaration)
					paramText.Add((p as VHDLConstantDeclaration).Type.GetClassifiedText());
				else if (p is VHDLSignalDeclaration)
					paramText.Add((p as VHDLSignalDeclaration).Type.GetClassifiedText());
				else if (p is VHDLPortDeclaration)
					paramText.Add((p as VHDLPortDeclaration).Type.GetClassifiedText());

				parameterSpans.Add(new Span(declText.Text.Length, paramText.Text.Length));
				declText.Add(paramText);
			}

			declText.Add(") ");

			return declText;
		}
		public override VHDLSignature BuildSignature(ITextBuffer textBuffer, ITrackingSpan applicableToSpan)
		{
			vhdlParser.Function_specificationContext functionSpecificationContext = Context as vhdlParser.Function_specificationContext;

			VHDLSignature signature = new VHDLSignature(textBuffer);

			List<Span> parameterSpans = null;
			signature.ClassifiedText.Add(GetClassifiedDeclaration(out parameterSpans));

			List<IParameter> parameters = new List<IParameter>();
			foreach (Tuple<VHDLDeclaration, Span> p in Parameters.Zip(parameterSpans, (x, y) => new Tuple<VHDLDeclaration, Span>(x, y)))
				parameters.Add(new VHDLParameter(p.Item1.Comment, p.Item2, p.Item1.UndecoratedName, signature));

			signature.ApplicableToSpan = applicableToSpan;
			signature.Documentation = Comment;
			signature.Parameters = new System.Collections.ObjectModel.ReadOnlyCollection<IParameter>(parameters);
			return signature;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName, "vhdl.procedure");
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName, "vhdl.procedure");
		}
	}
	class VHDLAliasDeclaration
	   : VHDLAbstractVariableDeclaration
	{
		public VHDLAliasDeclaration(AnalysisResult analysisResult,
			ParserRuleContext context,
			ParserRuleContext nameContext,
			string name,
			VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
		}

		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.VariableImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphVariable());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("alias ", "keyword");
			declText.Add(GetClassifiedName(true));

			if (Type != null)
			{
				declText.Add(" : ");
				declText.Add(Type.GetClassifiedText());
			}
			declText.Add(" is ", "keyword");
			declText.Add(InitializationExpression.GetClassifiedText());

			return declText;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName, "vhdl.alias");
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName, "vhdl.alias");
		}
	}
	class VHDLAttributeDeclaration
	   : VHDLModifiableDeclaration
	{
		public VHDLAttributeDeclaration(AnalysisResult analysisResult,
			ParserRuleContext context,
			ParserRuleContext nameContext,
			string name,
			VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			NameContext = nameContext;
			Name = name;
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.VariableImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphVariable());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("attribute ", "keyword");
			declText.Add(GetClassifiedName(true));
			return declText;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName);
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName);
		}
	}

	// Fake declaration, just to have a scope for "for loops" "i" variables
	class VHDLLoopDeclaration
	   : VHDLModifiableDeclaration
	{
		public VHDLLoopDeclaration(AnalysisResult analysisResult,
			ParserRuleContext context,
			VHDLDeclaration parent)
			: base(analysisResult, parent)
		{
			Context = context;
			Name = "@loop_" + Context.Start.StartIndex.ToString();
		}

		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			return Parent.GetClassifiedName(fullyQualified);
		}
	}

	class VHDLComponentDeclaration
	   : VHDLModifiableDeclaration
	{
		public VHDLComponentDeclaration(AnalysisResult analysisResult, vhdlParser.Component_declarationContext context, VHDLDeclaration parent, string name)
			: base(analysisResult, parent)
		{
			Context = context;
			Name = name;
			NameContext = context.identifier()?.FirstOrDefault();
		}

		public List<VHDLDeclaration> Generics = new List<VHDLDeclaration>();
		public IEnumerable<VHDLPortDeclaration> Ports => Children.OfType<VHDLPortDeclaration>();
		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add("component ", "keyword");
			declText.Add(GetClassifiedName(true));
			declText.Add(" is port", "keyword");

			declText.Add("(");
			int i = 0;
			foreach (VHDLPortDeclaration d in Ports)
			{
				if (i++ > 0)
					declText.Add(", ");
				declText.Add(d.GetClassifiedName());
				declText.Add(" : ");
				declText.Add(d.Type.GetClassifiedText());
			}
			declText.Add(")");

			return declText;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphVariable());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName);
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName);
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.ClassImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
		public override void Check(DeepAnalysisResult deepAnalysisResult, Action<VHDLError> errorListener)
		{
			VHDLDeclaration enclosingDeclaration = VHDLDeclarationUtilities.GetEnclosingDeclaration(deepAnalysisResult.AnalysisResult, Span.Start);
			VHDLDeclaration decl = VHDLDeclarationUtilities.FindAllNames(enclosingDeclaration, Name).FirstOrDefault(x => x != this);
			if (decl == null)
			{
				errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.Warning, string.Format("External component '{0}' not found", Name), NameContext.GetSpan()));
				return;
			}

			if (!(decl is VHDLEntityDeclaration))
			{
				errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("External component '{0}' should be an entity", Name), NameContext.GetSpan()));
				return;
			}

			VHDLEntityDeclaration entityDecl = (VHDLEntityDeclaration)decl;

			foreach (var p1 in Ports)
			{
				var p2 = entityDecl.Ports.FirstOrDefault(x => string.Compare(p1.Name, x.Name, true) == 0);
				if (p2 == null)
				{
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Port '{0}' not found in entity '{1}'", p1.Name, entityDecl.Name), p1.Span));
					continue;
				}

				VHDLType t1 = p1.Type;
				VHDLType t2 = p2.Type;
				if (t2.IsCompatible(t1) == VHDLCompatibilityResult.No)
					errorListener?.Invoke(new VHDLError(0,
								PredefinedErrorTypeNames.SyntaxError,
								string.Format("Cannot implicitly convert type '{0}' to '{1}'",
									t1?.GetClassifiedText()?.Text ?? "<error type>",
									t2?.GetClassifiedText()?.Text ?? "<error type>"),
								p1.Span));
			}

			// Find ports in entity, missing in this component
			foreach (var p2 in entityDecl.Ports)
			{
				var p1 = Ports.FirstOrDefault(x => string.Compare(p2.Name, x.Name, true) == 0);
				if (p1 == null)
					errorListener?.Invoke(new VHDLError(0, PredefinedErrorTypeNames.SyntaxError, string.Format("Missing port '{0}' from entity '{1}'", p2.Name, entityDecl.Name), Span));
			}
		}
	}
	class VHDLRecordElementDeclaration
	  : VHDLAbstractVariableDeclaration
	{
		public VHDLRecordElementDeclaration(AnalysisResult analysisResult, ParserRuleContext context, VHDLDeclaration parent, string name)
			: base(analysisResult, parent)
		{
			Context = context;
			Name = name;
		}

		private VHDLClassifiedText GetClassifiedDeclaration()
		{
			VHDLClassifiedText declText = new VHDLClassifiedText();
			declText.Add(GetClassifiedName(true));
			declText.Add(" : ");
			declText.Add(Type.GetClassifiedText());

			return declText;
		}
		public override async Task<object> BuildQuickInfoAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			System.Windows.Controls.TextBlock textBlock = GetClassifiedDeclaration().ToTextBlock();

			textBlock.Inlines.InsertBefore(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.glyphVariable());
			textBlock.Inlines.InsertAfter(textBlock.Inlines.FirstInline, VHDLQuickInfoHelper.text(" "));
			if (!string.IsNullOrWhiteSpace(Comment))
				textBlock.Inlines.Add(VHDLQuickInfoHelper.text(Environment.NewLine + Comment));

			return textBlock;
		}
		public override VHDLClassifiedText GetClassifiedName(bool fullyQualified = false)
		{
			if (fullyQualified && Parent != null && !(Parent is VHDLFileDeclaration))
			{
				VHDLClassifiedText text = Parent.GetClassifiedName(true);
				text.Add(".");
				text.Add(UndecoratedName);
				return text;
			}
			return new VHDLClassifiedText(UndecoratedName);
		}
		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.VariableImageElement);
			item.Properties["declaration"] = this;
			return item;
		}
	}

	// That is a virtual declaration, AnalysisResult is null, parent is null
	// it just has children that points to the associated packages in different documents
	class VHDLLibraryDeclaration
		: VHDLDeclaration
	{
		public VHDLLibraryDeclaration(string name)
			: base(null, null)
		{
			Name = name;
		}

		public ConcurrentDictionary<string, VHDLPackageDeclaration> Packages { get; } = new ConcurrentDictionary<string, VHDLPackageDeclaration>(StringComparer.OrdinalIgnoreCase);

		public override IEnumerable<VHDLDeclaration> Children => Packages.Values;

		public override CompletionItem BuildCompletion(IAsyncCompletionSource source)
		{
			CompletionItem item = new CompletionItem(UndecoratedName, source, VHDLQuickInfoHelper.PackageImageElement);
			return item;
		}
	}
}
