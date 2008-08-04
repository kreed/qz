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
	static class TileCollection {
		public readonly static SolidBrush CorrectBrush =
			new SolidBrush(Color.FromArgb(85, Color.Green));
		public const int LineHeight = 40;

		public static void Paint<T>(this List<T> tiles, Graphics g) where T : Tile
		{
			foreach (var tile in tiles)
				g.DrawString(tile.Text, Program.FontFace, tile.Correct ?
				             CorrectBrush : Brushes.Black,
				             tile.Rect.Location);
		}

		public static void Layout<T>(this List<T> current, bool order, int margin) where T : Tile
		{
			if (order)
				current.Sort();
			var e = order ? (IEnumerator<T>)current.GetEnumerator()
			              : new RandomEnumerator<T>(current);

			for (int y = 25; e.MoveNext(); y += LineHeight) {
				e.Current.X = margin;
				e.Current.Rect.Y = y;
			}
		}

		public static int CalcRightEdge<T>(this List<T> current) where T : Tile
		{
			return current.Max((Func<T, int>)(tile => tile.Rect.Width));
		}

		public static T FindContainer<T>(this List<T> current, Point loc) where T : Tile
		{
			return current.FirstOrDefault(tile =>
			                              tile.Rect.Contains(loc)
			                              && !tile.Correct);
		}
	}

	class Tile : IComparable {
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
			Rect.Size = g.MeasureString(Text, Program.FontFace).ToSize();
		}

		public int CompareTo(object other)
		{
			return Text.CompareTo((other as Tile).Text);
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
