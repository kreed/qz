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

		public void Fill(List<Entry> bank, Stream stream)
		{
			string line;
			using (var sr = new StreamReader(stream))
				while ((line = sr.ReadLine()) != null) {
					var toks = line.Split('\t');
					if (toks.Length == 2)
						bank.Add(new Entry(toks[0], toks[1]));
				}
			stream.Close();
		}

		public void FillFromEmbed()
		{
			bank.Clear();
			Fill(bank, Assembly.GetExecutingAssembly()
			     .GetManifestResourceStream("words.txt"));
			NextGroup();
		}

		public bool Fill(string file)
		{
			var newBank = new List<Entry>();

			try {
				Fill(newBank, new FileStream(file, FileMode.Open));
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
}
