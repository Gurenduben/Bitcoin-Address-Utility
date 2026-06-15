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

namespace Casascius.Bitcoin
{
    /// <summary>
    /// Deterministic paper-wallet key generation, factored out of the UI so the
    /// generated count and the keys themselves can be tested without a live window.
    /// </summary>
    public static class PaperWalletGenerator
    {
        /// <summary>
        /// Creates the key for a single sequence position. The seed is
        /// passphrase + sequence, matching the original paper-wallet scheme.
        /// </summary>
        public static KeyPair CreateKey(string passphrase, int sequence, bool miniKeys)
        {
            string seed = passphrase + sequence.ToString();
            return miniKeys
                ? MiniKeyPair.CreateDeterministic(seed)
                : new KeyPair(Util.ComputeSha256(seed));
        }

        /// <summary>
        /// Generates key pairs for sequences 1..count. Mirrors the timer loop in
        /// PaperWalletPrinterWindow so testing this validates that path's count.
        /// </summary>
        public static List<KeyCollectionItem> Generate(string passphrase, int count, bool miniKeys)
        {
            var result = new List<KeyCollectionItem>(count > 0 ? count : 0);
            for (int sequence = 1; sequence <= count; sequence++)
            {
                result.Add(new KeyCollectionItem(CreateKey(passphrase, sequence, miniKeys)));
            }
            return result;
        }
    }
}
