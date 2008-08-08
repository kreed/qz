/*
 * Copyright (c) 2008 Christopher Eby
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Qz {
	enum LayoutMode { Align, Order, Shuffle };

	static class TileCollection {
		public static Font FontFace =
			new Font(FontFamily.GenericSansSerif, 12);
		public static int LineHeight = 36;
		public readonly static SolidBrush CorrectBrush =
			new SolidBrush(Color.FromArgb(85, Color.Green));
		public static int Height;

		public static void PaintTiles<T>(this Graphics g, List<T> tiles,
		                                 bool expose)
			where T : Tile
		{
			foreach (var tile in tiles)
				g.DrawString(tile.Text, FontFace,
				             tile.Correct && expose ?
				             CorrectBrush : Brushes.Black,
				             tile.Rect.Location);
		}

		public static void Layout<T>(this List<T> current,
		                             LayoutMode mode, int margin)
			where T : Tile
		{
			IEnumerator<T> e;

			if (mode == LayoutMode.Shuffle)
				e = new RandomEnumerator<T>(current);
			else {
				if (mode == LayoutMode.Align) {
					using (var g = MainWindow.Instance.CreateGraphics())
						foreach (var tile in current)
							tile.CalcSize(g);
					current.Sort(Tile.CompareLocation);
				} else if (mode == LayoutMode.Order)
					current.Sort(Tile.CompareText);
				e = current.GetEnumerator();
			}

			for (var y = 5; e.MoveNext(); y += LineHeight) {
				e.Current.X = margin;
				e.Current.Rect.Y = y;
			}

			int i = current.Count - 1;
			Height = LineHeight * i + 10
			         + current[i - 1].Rect.Height;
		}

		public static int CalcRightEdge<T>(this List<T> current)
			where T : Tile
		{
			return current.Max((Func<T, int>)(tile => tile.Rect.Width));
		}

		public static T FindContainer<T>(this List<T> current,
		                                 Point loc, bool ignore)
			where T : Tile
		{
			return current.FirstOrDefault(tile =>
			                              tile.Rect.Contains(loc)
			                              && !(tile.Correct && ignore));
		}
	}

	class Tile {
		public readonly string Text;
		public Rectangle Rect;

		private Tile pair;

		public bool Correct
		{
			get {
				return pair != null;
			}
		}

		public virtual int X
		{
			set {
				Rect.X = value;
			}
		}

		public Tile(string text, Graphics g)
		{
			Text = text;
			CalcSize(g);
		}

		public void CalcSize(Graphics g)
		{
			Rect.Size = g.MeasureString(Text, TileCollection.FontFace).ToSize();
		}

		public static int CompareText(Tile a, Tile b)
		{
			return a.Text.CompareTo(b.Text);
		}

		public static int CompareLocation(Tile a, Tile b)
		{
			return a.Rect.Y - b.Rect.Y;
		}

		public Tile Pair(Tile other)
		{
			if (pair != null)
				pair.pair = null;
			pair = other;

			if (other != null) {
				if (other.pair != null)
					other.pair = null;
				other.pair = this;
			}

			return other;
		}
	}
}
