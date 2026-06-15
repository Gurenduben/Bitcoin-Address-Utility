// Bitcoin Address Utility
// Copyright (C) 2012 Mike Caldwell
// Copyright (C) 2026 odolvlobo
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using PC;
using Xunit;

namespace BtcAddress.UnitTests
{
    // RemoveZeros bounds-safety added in the WPF migration: previously
    // indexed _text[value] unconditionally, throwing IndexOutOfRangeException
    // for value < 0 or value >= length. Now clamps negatives and stops at end.
    public class PCPrintTests
    {
        static int Remove(string text, int start)
        {
            var doc = new PCPrint(text);
            int v = start;
            return doc.RemoveZeros(ref v);
        }

        [Fact]
        public void NoLeadingZeros_ReturnsUnchanged()
        {
            Assert.Equal(3, Remove("abcdef", 3));
        }

        [Fact]
        public void LeadingNulls_SkippedToFirstNonNull()
        {
            Assert.Equal(3, Remove("\0\0\0abc", 0));
        }

        [Fact]
        public void NegativeStart_ClampedToZeroThenSkipsNulls()
        {
            Assert.Equal(2, Remove("\0\0abc", -5));
        }

        [Fact]
        public void AllNulls_StopsAtLength()
        {
            Assert.Equal(4, Remove("\0\0\0\0", 0));
        }

        [Fact]
        public void StartAtLength_ReturnsLengthNoThrow()
        {
            Assert.Equal(6, Remove("abcdef", 6));
        }

        [Fact]
        public void StartPastLength_ReturnsValueNoThrow()
        {
            Assert.Equal(99, Remove("abcdef", 99));
        }

        [Fact]
        public void EmptyText_ReturnsZero()
        {
            Assert.Equal(0, Remove("", 0));
        }
    }
}
