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
	class MainWindow : Form {
		public static MainWindow Instance;

		static void Main()
		{
			Application.Run(new MainWindow());
		}

		Canvas Canvas;
		Bank WordBank;
		ToolStripStatusLabel count;

		private MainWindow()
		{
			Instance = this;

			Text = "Qz";
			Size = new Size(500, 625);

			WordBank = new Bank();
			Canvas = new Canvas(WordBank);

			var menu = new MenuStrip();
			menu.ShowItemToolTips = true;
			MainMenuStrip = menu;

			{
				var file = new ToolStripMenuItem("File");

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

				file.Put("Quit, Saving Session",
				         Keys.Control | Keys.Q, delegate {
					Application.Exit();
				});
				file.Put("Quit, Discarding Session",
				         Keys.Shift | Keys.Control | Keys.Q, delegate {
					WordBank.NoSave();
					Application.Exit();
				});

				menu.Items.Add(file);
			}

			{
				var words = new ToolStripMenuItem("Settings");

				words.Put("Fewer Words", Keys.None, delegate {
					WordBank.Mod(-1);
				}).ShortcutKeyDisplayString = "Shift+ScrollDown";
				words.Put("More Words", Keys.None, delegate {
					WordBank.Mod(1);
				}).ShortcutKeyDisplayString = "Shift+ScrollUp";

				words.AddSplit();

				words.Put("Smaller Font", Keys.None, delegate {
					Canvas.AdjustFont(-1);
				}).ShortcutKeyDisplayString = "Ctrl+ScrollDown";
				words.Put("Larger Font", Keys.None, delegate {
					Canvas.AdjustFont(1);
				}).ShortcutKeyDisplayString = "Ctrl+ScrollUp";

				words.AddSplit();

				var ca = words.Put("Check Automatically", Keys.None, delegate {
					Canvas.AutoCheck = !Canvas.AutoCheck;
				});
				var sw = words.Put("Shuffle Words", Keys.None, delegate {
					Canvas.ToggleMode(ref WordBank.WordMode);
				});
				var sd = words.Put("Shuffle Meanings", Keys.None, delegate {
					Canvas.ToggleMode(ref WordBank.MeaningMode);
				});
				sd.Checked = true;
				var hd = words.Put("Hide Meanings",
				                   Keys.Control | Keys.D, delegate {
					Canvas.HideDefs = !Canvas.HideDefs;
					Canvas.Invalidate();
				});
				var hc = words.Put("Hide Correctness", Keys.None, delegate {
					Canvas.ShowCorrect = !Canvas.ShowCorrect;
					UpdateCount();
				});
				hd.CheckOnClick = sw.CheckOnClick = ca.CheckOnClick
					= sd.CheckOnClick = hc.CheckOnClick = true;

				words.AddSplit();

				// This is a little weird in a menu titled "Settings", but
				// whatever.
				words.Put("Relayout", Keys.Control | Keys.R, delegate {
					Canvas.Relayout();
				});

				menu.Items.Add(words);
			}

			var check = new ToolStripMenuItem("Check/Advance", null, delegate {
				Canvas.Check();
			});
			check.ToolTipText = "Or press space to check/advance";
			check.AutoToolTip = false;
			menu.Items.Add(check);

			count = new ToolStripStatusLabel();
			count.Alignment = ToolStripItemAlignment.Right;
			menu.Items.Add(count);

			Canvas.Location = new Point(0, menu.Height);
			Canvas.Size = ClientSize - new Size(0, menu.Height);
			Canvas.Anchor = AnchorStyles.Bottom | AnchorStyles.Right
			              | AnchorStyles.Left | AnchorStyles.Top;
			Controls.Add(menu);
			Controls.Add(Canvas);

			WordBank.Init();

			Height = TileCollection.Height + menu.Height + 36;
		}

		public void UpdateCount()
		{
			if (Canvas.ShowCorrect)
				count.Text = String.Format("{0}/{1}",
				                           WordBank.Correct,
				                           WordBank.Remaining);
			else
				count.Text = (WordBank.Correct + WordBank.Remaining).ToString();
			Canvas.Invalidate();
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
