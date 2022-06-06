using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Windows;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace vhdl4vs
{
	/// <summary>
	/// Adornment class that draws a square box in the top right hand corner of the viewport
	/// </summary>
	internal sealed class VHDLColumnGuideAdornment
	{
		/// <summary>
		/// The width of the square box.
		/// </summary>
		//private const double AdornmentWidth = 30;

		/// <summary>
		/// The height of the square box.
		/// </summary>
		//private const double AdornmentHeight = 30;

		/// <summary>
		/// Distance from the viewport top to the top of the square box.
		/// </summary>
		private const double TopMargin = 30;

		/// <summary>
		/// Distance from the viewport right to the right end of the square box.
		/// </summary>
		private const double RightMargin = 30;

		/// <summary>
		/// Text view to add the adornment on.
		/// </summary>
		private readonly IWpfTextView view;

		/// <summary>
		/// Adornment image
		/// </summary>
		//private readonly Image image;

		/// <summary>
		/// The layer for the adornment.
		/// </summary>
		private readonly IAdornmentLayer adornmentLayer;

		private IList<Line> guidelines;
		private double baseIndentation;
		private double columnWidth;
		private const double lineThickness = 1.0;

		private VHDLDocument m_doc = null;
		List<KeyValuePair<VHDLGuideline, Line>> m_guidelines = null;
		
		async Task UpdateAsync(ParseResult result)
		{
			if (result.Snapshot != null)
			{
				VHDLOutliningVisitor visitor = new VHDLOutliningVisitor(result.Snapshot);
				visitor.Visit(result.Tree);

				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				m_guidelines = CreateGuidelines(visitor.Guidelines);
				AddGuidelinesToAdornmentLayer();
				UpdatePositions();
			}
		}
		async void OnDocumentParseComplete(object sender, ParseResultEventArgs e)
		{
			await UpdateAsync(e.Result);
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="VHDLColumnGuideAdornment"/> class.
		/// Creates a square image and attaches an event handler to the layout changed event that
		/// adds the the square in the upper right-hand corner of the TextView via the adornment layer
		/// </summary>
		/// <param name="view">The <see cref="IWpfTextView"/> upon which the adornment will be drawn</param>
		public VHDLColumnGuideAdornment(IWpfTextView view, VHDLDocumentTable vhdlDocumentTable)
		{
			if (view == null)
			{
				throw new ArgumentNullException("view");
			}

			this.view = view;
			m_doc = vhdlDocumentTable.GetOrAddDocument(view.TextBuffer);
			m_doc.Parser.ParseComplete += OnDocumentParseComplete;


			this.adornmentLayer = view.GetAdornmentLayer("VHDLColumnGuideAdornment");

			m_guidelines = new List<KeyValuePair<VHDLGuideline, Line>>();

			view.LayoutChanged +=
				new EventHandler<TextViewLayoutChangedEventArgs>(OnViewLayoutChanged);
			view.Closed += new EventHandler(OnViewClosed);


			ParseResult result = m_doc.Parser.PResult;
			if (result != null)
				UpdateAsync(result);
		}

		/// <summary>
		/// Event handler for viewport layout changed event. Adds adornment at the top right corner of the viewport.
		/// </summary>
		/// <param name="sender">Event sender</param>
		/// <param name="e">Event arguments</param>
		/*private void OnSizeChanged(object sender, EventArgs e)
		{
			// Clear the adornment layer of previous adornments
			this.adornmentLayer.RemoveAllAdornments();

			// Place the image in the top right hand corner of the Viewport
			Canvas.SetLeft(this.image, this.view.ViewportRight - RightMargin - AdornmentWidth);
			Canvas.SetTop(this.image, this.view.ViewportTop + TopMargin);

			// Add the image to the adornment layer and make it relative to the viewport
			this.adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, this.image, null);
		}*/

		void OnViewClosed(object sender, EventArgs e)
		{
			m_doc.Parser.ParseComplete -= OnDocumentParseComplete;

			view.LayoutChanged -= OnViewLayoutChanged;
			view.Closed -= OnViewClosed;
		}

		private bool _firstLayoutDone;

		void OnViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			bool fUpdatePositions = false;

			IFormattedLineSource lineSource = view.FormattedLineSource;
			if (lineSource == null)
			{
				return;
			}
			UpdatePositions();
		}

		private List<KeyValuePair<VHDLGuideline, Line>> CreateGuidelines(IList<VHDLGuideline> guidelines)
		{
			Brush lineBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
			DoubleCollection dashArray = new DoubleCollection(new double[] { 3.0, 3.0 });
			//IList<Line> result = new List<Line>();
			List<KeyValuePair<VHDLGuideline, Line>> result = new List<KeyValuePair<VHDLGuideline, Line>>();
			foreach (VHDLGuideline g in guidelines)
			{
				Line line = new Line()
				{
					// Use the DataContext slot as a cookie to hold the column
					Opacity = 0.184313,
					Stroke = lineBrush,
					StrokeThickness = lineThickness,
					StrokeDashArray = dashArray
				};
				result.Add(new KeyValuePair<VHDLGuideline, Line>(g, line));
			}
			return result;
		}

		void PopulateLineGeometry(ITextViewLine line, GeometryGroup g)
		{
			int iStart = 0;
			int i = 0;
			foreach(char c in line.Extent.GetText())
			{
				if(char.IsWhiteSpace(c))
				{
					if(iStart < i)
					{
						foreach(TextBounds b in line.GetNormalizedTextBounds(new SnapshotSpan(line.Snapshot, line.Start + iStart, i - iStart)))
						{
							g.Children.Add(new RectangleGeometry(new Rect(b.Left, b.Top, b.Width, b.Height)));
						}
					}
					iStart = i + 1;
				}
				++i;
			}
			if (iStart < i)
			{
				foreach (TextBounds b in line.GetNormalizedTextBounds(new SnapshotSpan(line.Snapshot, line.Start + iStart, i - iStart)))
				{
					g.Children.Add(new RectangleGeometry(new Rect(b.Left, b.Top, b.Width, b.Height)));
				}
			}
		}
		Geometry CreateTextGeometry()
		{
			GeometryGroup geom = new GeometryGroup();
			geom.Children.Add(new RectangleGeometry(new Rect(view.ViewportLeft, view.ViewportTop, view.ViewportRight - view.ViewportLeft, view.ViewportBottom - view.ViewportTop)));
			foreach(ITextViewLine line in view.TextViewLines)
			{
				if (line.VisibilityState == VisibilityState.Unattached || line.VisibilityState == VisibilityState.Hidden)
					continue;

				PopulateLineGeometry(line, geom);
				/*var aaa = line.GetNormalizedTextBounds(line.Extent);
				RectangleGeometry rect = new RectangleGeometry(new Rect(line.TextLeft, line.Top, line.TextWidth, line.Height));
				geom.Children.Add(rect);*/
			}
			geom.Freeze();
			return geom;
		}
		Geometry m_textGeometry = null;
		void UpdatePositions()
		{
			m_textGeometry = CreateTextGeometry();
			foreach (KeyValuePair<VHDLGuideline, Line> g in m_guidelines)
			{
				Line line = g.Value;
				if (g.Key.Points != null && g.Key.Points.Count >= 2)
				{
					SnapshotPoint p1 = g.Key.Points[0].TranslateTo(view.TextSnapshot, PointTrackingMode.Positive);
					//SnapshotPoint? p1InTopBuffer = view.BufferGraph.MapUpToSnapshot(p1, PointTrackingMode.Positive, PositionAffinity.Successor, view.VisualSnapshot);
					SnapshotPoint p2 = g.Key.Points[1].TranslateTo(view.TextSnapshot, PointTrackingMode.Positive);
					//SnapshotPoint? p2InTopBuffer = view.BufferGraph.MapUpToSnapshot(p2, PointTrackingMode.Positive, PositionAffinity.Successor, view.VisualSnapshot);

					/*
					 * - Check if p1 > last line
					 * - check if p2 < first line
					 * - check if p1 or p2 collapsed
					 */
					if(p2 < view.TextViewLines.FormattedSpan.Start || p1 > view.TextViewLines.FormattedSpan.End)
					{
						line.Visibility = Visibility.Hidden;
						continue;
					}
					line.Clip = m_textGeometry;
					line.Visibility = Visibility.Visible;

					if (view.TextViewLines.ContainsBufferPosition(p1))
					{
						TextBounds b = view.TextViewLines.GetCharacterBounds(p1);
						line.Y1 = b.Bottom + 0.5;
						line.X1 = line.X2 = (b.Left + b.Right) / 2;
					}
					else
					{
						//TextBounds b = view.TextViewLines.GetCharacterBounds(p1);
						line.Y1 = view.ViewportTop;
						//X = (b.Left + b.Right) / 2;
					}

					if (view.TextViewLines.ContainsBufferPosition(p2))
					{
						TextBounds b = view.TextViewLines.GetCharacterBounds(p2);
						line.Y2 = b.Bottom;
					}
					else
					{
						line.Y2 = view.ViewportBottom;
					}
				}
			}
		}

		void AddGuidelinesToAdornmentLayer()
		{
			// Grab a reference to the adornment layer that this adornment
			// should be added to
			// Must match exported name in ColumnGuideAdornmentTextViewCreationListener
			IAdornmentLayer adornmentLayer =
				view.GetAdornmentLayer("VHDLColumnGuideAdornment");
			if (adornmentLayer == null)
				return;
			adornmentLayer.RemoveAllAdornments();
			// Add the guidelines to the adornment layer and make them relative
			// to the viewport
			if (m_guidelines == null)
				return;
			foreach (KeyValuePair<VHDLGuideline, Line> g in m_guidelines)
			{
				Line line = g.Value;
				if (g.Key.Points != null && g.Key.Points.Count >= 2)
				{
					adornmentLayer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled,
													null, null, line, null);
				}
			}
		}
	}
}
