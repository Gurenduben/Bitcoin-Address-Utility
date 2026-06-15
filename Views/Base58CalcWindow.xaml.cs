using System.Windows;
using Casascius.Bitcoin;

namespace BtcAddress.Views
{
    public partial class Base58CalcWindow : Window
    {
        public Base58CalcWindow()
        {
            InitializeComponent();
        }

        private void TxtHex_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!txtHex.IsKeyboardFocusWithin)
            {
                return;
            }

            byte[] bytes = Util.HexStringToBytes(txtHex.Text);
            if (useChecksumMenuItem.IsChecked)
            {
                txtBase58.Text = Util.ByteArrayToBase58Check(bytes);
            }
            else
            {
                txtBase58.Text = Base58.FromByteArray(bytes);
            }

            UpdateByteCounts();
        }

        private void TxtBase58_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!txtBase58.IsKeyboardFocusWithin)
            {
                return;
            }

            byte[] bytes;
            if (useChecksumMenuItem.IsChecked)
            {
                bytes = Util.Base58CheckToByteArray(txtBase58.Text);
            }
            else
            {
                bytes = Base58.ToByteArray(txtBase58.Text);
            }

            string hex = "invalid";
            if (bytes != null)
            {
                hex = Util.ByteArrayToString(bytes);
            }

            txtHex.Text = hex;
            UpdateByteCounts();
        }

        private void UpdateByteCounts()
        {
            lblByteCounts.Text = "Bytes: " + Util.HexStringToBytes(txtHex.Text).Length + "  Base58 length: " + txtBase58.Text.Length;
        }

        private void UseChecksumMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (txtBase58.IsKeyboardFocusWithin)
            {
                TxtBase58_TextChanged(txtBase58, null);
            }
            else if (txtHex.IsKeyboardFocusWithin)
            {
                TxtHex_TextChanged(txtHex, null);
            }
        }
    }
}
