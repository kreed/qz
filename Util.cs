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

	static class Util {
		public readonly static Random Random = new Random();

		public static T Next<T>(this IList<T> list)
		{
			var i = Random.Next(list.Count);
			var e = list[i];
			list.RemoveAt(i);
			return e;
		}
	}
}
