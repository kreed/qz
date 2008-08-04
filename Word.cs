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
}
