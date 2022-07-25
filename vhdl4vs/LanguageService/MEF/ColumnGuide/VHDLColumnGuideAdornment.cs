/*
 Copyright (C) 2022 jobmarley

 This file is part of vhdl4vs.

 This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
 */

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
using System.Linq;

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
				await UpdatePositionsAsync();
			}
		}
		void OnDocumentParseComplete(object sender, ParseResultEventArgs e)
		{
			_ = UpdateAsync(e.Result);
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
				_ = UpdateAsync(result);
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
			_ = UpdatePositionsAsync();
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

		IEnumerable<SnapshotSpan> GetLineTextSpans(ITextViewLine line)
		{
			List<SnapshotSpan> spans = new List<SnapshotSpan>();

			int i = 0;
			string text = line.Extent.GetText();
			while (i < text.Length)
			{
				if (!char.IsWhiteSpace(text[i]))
				{
					int end = i + 1;
					while (end < text.Length && !char.IsWhiteSpace(text[end]))
						++end;

					spans.Add(new SnapshotSpan(line.Snapshot, line.Start + i, end - i));
					i = end;
					continue;
				}

				++i;
			}

			return spans;
		}
		void PopulateLineGeometry(ITextViewLine line, GeometryGroup g, IDictionary<VHDLGuideline, Tuple<Point, Point>> coordinates)
		{
			foreach (SnapshotSpan span in GetLineTextSpans(line))
			{
				TextBounds b1 = line.GetCharacterBounds(span.Start);
				TextBounds b2 = line.GetCharacterBounds(span.End - 1);

				var co = m_guidelines.Select(x => coordinates.TryGetValue(x.Key, out var value) ? value : null).Where(x => x != null);
				if (co.Any(x => x.Item1.X >= b1.Left && x.Item1.X < b2.Right && (x.Item1.Y < b1.Bottom || x.Item2.Y > b1.Top)))
					g.Children.Add(new RectangleGeometry(new Rect(b1.Left, b1.Top, b2.Right - b1.Left, b1.Height)));

			}
		}
		Geometry CreateTextGeometry(IDictionary<VHDLGuideline, Tuple<Point, Point>> coordinates)
		{
			GeometryGroup geom = new GeometryGroup();
			geom.Children.Add(new RectangleGeometry(new Rect(view.ViewportLeft, view.ViewportTop, view.ViewportRight - view.ViewportLeft, view.ViewportBottom - view.ViewportTop)));
			foreach(ITextViewLine line in view.TextViewLines)
			{
				if (line.VisibilityState == VisibilityState.Unattached || line.VisibilityState == VisibilityState.Hidden)
					continue;

				PopulateLineGeometry(line, geom, coordinates);
			}
			geom.Freeze();
			return geom;
		}

		IDictionary<VHDLGuideline, Tuple<Point, Point>> GetGuidelinesCoordinates(List<KeyValuePair<VHDLGuideline, Line>> guidelines)
		{
			Dictionary<VHDLGuideline, Tuple<Point, Point>> coordinates = new Dictionary<VHDLGuideline, Tuple<Point, Point>>();
			foreach (KeyValuePair<VHDLGuideline, Line> g in guidelines)
			{
				Line line = g.Value;
				if (g.Key.Points != null && g.Key.Points.Count >= 2)
				{
					SnapshotPoint snapshotPt1 = g.Key.Points[0].TranslateTo(view.TextSnapshot, PointTrackingMode.Positive);
					//SnapshotPoint? p1InTopBuffer = view.BufferGraph.MapUpToSnapshot(p1, PointTrackingMode.Positive, PositionAffinity.Successor, view.VisualSnapshot);
					SnapshotPoint snapshotPt2 = g.Key.Points[1].TranslateTo(view.TextSnapshot, PointTrackingMode.Positive);
					//SnapshotPoint? p2InTopBuffer = view.BufferGraph.MapUpToSnapshot(p2, PointTrackingMode.Positive, PositionAffinity.Successor, view.VisualSnapshot);

					Point p1 = new Point();
					Point p2 = new Point();

					/*
					 * - Check if p1 > last line
					 * - check if p2 < first line
					 * - check if p1 or p2 collapsed
					 */
					if (snapshotPt2 < view.TextViewLines.FormattedSpan.Start || snapshotPt1 > view.TextViewLines.FormattedSpan.End)
					{
						continue;
					}

					if (view.TextViewLines.ContainsBufferPosition(snapshotPt1))
					{
						TextBounds b = view.TextViewLines.GetCharacterBounds(snapshotPt1);
						p1.Y = b.Bottom + 0.5;
						p1.X = p2.X = (b.Left + b.Right) / 2;
					}
					else
					{
						p1.Y = view.ViewportTop;
					}

					if (view.TextViewLines.ContainsBufferPosition(snapshotPt2))
					{
						TextBounds b = view.TextViewLines.GetCharacterBounds(snapshotPt2);
						p2.Y = b.Bottom;
					}
					else
					{
						p2.Y = view.ViewportBottom;
					}

					coordinates[g.Key] = Tuple.Create(p1, p2);
				}
			}

			return coordinates;
		}
		async Task UpdatePositionsAsync()
		{
			var guidelines = m_guidelines;
			var guidelineCoordinates = GetGuidelinesCoordinates(guidelines);
			Geometry textGeometry = CreateTextGeometry(guidelineCoordinates);
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			foreach (KeyValuePair<VHDLGuideline, Line> g in guidelines)
			{
				Line line = g.Value;

				if (guidelineCoordinates.TryGetValue(g.Key, out var coord))
				{
					line.Clip = textGeometry;
					line.Visibility = Visibility.Visible;
					line.X1 = coord.Item1.X;
					line.Y1 = coord.Item1.Y;
					line.X2 = coord.Item2.X;
					line.Y2 = coord.Item2.Y;
				}
				else
				{
					line.Visibility = Visibility.Hidden;
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
