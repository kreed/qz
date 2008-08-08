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
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using Entry = System.Collections.Generic.KeyValuePair<string, string>;

namespace Qz {
	class Bank {
		private List<Entry> bank = new List<Entry>();

		public List<Word> Words = new List<Word>();
		public List<Meaning> Meanings = new List<Meaning>();

		public int Correct;
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
				var appData = Environment.SpecialFolder.ApplicationData;
				var appPath = Environment.GetFolderPath(appData);
				return Path.Combine(appPath, "Qz.state");
			}
		}

		public void Init()
		{
			if (!File.Exists(BankStateFile) || !Fill(BankStateFile))
				FillFromEmbed();

			Application.ApplicationExit += OnExit;
		}

		public void NoSave()
		{
			Application.ApplicationExit -= OnExit;
		}

		public void OnExit(object o, EventArgs e)
		{
			if (Remaining == 0) {
				if (File.Exists(BankStateFile))
					File.Delete(BankStateFile);
			} else
				Dump(BankStateFile);
		}

		public void Fill(Stream stream)
		{
			var newBank = new List<Entry>();
			string line;

			using (var sr = new StreamReader(stream))
				while ((line = sr.ReadLine()) != null) {
					var toks = line.Split('\t');
					if (toks.Length == 2)
						newBank.Add(new Entry(toks[0], toks[1]));
				}

			stream.Close();
			bank = newBank;
			Clear();
			Add();
		}

		public void FillFromEmbed()
		{
			Fill(Assembly.GetExecutingAssembly()
			     .GetManifestResourceStream("words.txt"));
		}

		public bool Fill(string file)
		{
			try {
				Fill(new FileStream(file, FileMode.Open));
			} catch (Exception e) {
				MessageBox.Show("Error reading word bank: " + e.Message);
				return false;
			}

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

		public bool Check()
		{
			int wrong = Words.TestWrong();
			Remaining = wrong + bank.Count;
			Correct = Words.Count - wrong;
			MainWindow.Instance.UpdateCount();
			return wrong == 0;
		}

		public void Reload()
		{
			int edge = Words.CalcRightEdge();
			Words.Layout(OrderWords ? LayoutMode.Order
			                        : LayoutMode.Randomize,
			             edge + 5);
			Meanings.Layout(OrderMeanings ? LayoutMode.Order
			                              : LayoutMode.Randomize,
			                edge + 10);
			Check();
		}

		private void RestoreRandom()
		{
			var word = Words.Next();
			if (!word.Correct)
				bank.Add(new Entry(word.Text, word.Meaning.Text));
			word.Meaning.Remove();
			Words.Remove(word);
			Meanings.Remove(word.Meaning);
			Reload();
		}

		public void Mod(int delta)
		{
			if (delta < 0 && Words.Count > 1) {
				GroupSize = Words.Count - 1;
				RestoreRandom();
			} else if (delta > 0 && bank.Count != 0) {
				++GroupSize;
				Add();
			}
		}

		public void Clear()
		{
			Words.Clear();
			Meanings.Clear();
		}

		public void Add()
		{
			if (bank.Count == 0)
				return;

			using (var g = MainWindow.Instance.CreateGraphics())
				while (Words.Count != GroupSize && bank.Count != 0) {
					var word = bank.Next();
					var meaning = new Meaning(word.Value, g, Meanings);
					Meanings.Add(meaning);
					Words.Add(new Word(word.Key, meaning, g));
				}

			Reload();
		}

		// This doesn't work with multiple instances of Bank, but that's not
		// much of a problem for us.
		public void AdjustFont(int delta)
		{
			var newSize = TileCollection.FontFace.Size + delta;
			TileCollection.FontFace =
				new Font(TileCollection.FontFace.FontFamily, newSize);
			TileCollection.LineHeight = Math.Abs((int)newSize) * 3;

			int edge = Words.CalcRightEdge();
			Words.Layout(LayoutMode.Align, edge + 5);
			Meanings.Layout(LayoutMode.Align, edge + 10);
		}
	}
}
