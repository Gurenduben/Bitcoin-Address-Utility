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

using System;
using System.Collections.Generic;
using System.Text;
using Casascius.Bitcoin;
using Xunit;

namespace BtcAddress.UnitTests
{
    // ByteArrayToString is still proven byte-for-byte identical to the original (it is
    // display-only and unchanged in semantics). HexStringToBytes was deliberately tightened:
    // each delimiter-separated run is now a BIG-ENDIAN byte string, so an odd digit count
    // gets a leading zero nibble ("ABC" -> {0x0A, 0xBC}) instead of the legacy trailing
    // low-byte ("ABC" -> {0xAB, 0x0C}). The reference oracle below encodes the NEW semantics.
    public class RefactorEquivalenceTests
    {
        // ---- ByteArrayToString original implementation, kept verbatim as the oracle ----

        private static string OldByteArrayToString(byte[] ba, int offset, int count)
        {
            string rv = "";
            int usedcount = 0;
            for (int i = offset; usedcount < count; i++, usedcount++)
            {
                rv += String.Format("{0:X2}", ba[i]) + " ";
            }
            return rv;
        }

        // ---- corrected big-endian reference oracle for HexStringToBytes (NEW semantics) ----

        private static byte[] RefHexStringToBytes(string source, bool testingForValidHex = false)
        {
            List<byte> bytes = new List<byte>();
            StringBuilder run = new StringBuilder();

            void Flush()
            {
                if (run.Length == 0) return;
                string s = run.ToString();
                if ((s.Length & 1) == 1) s = "0" + s; // big-endian: pad high nibble
                for (int i = 0; i < s.Length; i += 2)
                {
                    bytes.Add((byte)((HexVal(s[i]) << 4) | HexVal(s[i + 1])));
                }
                run.Clear();
            }

            foreach (char c in source)
            {
                if (c == ' ' || c == '-' || c == ':')
                {
                    Flush();
                }
                else if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
                {
                    run.Append(c);
                }
                else if (testingForValidHex)
                {
                    return null;
                }
            }
            Flush();
            return bytes.ToArray();
        }

        private static int HexVal(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return c - 'A' + 10;
        }

        // ---- ByteArrayToString ----

        [Fact]
        public void ByteArrayToString_Empty_ReturnsEmpty()
        {
            Assert.Equal("", Util.ByteArrayToString(Array.Empty<byte>()));
        }

        [Fact]
        public void ByteArrayToString_AllByteValues_MatchOracle()
        {
            byte[] all = new byte[256];
            for (int i = 0; i < 256; i++) all[i] = (byte)i;
            Assert.Equal(OldByteArrayToString(all, 0, all.Length), Util.ByteArrayToString(all));
        }

        [Fact]
        public void ByteArrayToString_Fuzz_MatchesOracle()
        {
            Random rng = new Random(1234567);
            for (int iter = 0; iter < 20000; iter++)
            {
                byte[] ba = new byte[rng.Next(0, 41)];
                rng.NextBytes(ba);
                int offset = ba.Length == 0 ? 0 : rng.Next(0, ba.Length);
                int count = ba.Length == 0 ? 0 : rng.Next(0, ba.Length - offset + 1);

                Assert.Equal(OldByteArrayToString(ba, offset, count), Util.ByteArrayToString(ba, offset, count));
            }
        }

        // ---- HexStringToBytes ----

        [Theory]
        // odd-length run is big-endian: a leading zero nibble is prepended to the run
        [InlineData("ABC", new byte[] { 0x0A, 0xBC })]          // 0x0ABC, not legacy {0xAB,0x0C}
        [InlineData("1", new byte[] { 0x01 })]                  // single nibble -> 0x01
        [InlineData("ABCDE", new byte[] { 0x0A, 0xBC, 0xDE })]  // 0x0ABCDE
        [InlineData("123", new byte[] { 0x01, 0x23 })]          // 0x0123
        // delimiters still force a byte boundary; each run is its own big-endian value
        [InlineData("1-A2", new byte[] { 0x01, 0xA2 })]         // runs "1","A2" -> {0x01},{0xA2}
        [InlineData("1 2 3", new byte[] { 0x01, 0x02, 0x03 })]  // single-nibble runs preserved
        [InlineData("AB-C", new byte[] { 0xAB, 0x0C })]         // runs "AB","C" -> {0xAB},{0x0C}
        [InlineData("ABC-DE", new byte[] { 0x0A, 0xBC, 0xDE })] // odd run "ABC" -> {0x0A,0xBC}, "DE"
        [InlineData("", new byte[] { })]
        [InlineData("   ", new byte[] { })]
        [InlineData("de:ad:be:ef", new byte[] { 0xDE, 0xAD, 0xBE, 0xEF })]
        public void HexStringToBytes_KnownCases(string src, byte[] expected)
        {
            Assert.Equal(expected, Util.HexStringToBytes(src));
        }

        [Fact]
        public void HexStringToBytes_OddLengthRun_IsBigEndian()
        {
            // The whole odd-length string read as one big-endian integer.
            byte[] actual = Util.HexStringToBytes("ABCDE");
            Assert.Equal(0x0ABCDE, (actual[0] << 16) | (actual[1] << 8) | actual[2]);
        }

        [Fact]
        public void HexStringToBytes_DelimiterStrippingIsBoundaryPreserving()
        {
            // Same digits, different delimiters -> same bytes when each run is even.
            byte[] a = Util.HexStringToBytes("AB CD");
            byte[] b = Util.HexStringToBytes("AB-CD");
            byte[] c = Util.HexStringToBytes("AB:CD");
            byte[] d = Util.HexStringToBytes("ABCD");
            Assert.Equal(new byte[] { 0xAB, 0xCD }, a);
            Assert.Equal(a, b);
            Assert.Equal(a, c);
            Assert.Equal(a, d);
        }

        // ---- GetHexBytes: short-input front-padding and leading-zero clip (big-endian) ----

        [Theory]
        // a few bytes short -> front-padded (value right-aligned, big-endian)
        [InlineData("0102", 4, new byte[] { 0x00, 0x00, 0x01, 0x02 })]
        // odd + short -> big-endian run THEN front-pad: "ABC"->{0x0A,0xBC}-> right-aligned
        [InlineData("ABC", 4, new byte[] { 0x00, 0x00, 0x0A, 0xBC })]
        // exact length passes through untouched
        [InlineData("01020304", 4, new byte[] { 0x01, 0x02, 0x03, 0x04 })]
        // one overhanging leading zero byte is clipped from the front
        [InlineData("00010203", 3, new byte[] { 0x01, 0x02, 0x03 })]
        public void GetHexBytes_ShortAndOdd_BigEndian(string src, int minimum, byte[] expected)
        {
            Assert.Equal(expected, Util.GetHexBytes(src, minimum));
        }

        [Fact]
        public void HexStringToBytes_InvalidCharNotTesting_SkipsButKeepsPairing()
        {
            // '.' is ignored and does NOT break a nibble pair when not validating.
            Assert.Equal(new byte[] { 0xAB }, Util.HexStringToBytes("A.B"));
        }

        [Fact]
        public void HexStringToBytes_InvalidCharTesting_ReturnsNull()
        {
            Assert.Null(Util.HexStringToBytes("A.B", testingForValidHex: true));
        }

        [Fact]
        public void HexStringToBytes_Fuzz_MatchesBigEndianOracle()
        {
            // Alphabet biased toward hex digits, delimiters, and a few invalid chars.
            char[] alphabet = "0123456789abcdefABCDEF -:gGzZ.\t\n".ToCharArray();
            Random rng = new Random(7654321);

            for (int iter = 0; iter < 50000; iter++)
            {
                int len = rng.Next(0, 25);
                StringBuilder sb = new StringBuilder(len);
                for (int i = 0; i < len; i++) sb.Append(alphabet[rng.Next(alphabet.Length)]);
                string src = sb.ToString();

                foreach (bool testing in new[] { false, true })
                {
                    byte[] expected = RefHexStringToBytes(src, testing);
                    byte[] actual = Util.HexStringToBytes(src, testing);
                    Assert.Equal(expected, actual); // Assert.Equal handles null == null
                }
            }
        }

        // ---- round trip across both refactored functions ----

        [Fact]
        public void HexRoundTrip_Fuzz()
        {
            Random rng = new Random(424242);
            for (int iter = 0; iter < 5000; iter++)
            {
                byte[] ba = new byte[rng.Next(0, 41)];
                rng.NextBytes(ba);
                string hex = Util.ByteArrayToString(ba);
                Assert.Equal(ba, Util.HexStringToBytes(hex));
            }
        }
    }
}
