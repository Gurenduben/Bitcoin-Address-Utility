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
using Casascius.Bitcoin;
using Xunit;

namespace BtcAddress.UnitTests
{
    // AddressBase(string) constructor hardening added in the WPF migration:
    // null/empty/whitespace rejection, Trim() of surrounding whitespace, and
    // null-return handling from Util.Base58CheckToByteArray (bad checksum / too short).
    public class AddressBaseStringTests
    {
        // Bitcoin genesis block coinbase address (mainnet, type byte 0).
        const string Genesis = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";

        [Fact]
        public void Valid_MainnetAddress_RoundTrips()
        {
            var a = new AddressBase(Genesis);
            Assert.Equal(0, a.AddressType);
            Assert.Equal(Genesis, a.AddressBase58);
        }

        [Fact]
        public void Valid_AllZeroAddress_Parses()
        {
            // 21 zero bytes encoded as Base58Check (see UtilTests).
            var a = new AddressBase("1111111111111111111114oLvT2");
            Assert.Equal(0, a.AddressType);
            Assert.Equal(new byte[20], a.Hash160);
        }

        [Fact]
        public void SurroundingWhitespace_IsTrimmed()
        {
            var trimmed = new AddressBase(Genesis);
            var padded = new AddressBase("   " + Genesis + "\t\r\n ");
            Assert.Equal(trimmed.AddressBase58, padded.AddressBase58);
        }

        [Fact]
        public void Null_Throws()
        {
            Assert.Throws<ArgumentException>(() => new AddressBase((string)null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\r\n")]
        public void EmptyOrWhitespace_Throws(string s)
        {
            Assert.Throws<ArgumentException>(() => new AddressBase(s));
        }

        [Fact]
        public void BadChecksum_Throws()
        {
            // Last char mutated -> checksum mismatch -> Base58CheckToByteArray returns null.
            string bad = Genesis.Substring(0, Genesis.Length - 1) + "b";
            Assert.Throws<ArgumentException>(() => new AddressBase(bad));
        }

        [Fact]
        public void NotBase58_Throws()
        {
            // '0', 'O', 'I', 'l' are not in the Base58 alphabet.
            Assert.Throws<ArgumentException>(() => new AddressBase("0OIl0OIl"));
        }

        [Fact]
        public void TooShort_Throws()
        {
            // Decodes to fewer than 4 bytes -> null -> rejected.
            Assert.Throws<ArgumentException>(() => new AddressBase("1"));
        }

        [Fact]
        public void ValidChecksumWrongLength_Throws()
        {
            // Well-formed Base58Check payload but 20 bytes, not the required 21.
            string twentyBytes = Util.ByteArrayToBase58Check(new byte[20]);
            Assert.Throws<ArgumentException>(() => new AddressBase(twentyBytes));
        }
    }
}
