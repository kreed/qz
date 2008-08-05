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
using System.Drawing;
using System.Windows.Forms;

namespace Qz {
	class Canvas : ScrollableControl {
		Bank WordBank;

		bool proceed;
		Meaning moving;
		Point lastLoc;
		int scrollOffset;
		Timer scrollTimer;

		public bool HideDefs;
		public bool AutoCheck;
		public bool ShowCorrect = true;

		public Canvas(Bank wb)
		{
			WordBank = wb;

			BackColor = Color.White;
			AutoScroll = true;

			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
			         | ControlStyles.DoubleBuffer, true);

			scrollTimer = new Timer();
			scrollTimer.Interval = 50;
			scrollTimer.Tick += DragScroll;
		}

		public void Relayout()
		{
			WordBank.Reload();
			proceed = false;
		}

		public void Check()
		{
			Drop();
			if (proceed) {
				if (WordBank.Check())
					WordBank.NextGroup();
				proceed = false;
			} else if (WordBank.Check())
				proceed = true;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (WordBank.Finished) {
				AutoScrollMinSize = Size.Empty;
				TextRenderer.DrawText(e.Graphics, "SUCCESS",
				                      new Font(FontFamily.GenericSansSerif, 36),
				                      DisplayRectangle, Color.Black,
				                      TextFormatFlags.HorizontalCenter |
				                      TextFormatFlags.VerticalCenter);
			} else {
				AutoScrollMinSize
					= new Size(0, WordBank.Words.Count * TileCollection.LineHeight - 10);

				var g = e.Graphics;
				g.TranslateTransform(0, AutoScrollPosition.Y);

				g.PaintTiles(WordBank.Words, ShowCorrect || proceed);
				if (!HideDefs)
					g.PaintTiles(WordBank.Meanings, ShowCorrect || proceed);
			}
			base.OnPaint(e);
		}

		// Since Microsoft hates us, it only allows us to specify a ShortcutKey
		// that includes a modifier, and doesn't allow us to specify multiple
		// shortcuts for a single item. So we have this.
		//
		// Curiously, Mono doesn't appear to have this limitation..
		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch (e.KeyCode) {
			case Keys.Space:
				Check();
				break;
			case Keys.Add:
			case Keys.Oemplus:
				WordBank.AddRandom();
				break;
			case Keys.Subtract:
			case Keys.OemMinus:
				WordBank.RestoreLast();
				break;
			case Keys.J:
			case Keys.Down:
				VScrollBy(25);
				break;
			case Keys.K:
			case Keys.Up:
				VScrollBy(-25);
				break;
			}
			base.OnKeyDown(e);
		}

		public void VScrollBy(int v)
		{
			AutoScrollPosition = new Point(0, -AutoScrollPosition.Y + v);
		}

		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			Check();
			base.OnMouseDoubleClick(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left) {
				var loc = e.Location;
				loc.Y -= AutoScrollPosition.Y;
				moving = WordBank.Meanings
					.FindContainer(e.Location, ShowCorrect || proceed);

				if (moving != null) {
					MouseMove += OnMouseMove;
					lastLoc = e.Location;
				}
			}
			base.OnMouseDown(e);
		}

		public void Drop()
		{
			if (moving != null) {
				MouseMove -= OnMouseMove;
				moving = null;
				scrollTimer.Enabled = false;
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (moving != null && e.Button == MouseButtons.Left) {
				Drop();
				if (AutoCheck)
					Check();
			}
			base.OnMouseUp(e);
		}

		private void DragScroll(object o, EventArgs e)
		{
			int y = AutoScrollPosition.Y;
			VScrollBy(scrollOffset);
			MoveBy(0, y - AutoScrollPosition.Y);

			if (AutoScrollPosition.Y == 0 ||
			    AutoScrollPosition.Y ==
				ClientRectangle.Height - DisplayRectangle.Height)
				scrollTimer.Enabled = false;
		}

		private void MoveBy(int x, int y)
		{
			var update = moving.Rect;
			moving.Rect.X += x;
			moving.Rect.Y += y;
			moving.Moved = true;
			update = Rectangle.Union(update, moving.Rect);
			update.Offset(AutoScrollPosition);
			Invalidate(update);
		}

		private void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (ClientRectangle.Contains(e.Location)) {
				lastLoc -= new Size(e.Location);
				MoveBy(-lastLoc.X, -lastLoc.Y);
				lastLoc = e.Location;

				if (e.Location.Y + 30 > ClientRectangle.Height) {
					scrollOffset = 20;
					scrollTimer.Enabled = true;
				} else if (e.Location.Y - 40 < 0) {
					scrollOffset = -20;
					scrollTimer.Enabled = true;
				} else
					scrollTimer.Enabled = false;
			}
		}
	}
}
