// Copyright 2012 Mike Caldwell (Casascius)
// Copyright (C) 2026 odolvlobo
// This file is part of Bitcoin Address Utility.

// Bitcoin Address Utility is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Bitcoin Address Utility is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with Bitcoin Address Utility.  If not, see http://www.gnu.org/licenses/.


using System.Windows;
using BtcAddress.Views;

namespace BtcAddress
{
    static class Program
    {
        public static MainWindow AddressUtility = null;

        public static Base58CalcWindow Base58Calc = null;

        public static MofNcalcWindow MofNcalc = null;

        public static PpecKeygenWindow IntermediateGen = null;

        public static KeyCombinerWindow KeyCombiner = null;

        public static DecryptKeyWindow DecryptKey = null;

        public static Bip38ConfValidatorWindow ConfValidator = null;

        public static EscrowToolsShellWindow EscrowTools = null;

        public static PaperWalletPrinterWindow PaperWalletPrinter = null;

        public static void ShowAddressUtility()
        {
            AddressUtility = showWindow<MainWindow>(AddressUtility, () => AddressUtility = null);
        }

        public static void ShowAddressUtility(Casascius.Bitcoin.KeyCollectionItem item)
        {
            ShowAddressUtility();
            if (AddressUtility == null || item == null)
            {
                return;
            }

            AddressUtility.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                AddressUtility.DisplayKeyCollectionItem(item);
                AddressUtility.Activate();
            }));
        }

        public static void ShowBase58Calc()
        {
            Base58Calc = showWindow<Base58CalcWindow>(Base58Calc, () => Base58Calc = null);
        }

        public static void ShowMofNcalc()
        {
            MofNcalc = showWindow<MofNcalcWindow>(MofNcalc, () => MofNcalc = null);
        }

        public static void ShowIntermediateGen()
        {
            IntermediateGen = showWindow<PpecKeygenWindow>(IntermediateGen, () => IntermediateGen = null);
        }

        public static void ShowKeyCombiner()
        {
            KeyCombiner = showWindow<KeyCombinerWindow>(KeyCombiner, () => KeyCombiner = null);
        }

        public static void ShowConfValidator()
        {
            ConfValidator = showWindow<Bip38ConfValidatorWindow>(ConfValidator, () => ConfValidator = null);
        }

        public static void ShowKeyDecrypter()
        {
            DecryptKey = showWindow<DecryptKeyWindow>(DecryptKey, () => DecryptKey = null);
        }

        public static void ShowEscrowTools()
        {
            EscrowTools = showWindow<EscrowToolsShellWindow>(EscrowTools, () => EscrowTools = null);
        }

        public static void ShowPaperWalletPrinter()
        {
            PaperWalletPrinter = showWindow<PaperWalletPrinterWindow>(PaperWalletPrinter, () => PaperWalletPrinter = null);
        }

        private static T showWindow<T>(T currentWindow, System.Action onClosed) where T : Window, new()
        {
            if (currentWindow == null || !currentWindow.IsVisible)
            {
                T rv = new T();
                rv.Closed += (_, __) => onClosed?.Invoke();
                rv.Show();
                return rv;
            }
            else
            {
                currentWindow.Activate();
                return currentWindow;
            }
        }
    }
}
