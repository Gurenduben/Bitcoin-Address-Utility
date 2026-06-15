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

using System.Collections.Generic;
using System.Linq;
using Casascius.Bitcoin;
using Xunit;

namespace BtcAddress.UnitTests
{
    // PaperWalletGenerator: the UI-free generation path used by PaperWalletPrinterWindow.
    public class PaperWalletGeneratorTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(50)]
        public void Generate_NormalMode_ProducesRequestedCount(int count)
        {
            var keys = PaperWalletGenerator.Generate("correct horse battery staple", count, miniKeys: false);
            Assert.Equal(count, keys.Count);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public void Generate_MiniKeyMode_ProducesRequestedCount(int count)
        {
            var keys = PaperWalletGenerator.Generate("correct horse battery staple", count, miniKeys: true);
            Assert.Equal(count, keys.Count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Generate_NonPositiveCount_ProducesEmptyList(int count)
        {
            var keys = PaperWalletGenerator.Generate("passphrase", count, miniKeys: false);
            Assert.Empty(keys);
        }

        [Fact]
        public void Generate_NormalMode_PopulatesEveryItem()
        {
            var keys = PaperWalletGenerator.Generate("passphrase", 5, miniKeys: false);
            Assert.All(keys, k => Assert.NotNull(k.Address));
        }

        [Fact]
        public void Generate_NormalMode_AllAddressesDistinct()
        {
            var addresses = PaperWalletGenerator
                .Generate("passphrase", 25, miniKeys: false)
                .Select(k => k.GetAddressBase58())
                .ToList();

            Assert.Equal(addresses.Count, addresses.Distinct().Count());
        }

        [Fact]
        public void Generate_IsDeterministic_ForSamePassphrase()
        {
            var a = PaperWalletGenerator.Generate("same passphrase", 5, miniKeys: false);
            var b = PaperWalletGenerator.Generate("same passphrase", 5, miniKeys: false);

            Assert.Equal(
                a.Select(k => k.GetAddressBase58()),
                b.Select(k => k.GetAddressBase58()));
        }

        [Fact]
        public void CreateKey_NormalMode_MatchesGenerateSequence()
        {
            // The timer ticks CreateKey for sequences 1..N; Generate must agree.
            var generated = PaperWalletGenerator.Generate("passphrase", 3, miniKeys: false);
            var byHand = new List<string>
            {
                new KeyCollectionItem(PaperWalletGenerator.CreateKey("passphrase", 1, false)).GetAddressBase58(),
                new KeyCollectionItem(PaperWalletGenerator.CreateKey("passphrase", 2, false)).GetAddressBase58(),
                new KeyCollectionItem(PaperWalletGenerator.CreateKey("passphrase", 3, false)).GetAddressBase58(),
            };

            Assert.Equal(byHand, generated.Select(k => k.GetAddressBase58()));
        }
    }
}
