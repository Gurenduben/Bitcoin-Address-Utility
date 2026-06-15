using System;
using System.Windows;
using Casascius.Bitcoin;

namespace BtcAddress.Views
{
    public partial class PpecKeygenWindow : Window
    {
        public PpecKeygenWindow()
        {
            InitializeComponent();
        }

        private void BtnEncode_Click(object sender, RoutedEventArgs e)
        {
            if ((txtPassphrase.Text ?? "") == "")
            {
                MessageBox.Show("Enter a passphrase first.");
                return;
            }

            try
            {
                Bip38Intermediate intermediate = new Bip38Intermediate(txtPassphrase.Text, Bip38Intermediate.Interpretation.Passphrase);
                txtPassphraseCode.Text = intermediate.Code;
            }
            catch (Exception ae)
            {
                MessageBox.Show(ae.Message);
            }
        }
    }
}
