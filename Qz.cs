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
	class Bank {
		private const string bankStateFile = "stored.txt";
		public int Count;
		public int GroupSize = 16;
		private List<string> bank = new List<string>();
		public List<Word> Words = new List<Word>();
		public List<Meaning> Meanings = new List<Meaning>();

		public bool Finished
		{
			get {
				return Count == 0 && Words.Count == 0;
			}
		}

		// A few sanity checks here would probably be wise here. Currently,
		// we don't actually parse the file until we display the words. We
		// should parse it here or at least make sure it's parsable.
		public bool FillBank(string file)
		{
			if (file == null)
				file = bankStateFile;

			bank.Clear();
			string line;

			try {
				using (var sr = new System.IO.StreamReader(file))
					while ((line = sr.ReadLine()) != null)
						bank.Add(line);
			} catch (Exception e) {
				MessageBox.Show("Error reading word bank: " + e.Message);
				return false;
			}

			NextGroup();
			return true;
		}

		public bool DumpBank(string file)
		{
			Check();

			if (file == null)
				file = bankStateFile;

			try {
				using (var sw = new System.IO.StreamWriter(file)) {
					foreach (var line in bank)
						sw.WriteLine(line);
					foreach (var word in Words)
						if (!word.Correct)
							sw.WriteLine(word.Text + '\t' + word.Meaning.Text);
				}
			} catch (Exception e) {
				MessageBox.Show("Error writing word bank: " + e.Message);
				return false;
			}

			return true;
		}

		private void Next(Graphics g)
		{
			var line = bank.TakeAt(Util.Random.Next(bank.Count)).Split('\t');
			var meaning = new Meaning(line[1], g, Meanings);
			Words.Add(new Word(line[0], meaning, g));
			Meanings.Add(meaning);
		}

		public bool Check()
		{
			int wrong = Words.TestWrong();
			Count = Words.Count - wrong + bank.Count;
			Program.Instance.Invalidate();
			return wrong == 0;
		}

		public void Shuffle()
		{
			Meanings.Shuffle(Words.CalcRightEdge() + 5);
			Check();
		}

		private void Reload()
		{
			int edge = Words.Order() + 5;
			Meanings.Shuffle(edge);
			Check();
		}

		public void AddRandom()
		{
			if (bank.Count != 0) {
				using (var g = Program.Instance.CreateGraphics())
					Next(g);
				GroupSize = Words.Count;
				Reload();
			}
		}

		public void RestoreLast()
		{
			if (Words.Count > 1) {
				var word = Words.TakeAt(Words.Count - 1);
				word.Meaning.Remove();
				Words.Remove(word);
				Meanings.Remove(word.Meaning);
				bank.Add(word.Text + '\t' + word.Meaning.Text);
				GroupSize = Words.Count;
				Reload();
			}
		}

		public void NextGroup()
		{
			Words.Clear();
			Meanings.Clear();

			if (bank.Count != 0) {
				int size = GroupSize;
				using (var g = Program.Instance.CreateGraphics())
					while (--size != -1 && bank.Count != 0)
						Next(g);
				Reload();
			}
		}
	}

	static class TileCollection {
		public static SolidBrush CorrectBrush =
			new SolidBrush(Color.FromArgb(85, Color.Green));
		public const int LineHeight = 40;

		public static void Paint<T>(this List<T> tiles, Graphics g) where T : Tile
		{
			foreach (var tile in tiles)
				g.DrawString(tile.Text, Program.FontFace, tile.Correct ?
				             CorrectBrush : Brushes.Black,
				             tile.Rect.Location);
		}
	}

	class Tile : IComparable {
		public readonly string Text;
		public Rectangle Rect;

		private Tile pair;

		public virtual bool Correct
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

	static class WordCollection {
		public static int TestWrong(this List<Word> current)
		{
			// It would be better to use Count(), but Mono (as of 1.9.1)
			// ignores it because it has the same name as a property. . .
			return (int)current.LongCount(word => !word.TestCorrect());
		}

		public static int Order(this List<Word> current)
		{
			current.Sort();

			int right = current.CalcRightEdge();
			int y = -20;
			foreach (var word in current) {
				word.Rect.X = right - word.Rect.Width;
				word.Rect.Y = y += TileCollection.LineHeight;
			}

			return right;
		}

		public static int CalcRightEdge(this List<Word> current)
		{
			return current.Max((Func<Word, int>)(word => word.Rect.Width));
		}
	}


	class Word : Tile {
		public readonly Meaning Meaning;

		public override int X
		{
			set {
				Rect.X = value - Rect.Width - 5;
			}
		}

		public Word(string text, Meaning meaning, Graphics g)
			: base(text, g)
		{
			Meaning = meaning;
		}

		public bool TestCorrect()
		{
			return Pair(Meaning.GetCorrect(this)) != null;
		}
	}

	static class MeaningCollection {
		public static Meaning FindContainer(this List<Meaning> current, Point loc)
		{
			return current.FirstOrDefault(def =>
			                              def.Rect.Contains(loc)
			                              && !def.Correct);
		}

		public static void Shuffle<T>(this List<T> current, int offset) where T : Tile
		{
			var rands = new List<T>(current);
			for (int y = 20; rands.Count != 0; y += TileCollection.LineHeight) {
				var def = rands.TakeAt(Util.Random.Next(rands.Count));
				def.Rect.X = offset;
				def.Rect.Y = y;
			}
		}
	}

	class Meaning : Tile {
		public Meaning Next;

		public Meaning(string text, Graphics g, List<Meaning> current)
			: base(text, g)
		{
			var dup = current.FirstOrDefault(def => def.Text == Text);
			if (dup != null) {
				Next = dup.Next;
				dup.Next = this;
			} else
				Next = this;
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
			Pair(null);
			var def = Next;
			while (def.Next != this)
				def = def.Next;
			def.Next = Next;
		}
	}

	class Program : Form {
		public static Program Instance;
		public static Font FontFace =
			new Font(FontFamily.GenericSansSerif, 12);

		Bank WordBank = new Bank();

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
			Size = new Size(500, 700);
			AutoScroll = true;

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
					WordBank.FillBank(null);
				});
				file.Put("Load Remaining From...", Shortcut.CtrlO, delegate {
					ShowFileDialog(new OpenFileDialog(), WordBank.FillBank);
				});

				file.Put("-");

				file.Put("Save Remaining\tD", delegate {
					WordBank.DumpBank(null);
				});
				file.Put("Save Remaining To...", Shortcut.CtrlS, delegate {
					ShowFileDialog(new SaveFileDialog(), WordBank.DumpBank);
				});

				file.Put("-");

				file.Put("Quit", Shortcut.CtrlQ, delegate {
					Application.Exit();
				});
			}

			{
				var view = Menu.Put("View");

				view.Put("Fewer\t-", delegate {
					WordBank.RestoreLast();
				});
				view.Put("More\t+", delegate {
					WordBank.AddRandom();
				});

				view.Put("-");

				view.Put("Shuffle Meanings\tS", delegate {
					ShuffleDefs();
				});
				toggleDefs = view.Put("Hide Meanings\tH", delegate {
					ToggleDefs();
				});
			}

			if (!WordBank.FillBank("words.txt"))
				ShowFileDialog(new OpenFileDialog(), WordBank.FillBank);
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
			WordBank.Shuffle();
			proceed = false;
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

				g.DrawString("Remaining: " + WordBank.Count
				             , FontFace, Brushes.Black, 0, 0);

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
			case Keys.Oemplus:
			case Keys.Add:
				WordBank.AddRandom();
				break;
			case Keys.OemMinus:
			case Keys.Subtract:
				WordBank.RestoreLast();
				break;
			case Keys.H:
				ToggleDefs();
				break;
			case Keys.S:
				ShuffleDefs();
				break;
			case Keys.D:
				WordBank.DumpBank(null);
				break;
			case Keys.L:
				WordBank.FillBank(null);
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
