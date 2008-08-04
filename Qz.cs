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
	class Program : Form {
		public static Program Instance;
		public static Font FontFace =
			new Font(FontFamily.GenericSansSerif, 12);

		Bank WordBank = new Bank();

		bool proceed;
		Meaning moving;
		Point lastLoc;
		int scrollOffset;
		ToolStripStatusLabel count;
		Timer scrollTimer;

		bool hideDefs;
		bool autoCheck;

		static void Main()
		{
			Application.Run(new Program());
		}

		private Program()
		{
			Instance = this;

			Text = "Qz";
			BackColor = Color.White;
			Size = new Size(500, 685);
			AutoScroll = true;

			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
			         | ControlStyles.DoubleBuffer, true);

			scrollTimer = new Timer();
			scrollTimer.Interval = 50;
			scrollTimer.Tick += new EventHandler(DragScroll);

			KeyDown += OnKeyDown;
			MouseUp += OnMouseUp;
			MouseDown += OnMouseDown;

			var menu = new MenuStrip();
			MainMenuStrip = menu;

			{
				var file = new ToolStripMenuItem("File");

				file.Put("Check/Advance", Keys.None, delegate {
					Check();
				}).ShortcutKeyDisplayString = "Space";
				file.Put("Check Automatically", Keys.None, delegate {
					autoCheck = !autoCheck;
				}).CheckOnClick = true;

				file.AddSplit();

				file.Put("Load Words...", Keys.Control | Keys.O, delegate {
					ShowFileDialog<OpenFileDialog>(WordBank.Fill);
				});
				file.Put("Load Previous Session", Keys.None, delegate {
					WordBank.Fill(WordBank.BankStateFile);
				});
				file.Put("Load Embedded Words", Keys.None,  delegate {
					WordBank.FillFromEmbed();
				});

				file.AddSplit();

				file.Put("Save Remaining...", Keys.Control | Keys.S, delegate {
					ShowFileDialog<SaveFileDialog>(WordBank.Dump);
				});
				file.Put("Save Session Now", Keys.None, delegate {
					WordBank.Dump(WordBank.BankStateFile);
				});

				file.AddSplit();

				file.Put("Quit, Saving Session", Keys.Control | Keys.Q, delegate {
					Application.Exit();
				});
				file.Put("Quit, Discarding Session", Keys.Shift | Keys.Control | Keys.Q, delegate {
					WordBank.NoSave();
					Application.Exit();
				});

				menu.Items.Add(file);
			}

			{
				var view = new ToolStripMenuItem("View");

				// There doesn't seem to be a good constant for plus/minus
				// (You have to use two for each, and both of those display
				// their names instead of their symbols in the menu)
				view.Put("Fewer", Keys.None, delegate {
					WordBank.RestoreLast();
				}).ShortcutKeyDisplayString = "-";
				view.Put("More", Keys.None, delegate {
					WordBank.AddRandom();
				}).ShortcutKeyDisplayString = "+";

				view.AddSplit();

				var words = view.Put("Shuffle Words", Keys.None, delegate {
					WordBank.OrderWords = !WordBank.OrderWords;
					Relayout();
				});
				var defs = view.Put("Shuffle Meanings", Keys.None, delegate {
					WordBank.OrderMeanings = !WordBank.OrderMeanings;
					Relayout();
				});
				var hide = view.Put("Hide Meanings", Keys.Control | Keys.D, delegate {
					hideDefs = !hideDefs;
					Invalidate();
				});
				hide.CheckOnClick = words.CheckOnClick = defs.CheckOnClick = true;
				defs.Checked = true;

				view.AddSplit();

				view.Put("Relayout", Keys.Control | Keys.R, delegate {
					Relayout();
				});

				menu.Items.Add(view);
			}

			count = new ToolStripStatusLabel();
			count.Alignment = ToolStripItemAlignment.Right;
			menu.Items.Add(count);

			Controls.Add(menu);

			WordBank.Init();
		}

		delegate bool Callback(string val);
		private void ShowFileDialog<T>(Callback cb)
			where T : FileDialog, new()
		{
			var dlg = new T();
			dlg.Filter = "Tab Separated Values|*.tsv";
			dlg.RestoreDirectory = true;
			if (dlg.ShowDialog(this) == DialogResult.OK)
				cb(dlg.FileName);
		}

		private void Relayout()
		{
			WordBank.Reload();
			proceed = false;
		}

		public void UpdateCount(int active, int bank)
		{
			count.Text = String.Format("{0}/{1}", active, bank);
			Invalidate();
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
					= new Size(0, WordBank.GroupSize * TileCollection.LineHeight + 10);

				var g = e.Graphics;
				g.TranslateTransform(0, AutoScrollPosition.Y);

				WordBank.Words.Paint(g);
				if (!hideDefs)
					WordBank.Meanings.Paint(g);
			}
		}

		private void Check()
		{
			if (proceed) {
				if (WordBank.Check())
					WordBank.NextGroup();
				proceed = false;
			} else if (WordBank.Check())
				proceed = true;
		}

		// Since Shortcut hates us, it has implemented itself as an enum,
		// meaning we can't specify shortcuts other than those blessed by
		// Microsoft. Thus we have our own key handler and specify fake
		// shortcuts in the MenuItems
		private void OnKeyDown(object sender, KeyEventArgs e)
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
		}

		public void VScrollBy(int v)
		{
			AutoScrollPosition = new Point(0, -AutoScrollPosition.Y + v);
		}

		private void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left) {
				var loc = e.Location;
				loc.Y -= AutoScrollPosition.Y;
				moving = WordBank.Meanings.FindContainer(e.Location);

				if (moving != null) {
					MouseMove += OnMouseMove;
					lastLoc = e.Location;
				}
			}
		}

		private void OnMouseUp(object sender, MouseEventArgs e)
		{
			if (moving != null && e.Button == MouseButtons.Left) {
				MouseMove -= OnMouseMove;
				moving = null;
				scrollTimer.Enabled = false;

				if (autoCheck)
					WordBank.Check();
			}
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

	static class MenuUtil {
		public static ToolStripMenuItem Put(this ToolStripMenuItem menu,
		                           string text, Keys sc, EventHandler e)
		{
			var item = new ToolStripMenuItem(text, null, e);
			item.ShortcutKeys = sc;
			menu.DropDownItems.Add(item);
			return item;
		}

		public static void AddSplit(this ToolStripMenuItem menu)
		{
			menu.DropDownItems.Add(new ToolStripSeparator());
		}
	}
}
