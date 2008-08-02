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
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Qz {
	class Tile : IComparable {
		public readonly string Text;
		public bool Correct;
		public Rectangle Rect;

		public Tile(string text, Graphics g)
		{
			Text = text;
			Rect.Size = g.MeasureString(Text, Program.FontFace).ToSize();
		}

		public int CompareTo(object other)
		{
			return Text.CompareTo((other as Tile).Text);
		}
	}
	class Word : Tile {
		private const string bankStateFile = "stored.txt";
		public const int LineHeight = 40;

		static List<string> bank;
		static List<Word> current = new List<Word>();
		public static int Count;
		public static int GroupSize = 16;
		public static bool Finished;

		// A few sanity checks here would probably be wise here. Currently,
		// we don't actually parse the file until we display the words. We
		// should parse it here or at least make sure it's parsable.
		public static bool FillBank(string file)
		{
			if (file == null)
				file = bankStateFile;

			bank = new List<string>();
			string line;

			try {
				using (var sr = new System.IO.StreamReader(file))
					while ((line = sr.ReadLine()) != null)
						bank.Add(line);
			} catch (Exception e) {
				MessageBox.Show("Error reading word bank: " + e.Message);
				return false;
			}

			Finished = false;

			NextGroup();
			return true;
		}

		public static bool DumpBank(string file)
		{
			TestAllCorrect(false);

			if (file == null)
				file = bankStateFile;

			try {
				using (var sw = new System.IO.StreamWriter(file)) {
					foreach (var line in bank)
						sw.WriteLine(line);
					foreach (var word in current)
						if (!word.Correct)
							sw.WriteLine(word.Text + '\t' + word.Meaning.Text);
				}
			} catch (Exception e) {
				MessageBox.Show("Error writing word bank: " + e.Message);
				return false;
			}

			return true;
		}

		public static bool TestAllCorrect(bool reset)
		{
			if (reset)
				foreach (var word in current)
					word.Correct = word.Meaning.Correct = false;

			// It would be better to use Count(), but Mono (as of 1.9.1)
			// ignores it because it has the same name as a property. . .
			int wrong = (int)current.LongCount(word => !word.TestCorrect());
			Count = bank.Count + wrong;
			return wrong == 0;
		}

		static void OrderAll()
		{
			current.Sort();

			int marginX = current.Max((Func<Word, int>)(word => word.Rect.Width));

			int y = -20;
			foreach (var word in current) {
				word.Rect.X = marginX - word.Rect.Width;
				word.Rect.Y = y += LineHeight;
			}

			Meaning.Offset = marginX + 5;
			Meaning.ShuffleAll();
			TestAllCorrect(true);
		}

		public static void PaintAll(Graphics g)
		{
			foreach (var word in current)
				g.DrawString(word.Text, Program.FontFace, word.Correct ?
				             Program.CorrectBrush : Brushes.Black,
				             word.Rect.Location);
		}

		public static void AddRandom()
		{
			if (bank.Count != 0) {
				using (var g = Program.Instance.CreateGraphics())
					current.Add(new Word(bank.TakeAt(Util.Random.Next(bank.Count)).Split('\t'), g));
				GroupSize = current.Count;
				OrderAll();
			}
		}

		public static void RestoreLast()
		{
			if (!Finished && current.Count != 1) {
				bank.Add(current.TakeAt(current.Count - 1).Remove());
				GroupSize = current.Count;
				OrderAll();
			}
		}

		public static void NextGroup()
		{	
			current.Clear();
			Meaning.ClearAll();

			if (bank.Count == 0) {
				Finished = true;
			} else {
				int size = GroupSize;
				using (var g = Program.Instance.CreateGraphics())
					while (--size != -1 && bank.Count != 0)
						current.Add(new Word(bank.TakeAt(Util.Random.Next(bank.Count)).Split('\t'), g));

				OrderAll();
			}
		}

		public readonly Meaning Meaning;

		public Word(string[] line, Graphics g)
			: base(line[0], g)
		{
			Meaning = new Meaning(line[1], g);
		}

		public bool TestCorrect()
		{
			var def = Meaning.GetCorrect(this);
			if (def != null)
				return Correct = def.Correct = true;
			else
				return false;
		}

		public string Remove()
		{
			Meaning.Remove();
			return Text + '\t' + Meaning.Text;
		}
	}

	class Meaning : Tile {
		public static int Offset;
		static List<Meaning> current = new List<Meaning>();

		public static void PaintAll(Graphics g)
		{
			foreach (var def in current)
				g.DrawString(def.Text, Program.FontFace,
				             def.Correct ? Program.CorrectBrush : Brushes.Black,
				             def.Rect.Location);
		}

		public static Meaning FindContainer(Point loc)
		{
			return current.FirstOrDefault(def =>
				                       def.Rect.Contains(loc)
				                       && !def.Correct);
		}

		public static void ShuffleAll()
		{
			var rands = new List<Meaning>(current);
			int i;

			for (int y = 20; rands.Count != 0; y += Word.LineHeight) {
				// This has the possibility of leaving one meaning at its
				// original position, however the chances are low enough
				// that it's not worth the added complexity to fix this.
				// A free word every now and then is satisfying anyway. ^_^
				do {
					i = Util.Random.Next(rands.Count);
				} while (rands[i].Rect.Y == y && rands.Count != 1);
				var def = rands.TakeAt(i);
				def.Rect.X = Offset;
				def.Rect.Y = y;
			}
		}

		public static void ClearAll()
		{
			current.Clear();
		}

		public Meaning Next;

		public Meaning(string text, Graphics g)
			: base(text, g)
		{
			var dup = current.FirstOrDefault(def => def.Text == Text);
			if (dup != null) {
				Next = dup.Next;
				dup.Next = this;
			} else
				Next = this;

			current.Add(this);
		}

		public Meaning GetCorrect(Word word)
		{
			if (word.Rect.Y - 15 < Rect.Y
			 && word.Rect.Y + 25 > Rect.Y)
				return this;
			if (word.Meaning.Equals(Next))
				return null;
			return Next.GetCorrect(word);
		}

		public void Remove()
		{
			current.Remove(this);

			var def = Next;
			while (def.Next != this)
				def = def.Next;
			def.Next = Next;
		}
	}

	class Program : Form {
		public static Program Instance;
		public static SolidBrush CorrectBrush =
			new SolidBrush(Color.FromArgb(85, Color.Green));
		public static Font FontFace =
			new Font(FontFamily.GenericSansSerif, 12);

		bool proceed;
		Meaning moving;
		Point lastLoc;

		MouseEventHandler motion;
		MenuItem toggleAC;
		MenuItem toggleDefs;
		int scrollOffset;
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
			Width = 500;
			Height = 700;

			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
			         | ControlStyles.DoubleBuffer, true);

			scrollTimer = new Timer();
			scrollTimer.Interval = 50;
			scrollTimer.Tick += new EventHandler(DragScroll);

			KeyDown += new KeyEventHandler(OnKeyDown);
			MouseUp += new MouseEventHandler(OnMouseUp);
			MouseDown += new MouseEventHandler(OnMouseDown);
			motion = new MouseEventHandler(OnMouseMove);

			Menu = new MainMenu();

			{
				var file = Menu.Put("File");

				file.Put("Check/Advance\tSpace", delegate { Check(); });
				toggleAC = file.Put("Check Automatically", delegate {
					toggleAC.Checked = autoCheck = !autoCheck;
				});

				file.Put("-");

				file.Put("Load Remaining\tL", delegate {
					Word.FillBank(null);
					Invalidate();
				});
				file.Put("Load Remaining From...", Shortcut.CtrlO, delegate {
					ShowFileDialog(new OpenFileDialog(), Word.FillBank);
					Invalidate();
				});

				file.Put("-");

				file.Put("Save Remaining\tD", delegate {
					Word.DumpBank(null);
				});
				file.Put("Save Remaining To...", Shortcut.CtrlS, delegate {
					ShowFileDialog(new SaveFileDialog(), Word.DumpBank);
				});

				file.Put("-");

				file.Put("Quit", Shortcut.CtrlQ, delegate {
					Application.Exit();
				});
			}

			{
				var view = Menu.Put("View");

				view.Put("Fewer\t-", delegate {
					Word.RestoreLast();
					Invalidate();
				});
				view.Put("More\t+", delegate {
					Word.AddRandom();
					Invalidate();
				});

				view.Put("-");

				view.Put("Shuffle Meanings\tS", delegate {
					ShuffleDefs();
				});
				toggleDefs = view.Put("Hide Meanings\tH", delegate {
					ToggleDefs();
				});
			}

			if (!Word.FillBank("words.txt"))
				ShowFileDialog(new OpenFileDialog(), Word.FillBank);
		}

		private void ShowFileDialog(FileDialog dlg, Func<string, bool> cb)
		{
			dlg.DefaultExt = "*.*";
			dlg.RestoreDirectory = true;
			if (dlg.ShowDialog() == DialogResult.OK)
				cb(dlg.FileName);
		}

		private void ToggleDefs()
		{
			toggleDefs.Checked = hideDefs = !hideDefs;
			Invalidate();
		}

		private void ShuffleDefs()
		{
			Meaning.ShuffleAll();
			Word.TestAllCorrect(true);
			proceed = false;
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (Word.Finished) {
				AutoScrollMinSize = Size.Empty;
				TextRenderer.DrawText(e.Graphics, "SUCCESS",
				                      new Font(FontFamily.GenericSansSerif, 36),
				                      DisplayRectangle, Color.Black,
				                      TextFormatFlags.HorizontalCenter |
				                      TextFormatFlags.VerticalCenter);
			} else {
				AutoScrollMinSize
					= new Size(0, Word.GroupSize * Word.LineHeight + 10);

				var g = e.Graphics;
				g.TranslateTransform(0, AutoScrollPosition.Y);

				g.DrawString("Remaining: " + Word.Count
				             , FontFace, Brushes.Black, 0, 0);

				Word.PaintAll(g);
				if (!hideDefs)
					Meaning.PaintAll(g);
			}
		}

		private void Check()
		{
			if (proceed) {
				if (Word.TestAllCorrect(false))
					Word.NextGroup();
				proceed = false;
			} else if (Word.TestAllCorrect(false))
				proceed = true;

			Invalidate();
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
			case Keys.Oemplus:
			case Keys.Add:
				Word.AddRandom();
				Invalidate();
				break;
			case Keys.OemMinus:
			case Keys.Subtract:
				Word.RestoreLast();
				Invalidate();
				break;
			case Keys.H:
				ToggleDefs();
				break;
			case Keys.S:
				ShuffleDefs();
				break;
			case Keys.D:
				Word.DumpBank(null);
				break;
			case Keys.L:
				Word.FillBank(null);
				Invalidate();
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

		[DllImport("user32.dll")]
		private static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

		// Why there is not a method that both moves the scrollbar _and_ client
		// area escapes me...
		public void VScrollBy(int v)
		{
			v = AutoScrollPosition.Y - v;
			SetDisplayRectLocation(0, v);
			SetScrollPos(Handle, 0x1, -v, true);
		}

		private void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left) {
				var loc = e.Location;
				loc.Y -= AutoScrollPosition.Y;
				moving = Meaning.FindContainer(e.Location);

				if (moving != null) {
					MouseMove += motion;
					lastLoc = e.Location;
				}
			}
		}

		private void OnMouseUp(object sender, MouseEventArgs e)
		{
			if (moving != null && e.Button == MouseButtons.Left) {
				MouseMove -= motion;
				moving = null;
				scrollTimer.Enabled = false;

				if (autoCheck)
					Check();
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
			var update = new Region(moving.Rect);
			moving.Rect.X += x;
			moving.Rect.Y += y;
			update.Union(moving.Rect);
			update.Translate(0, AutoScrollPosition.Y);
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

	static class Util {
		public static Random Random = new Random();

		// Why doesn't .NET have a take method? :-/
		public static T TakeAt<T>(this IList<T> l, int i)
		{
			var e = l[i];
			l.RemoveAt(i);
			return e;
		}

		public static MenuItem Put(this Menu menu, string text,
		                           EventHandler e)
		{
			return menu.Put(text, Shortcut.None, e);
		}

		public static MenuItem Put(this Menu menu, string text,
		                           Shortcut sc, EventHandler e)
		{
			var item = new MenuItem(text, e, sc);
			menu.MenuItems.Add(item);
			return item;
		}

		public static MenuItem Put(this Menu menu, string text)
		{
			return menu.Put(text, Shortcut.None, null);
		}
	}
}
