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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Resources;
using System.Windows.Forms;

using Entry = System.Collections.Generic.KeyValuePair<string, string>;

namespace Qz {
	// note: this modifies the list, so it's not really an enumerator (I guess)
	class RandomEnumerator<T> : IEnumerator<T> {
		List<T> list;
		T e;

		public RandomEnumerator(IList<T> list)
		{
			this.list = new List<T>(list);
		}

		public T Current
		{
			get {
				return e;
			}
		}

		object IEnumerator.Current
		{
			get {
				return e;
			}
		}

		public bool MoveNext()
		{
			if (list.Count == 0)
				return false;
			e = list.Next();
			return true;
		}

		public void Reset()
		{
		}

		public void Dispose()
		{
		}
	}

	class Bank {
		private List<Entry> bank = new List<Entry>();

		public List<Word> Words = new List<Word>();
		public List<Meaning> Meanings = new List<Meaning>();

		public int Remaining;

		public int GroupSize = 16;
		public bool OrderWords = true;
		public bool OrderMeanings;

		public bool Finished
		{
			get {
				return bank.Count == 0 && Words.Count == 0;
			}
		}

		public string BankStateFile
		{
			get {
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Qz.state");
			}
		}

		public void Init()
		{
			if (!File.Exists(BankStateFile) || !Fill(BankStateFile)) {
				using (var rr = new ResourceReader("words.resources"))
					foreach (System.Collections.DictionaryEntry word in rr)
						bank.Add(new Entry((string)word.Key, (string)word.Value));
				NextGroup();
			}

			Application.ApplicationExit += OnExit;
		}

		public void NoSave()
		{
			Application.ApplicationExit -= OnExit;
		}

		public void OnExit(object o, EventArgs e)
		{
			if (Remaining == 0 && File.Exists(BankStateFile))
				File.Delete(BankStateFile);
			else
				Dump(BankStateFile);
		}

		// A few sanity checks here would probably be wise here. Currently,
		// we don't actually parse the file until we display the words. We
		// should parse it here or at least make sure it's parsable.
		public bool Fill(string file)
		{
			var newBank = new List<Entry>();

			try {
				string line;
				using (var sr = new StreamReader(file))
					while ((line = sr.ReadLine()) != null) {
						var toks = line.Split('\t');
						if (toks.Length == 2)
							newBank.Add(new Entry(toks[0], toks[1]));
					}
			} catch (Exception e) {
				MessageBox.Show("Error reading word bank: " + e.Message);
				return false;
			}

			bank = newBank;
			NextGroup();
			return true;
		}

		public bool Dump(string file)
		{
			try {
				using (var sw = new System.IO.StreamWriter(file)) {
					foreach (var line in bank)
						sw.WriteLine(line.Key + '\t' + line.Value);
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
			var word = bank.Next();
			var meaning = new Meaning(word.Value, g, Meanings);
			Words.Add(new Word(word.Key, meaning, g));
			Meanings.Add(meaning);
		}

		public bool Check()
		{
			int wrong = Words.TestWrong();
			Remaining = wrong + bank.Count;
			Program.Instance.UpdateCount(Words.Count - wrong, Remaining);
			return wrong == 0;
		}

		public void Reload()
		{
			int edge = Words.CalcRightEdge();
			Words.Layout(OrderWords, edge + 5);
			Meanings.Layout(OrderMeanings, edge + 10);
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
				if (!word.Correct)
					bank.Add(new Entry(word.Text, word.Meaning.Text));
				word.Meaning.Remove();
				Words.Remove(word);
				Meanings.Remove(word.Meaning);
				GroupSize = Words.Count;
				Reload();
			}
		}

		public void NextGroup()
		{
			Words.Clear();
			Meanings.Clear();

			if (bank.Count != 0) {
				using (var g = Program.Instance.CreateGraphics())
					while (Words.Count != GroupSize && bank.Count != 0)
						Next(g);
				Reload();
			}
		}
	}

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

	static class WordCollection {
		public static int TestWrong(this List<Word> current)
		{
			// It would be better to use Count(), but Mono (as of 1.9.1)
			// ignores it because it has the same name as a property. . .
			return (int)current.LongCount(word => !word.TestCorrect());
		}
	}

	class Word : Tile {
		public readonly Meaning Meaning;

		public override int X
		{
			set {
				Rect.X = value - Rect.Width;
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

	class Meaning : Tile {
		public Meaning Next;
		public bool Moved;

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
			if (Moved && word.Rect.Y - 15 < Rect.Y
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

	static class Util {
		public readonly static Random Random = new Random();

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

		public static T TakeAt<T>(this IList<T> list, int i)
		{
			var e = list[i];
			list.RemoveAt(i);
			return e;
		}

		public static T Next<T>(this IList<T> list)
		{
			return list.TakeAt(Random.Next(list.Count));
		}
	}
}
