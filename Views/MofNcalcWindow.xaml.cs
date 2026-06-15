using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Casascius.Bitcoin;

namespace BtcAddress.Views
{
    public partial class MofNcalcWindow : Window
    {
        private byte[] targetPrivKey = null;

        public MofNcalcWindow()
        {
            InitializeComponent();
        }

        private TextBox GetPartBox(int i)
        {
            TextBox[] parts = new TextBox[] { txtPart1, txtPart2, txtPart3, txtPart4, txtPart5, txtPart6, txtPart7, txtPart8 };
            return parts[i];
        }

        private int GetComboValue(ComboBox combo)
        {
            if (combo.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out int value))
            {
                return value;
            }
            return 1;
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            int partsNeeded = GetComboValue(numPartsNeeded);
            int partsToGenerate = GetComboValue(numPartsToGenerate);

            if (partsNeeded > partsToGenerate)
            {
                MessageBox.Show("Number of parts needed exceeds number of parts to generate.");
                return;
            }

            for (int i = 0; i < 8; i++)
            {
                TextBox t = GetPartBox(i);
                t.Text = "";
                t.Background = Brushes.White;
            }

            MofN mn = new MofN();

            if (targetPrivKey == null)
            {
                mn.Generate(partsNeeded, partsToGenerate);
            }
            else
            {
                mn.Generate(partsNeeded, partsToGenerate, targetPrivKey);
            }

            int j = 0;
            foreach (string kp in mn.GetKeyParts())
            {
                GetPartBox(j++).Text = kp;
            }

            txtPrivKey.Text = mn.BitcoinPrivateKey ?? "?";
            txtAddress.Text = mn.BitcoinAddress ?? "?";
        }

        private void BtnDecode_Click(object sender, RoutedEventArgs e)
        {
            MofN mn = new MofN();

            for (int i = 0; i < 8; i++)
            {
                TextBox t = GetPartBox(i);
                string p = t.Text.Trim();

                if (p == "" || (mn.PartsAccepted >= mn.PartsNeeded && mn.PartsNeeded > 0))
                {
                    t.Background = Brushes.White;
                }
                else
                {
                    string result = mn.AddKeyPart(p);
                    if (result == null)
                    {
                        t.Background = Brushes.LightGreen;
                    }
                    else
                    {
                        t.Background = Brushes.Pink;
                    }
                }
            }

            if (mn.PartsAccepted >= mn.PartsNeeded && mn.PartsNeeded > 0)
            {
                mn.Decode();
                txtPrivKey.Text = mn.BitcoinPrivateKey;
                txtAddress.Text = mn.BitcoinAddress;
            }
            else
            {
                MessageBox.Show("Not enough valid parts were present to decode an address.");
            }
        }

        private void BtnGenerateSpecific_Click(object sender, RoutedEventArgs e)
        {
            KeyPair k = null;

            try
            {
                k = new KeyPair(txtPrivKey.Text);
                targetPrivKey = k.PrivateKeyBytes;
            }
            catch (Exception)
            {
                MessageBox.Show("Not a valid private key.");
            }

            BtnGenerate_Click(sender, e);
            targetPrivKey = null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This feature is experimental, a proof of concept, and the key format will probably be revised heavily before this ever makes it into production.  Don't rely on it to secure large numbers of Bitcoins.  If you use it, make sure you keep a copy of this version of the utility in case the m-of-n format is changed before being accepted as any kind of standard.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
