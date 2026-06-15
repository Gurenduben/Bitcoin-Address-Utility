using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Casascius.Bitcoin;
using Org.BouncyCastle.Security;
using Drawing = System.Drawing.Printing;

namespace BtcAddress.Views
{
    public partial class PaperWalletPrinterWindow : Window
    {
        protected int CurrentSequence;
        protected string CurrentPassphrase;
        protected bool CurrentlyGenerating = false;
        protected int TotalToGenerate = 0;
        protected List<KeyCollectionItem> Addresses = new List<KeyCollectionItem>();

        protected bool CurrentSelectionPrinted = false;
        protected bool CurrentSelectionSaved = false;

        private readonly DispatcherTimer timer1;

        public PaperWalletPrinterWindow()
        {
            InitializeComponent();
            timer1 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            timer1.Tick += Timer1_Tick;
        }

        private void RdoDeterministicWallet_Checked(object sender, RoutedEventArgs e)
        {
            if (lblInputDescription == null || txtPassphrase == null || rdoDeterministicWallet == null)
            {
                return;
            }

            if (rdoDeterministicWallet.IsChecked == true)
            {
                lblInputDescription.Text = "Passphrase";
                txtPassphrase.Text = GetUglyRandomString();
            }
        }

        private void RdoRandomWallet_Checked(object sender, RoutedEventArgs e)
        {
            if (lblInputDescription == null || rdoRandomWallet == null)
            {
                return;
            }

            if (rdoRandomWallet.IsChecked == true)
            {
                lblInputDescription.Text = "Enter some random text with your keyboard to add entropy.";
            }
        }

        private string GetUglyRandomString()
        {
            StringBuilder sb = new StringBuilder(128);
            for (int i = 0; i < 64; i++)
            {
                SecureRandom sr = new SecureRandom();
                int idx = sr.Next(0, 61);
                sb.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".Substring(idx, 1));
            }
            return sb.ToString();
        }

        private void LockButtons(bool locked)
        {
            btnSortKeys.IsEnabled = !locked;
            btnPrintWallet.IsEnabled = !locked;
        }

        private void BtnGenerateAddresses_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentlyGenerating == false)
            {
                if (rdoRandomWallet.IsChecked == true)
                {
                    if (txtPassphrase.Text.Length < 30)
                    {
                        MessageBox.Show(this, "Please provide some random characters.  Just hit different keys on the keyboard until the box is full. This adds security to your paper wallet.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }

                if (!int.TryParse(txtGenCount.Text, out int requestedCount) || requestedCount < 1)
                {
                    MessageBox.Show(this, "Enter the number of addresses to create.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                if (Addresses.Count > 0 && CurrentSelectionSaved == false && rdoDeterministicWallet.IsChecked != true)
                {
                    string msg = "You have generated " + Addresses.Count + " addresses, which will be discarded if you continue.  Continue?";
                    if (Addresses.Count == 1) msg = msg.Replace("addresses", "address");

                    if (MessageBox.Show(this, msg, "Continue with generation?", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
                }

                if (rdoDeterministicWallet.IsChecked == true && txtPassphrase.Text == CurrentPassphrase)
                {
                    string msg = "You have not changed the passphrase since the last time you generated addresses, so you will be generating the same addresses as last time.  Continue?";
                    if (MessageBox.Show(this, msg, "Continue with generation?", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
                }
                else
                {
                    CurrentSelectionSaved = false;
                    CurrentSelectionPrinted = false;
                }

                Addresses = new List<KeyCollectionItem>();
                lblGenCount.Text = Addresses.Count.ToString() + " addresses have been generated.";

                TotalToGenerate = requestedCount;
                CurrentSequence = 1;

                if (rdoRandomWallet.IsChecked == true)
                {
                    CurrentPassphrase = txtPassphrase.Text + GetUglyRandomString();
                }
                else
                {
                    if (txtPassphrase.Text.Length < 30)
                    {
                        if (MessageBox.Show(this, "Passphrases must be highly unique and very long to be secure against hackers, who try trillions of random passwords in hopes of finding coins to steal.  Use a Random Wallet if you are not 100% sure about what you're doing.  Continue?", "", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Cancel) return;
                    }

                    CurrentPassphrase = txtPassphrase.Text;
                }

                CurrentlyGenerating = true;
                LockButtons(true);
                btnGenerateAddresses.Content = "Stop generating";
                timer1.Start();
            }
            else
            {
                CurrentlyGenerating = false;
                LockButtons(false);
                timer1.Stop();
                btnGenerateAddresses.Content = "Generate addresses";
            }
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (CurrentlyGenerating == false) return;
            if (CurrentSequence >= TotalToGenerate)
            {
                CurrentlyGenerating = false;
                LockButtons(false);
                timer1.Stop();
                btnGenerateAddresses.Content = "Generate addresses";
            }

            string myhash = CurrentPassphrase + ((int)CurrentSequence).ToString();

            KeyPair k;
            if (chkMiniKeys.IsChecked == true)
            {
                k = MiniKeyPair.CreateDeterministic(myhash);
                Addresses.Add(new KeyCollectionItem(k));
            }
            else
            {
                byte[] mykey = Util.ComputeSha256(myhash);
            }

            lblGenCount.Text = Addresses.Count.ToString() + " addresses have been generated.";
            CurrentSequence++;
        }

        private void BtnPrintWallet_Click(object sender, RoutedEventArgs e)
        {
            if (Addresses.Count == 0)
            {
                MessageBox.Show(this, "Please generate some addresses before trying to print.");
                return;
            }

            if (CurrentSelectionPrinted)
            {
                string msg = "You have already printed these addresses before.  Print again?";
                if (MessageBox.Show(this, msg, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            }

            PrintDialog pd = new PrintDialog();
            bool? dr = pd.ShowDialog();

            if (dr == true)
            {
                QRPrint printer = new QRPrint();
                if (this.rdoWalletPrivQR.IsChecked == true) printer.PrintMode = QRPrint.PrintModes.PrivQR;
                if (this.rdoWalletPubPrivQR.IsChecked == true) printer.PrintMode = QRPrint.PrintModes.PubPrivQR;
                printer.keys = new List<KeyCollectionItem>(Addresses.Count);
                foreach (KeyCollectionItem a in Addresses) printer.keys.Add(a);
                printer.PrinterSettings = pd.PrintQueue != null ? new Drawing.PrinterSettings { PrinterName = pd.PrintQueue.Name } : new Drawing.PrinterSettings();
                CurrentSelectionPrinted = true;
                printer.Print();
            }
        }
    }
}

