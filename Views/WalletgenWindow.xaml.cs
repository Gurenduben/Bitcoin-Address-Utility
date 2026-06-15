using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Casascius.Bitcoin;
using PC;
using Drawing = System.Drawing;
using DrawingPrint = System.Drawing.Printing;

namespace BtcAddress.Views
{
    public partial class WalletgenWindow : Window
    {
        private int GenerationFormula = 1;

        public WalletgenWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cboOutputType.SelectedIndex = 0;

            const int phraselength = 80;
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] byte8 = new byte[phraselength];
            rng.GetBytes(byte8);
            string randomphrase = "";
            string junk64 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz*#_%+!";
            for (int i = 0; i < phraselength; i++)
            {
                randomphrase += junk64.Substring(byte8[i] & 63, 1);
            }
            txtPassphrase.Text = randomphrase;
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (!btnGenerate.IsEnabled)
            {
                return;
            }

            int n = 0;
            if (int.TryParse(txtAddressCount.Text, out n) == false) n = 0;
            if (n < 1 || n > 9999)
            {
                MessageBox.Show("Please enter a number of addresses between 1 and 9999", "Invalid entry");
                return;
            }

            if (txtPassphrase.Text.Length < 20)
            {
                if (MessageBox.Show("Your passphrase is too short (< 20 characters). If you generate this wallet it may be easily compromised. Are you sure you'd like to use this passphrase?", "Passphrase too short", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    return;
                }
            }

            if (Util.PassphraseTooSimple(txtPassphrase.Text))
            {
                if (MessageBox.Show("Your passphrase is too simple. If you generate this wallet it may be easily compromised. Are you sure you'd like to use this passphrase?", "Passphrase too simple", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    return;
                }
            }

            StringBuilder wallet = new StringBuilder();

            string outputType = (cboOutputType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Normal";
            bool CSVmode = outputType.Contains("CSV");
            bool ScriptMode = outputType.Contains("Import script");
            bool ShowHelpText = outputType.Contains("Normal");

            if (ShowHelpText)
            {
                wallet.AppendLine("Paper Bitcoin Wallet.  Keep private, do not lose, do not allow anyone to make a copy.  Anyone with the passphrase or private keys can steal your funds.\r\n");
                wallet.AppendLine("Passphrase was:");
                wallet.AppendLine(txtPassphrase.Text);
                wallet.AppendLine("Freely give out the Bitcoin address.  The private key after each address is the key needed to unlock funds sent to the Bitcoin address.\r\n");
            }

            progressBar1.Maximum = n;
            progressBar1.Minimum = 0;
            progressBar1.Visibility = Visibility.Visible;
            btnGenerate.IsEnabled = false;
            txtPassphrase.Visibility = Visibility.Collapsed;

            for (int i = 1; i <= n; i++)
            {
                Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

                string privatestring;
                switch (GenerationFormula)
                {
                    case 1:
                        privatestring = txtPassphrase.Text + i.ToString();
                        break;
                    default:
                        privatestring = i.ToString() + "/" + txtPassphrase.Text + "/" + i.ToString() + "/BITCOIN";
                        break;
                }

                byte[] privatekey = Util.ComputeSha256(privatestring);
                KeyPair kp = new KeyPair(privatekey);

                string PrivWIF = kp.PrivateKeyBase58;
                string Address = kp.AddressBase58;

                if (CSVmode)
                {
                    wallet.AppendFormat("{0},\"{1}\",\"{2}\"\r\n", i, Address, PrivWIF);
                }
                else if (ScriptMode)
                {
                    wallet.AppendFormat("# {0}: {1}\"\r\n./bitcoind importprivkey {2}\r\n", i, Address, PrivWIF);
                }
                else
                {
                    wallet.AppendFormat("Bitcoin Address #{0}: {1}\r\n", i, Address);
                    wallet.AppendFormat("Private Key: {0}\r\n\r\n", PrivWIF);
                }

                progressBar1.Value = i;
            }

            txtWallet.Text = wallet.ToString();

            progressBar1.Value = 0;
            progressBar1.Visibility = Visibility.Collapsed;
            txtPassphrase.Visibility = Visibility.Visible;
            btnGenerate.IsEnabled = true;
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog pd = new PrintDialog();
            bool? dr = pd.ShowDialog();

            if (dr == true)
            {
                PCPrint printer = new PCPrint();
                printer.PrinterSettings.PrinterName = pd.PrintQueue?.Name ?? "";
                printer.PrinterFont = new Drawing.Font("Verdana", 10);
                printer.TextToPrint = txtWallet.Text;
                printer.Print();
            }
        }

        private void LblFormula_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2)
            {
                return;
            }

            GenerationFormula = GenerationFormula == 1 ? 0 : 1;
            if (GenerationFormula == 0)
            {
                lblFormula.Text = "Generation formula: PrivKey = SHA256(n + \"/\" + passphrase + \"/\" + n + \"/BITCOIN) where n = \"1\" thru \"10\" (double-click to toggle)";
            }
            else
            {
                lblFormula.Text = "Generation formula: PrivKey = SHA256(passphrase + n) (double-click to toggle)";
            }
        }
    }
}
