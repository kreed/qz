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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Qz {
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
}
