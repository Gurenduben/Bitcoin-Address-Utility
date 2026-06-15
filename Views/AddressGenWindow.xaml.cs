using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Casascius.Bitcoin;

namespace BtcAddress.Views
{
    public partial class AddressGenWindow : Window
    {
        private enum GenChoices
        {
            Minikey, WIF, Encrypted, Deterministic, TwoFactor
        }

        private GenChoices GenChoice;
        private bool Generating = false;
        private bool GeneratingEnded = false;
        private bool StopRequested = false;
        private bool PermissionToCloseWindow = false;
        private bool RetainPrivateKeys = false;
        private string UserText;
        private int RemainingToGenerate = 0;
        private Thread GenerationThread = null;

        public List<KeyCollectionItem> GeneratedItems = new List<KeyCollectionItem>();

        private Bip38Intermediate[] intermediatesForGeneration;
        private int intermediateIdx;
        private readonly DispatcherTimer timer1;

        public AddressGenWindow()
        {
            InitializeComponent();
            timer1 = new DispatcherTimer();
            timer1.Tick += Timer1_Tick;
            txtGenCount.Text = "8";
        }

        private void RdoWalletType_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (txtTextInput == null || lblTextInput == null || chkRetainPrivKey == null ||
                rdoDeterministicWallet == null || rdoEncrypted == null || rdoTwoFactor == null)
            {
                return;
            }

            txtTextInput.Text = "";
            txtTextInput.Visibility = (rdoDeterministicWallet.IsChecked == true || rdoEncrypted.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
            lblTextInput.Visibility = (rdoDeterministicWallet.IsChecked == true || rdoEncrypted.IsChecked == true || rdoTwoFactor.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;

            if (rdoDeterministicWallet.IsChecked == true)
            {
                lblTextInput.Text = "Seed for deterministic generation";
            }
            else if (rdoEncrypted.IsChecked == true)
            {
                lblTextInput.Text = "Encryption passphrase or Intermediate Code";
            }
            else if (rdoTwoFactor.IsChecked == true)
            {
                int icodect = ScanClipboardForIntermediateCodes().Count;
                lblTextInput.Text = icodect == 0
                    ? "Copy one or more intermediate codes to the clipboard."
                    : icodect + " intermediate codes found on clipboard.";
            }

            chkRetainPrivKey.Visibility = (rdoEncrypted.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (PermissionToCloseWindow) return;
            if (Generating)
            {
                if (MessageBox.Show("Cancel and abandon generation in progress?", "Abort generation", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    StopRequested = true;
                    if (GenerationThread != null && GenerationThread.ThreadState == ThreadState.Running)
                    {
                        GenerationThread.Join();
                        GeneratedItems.Clear();
                    }
                }
            }
        }

        private void BtnGenerateAddresses_Click(object sender, RoutedEventArgs e)
        {
            if (Generating)
            {
                StopRequested = true;
                btnGenerateAddresses.Content = "Stopping...";
                return;
            }

            if (rdoEncrypted.IsChecked == true && txtTextInput.Text == "")
            {
                MessageBox.Show("An encryption passphrase is required. Choose a different option if you don't want encrypted keys.", "Passphrase missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (rdoDeterministicWallet.IsChecked == true && txtTextInput.Text == "")
            {
                MessageBox.Show("A deterministic seed is required.  If you do not intend to create a deterministic wallet or know what one is used for, it is recommended you choose one of the other options.  An inappropriate seed can result in the unexpected theft of funds.", "Seed missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (rdoTwoFactor.IsChecked == true)
            {
                List<Bip38Intermediate> intermediates = ScanClipboardForIntermediateCodes();
                if (intermediates.Count == 0)
                {
                    MessageBox.Show("No valid intermediate codes were found on the clipboard.  Intermediate codes are typically sent to you from someone else desiring paper wallets, or from your mobile phone.  Copy the received intermediate codes to the clipboard, and try again.  Address Generator automatically detects valid intermediate codes and ignores everything else on the clipboard", "No intermediate codes found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                intermediatesForGeneration = intermediates.ToArray();
                intermediateIdx = 0;
            }
            else
            {
                intermediatesForGeneration = null;
            }

            if (!int.TryParse(txtGenCount.Text, out int requestedCount) || requestedCount < 1)
            {
                MessageBox.Show("Enter a valid number of addresses to generate.", "Invalid count", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GenerationThread = new Thread(new ThreadStart(GenerationThreadProcess));
            RemainingToGenerate = requestedCount;
            UserText = txtTextInput.Text;
            RetainPrivateKeys = chkRetainPrivKey.IsChecked == true;

            if (rdoDeterministicWallet.IsChecked == true) GenChoice = GenChoices.Deterministic;
            if (rdoEncrypted.IsChecked == true)
            {
                GenChoice = GenChoices.Encrypted;
                string ti = txtTextInput.Text.Trim();
            }
            if (rdoMiniKeys.IsChecked == true) GenChoice = GenChoices.Minikey;
            if (rdoRandomWallet.IsChecked == true) GenChoice = GenChoices.WIF;
            if (rdoTwoFactor.IsChecked == true) GenChoice = GenChoices.TwoFactor;

            timer1.Interval = TimeSpan.FromMilliseconds(250);
            timer1.Start();
            Generating = true;
            GeneratingEnded = false;
            StopRequested = false;
            btnGenerateAddresses.Content = "Cancel";
            SetControlsEnabled(false);
            progressBar.Visibility = Visibility.Visible;
            GenerationThread.Start();
        }

        private void SetControlsEnabled(bool enabled)
        {
            txtTextInput.IsEnabled = enabled;
            txtGenCount.IsEnabled = enabled;
            rdoMiniKeys.IsEnabled = enabled;
            rdoRandomWallet.IsEnabled = enabled;
            rdoEncrypted.IsEnabled = enabled;
            rdoTwoFactor.IsEnabled = enabled;
            rdoDeterministicWallet.IsEnabled = enabled;
        }

        private void GenerationThreadProcess()
        {
            Bip38Intermediate intermediate = null;
            if (GenChoice == GenChoices.Encrypted)
            {
                intermediate = new Bip38Intermediate(UserText, Bip38Intermediate.Interpretation.Passphrase);
            }

            int detcount = 1;

            while (RemainingToGenerate > 0 && StopRequested == false)
            {
                KeyCollectionItem newitem = null;
                switch (GenChoice)
                {
                    case GenChoices.Minikey:
                        MiniKeyPair mkp = MiniKeyPair.CreateRandom(ExtraEntropy.GetEntropy());
                        string s = mkp.AddressBase58;
                        newitem = new KeyCollectionItem(mkp);
                        break;
                    case GenChoices.WIF:
                        KeyPair kp = KeyPair.Create(ExtraEntropy.GetEntropy());
                        s = kp.AddressBase58;
                        newitem = new KeyCollectionItem(kp);
                        break;
                    case GenChoices.Deterministic:
                        kp = KeyPair.CreateFromString(UserText + detcount);
                        detcount++;
                        s = kp.AddressBase58;
                        newitem = new KeyCollectionItem(kp);
                        break;
                    case GenChoices.Encrypted:
                        Bip38KeyPair ekp = new Bip38KeyPair(intermediate);
                        if (RetainPrivateKeys)
                        {
                            ekp = new Bip38KeyPair(ekp.GetUnencryptedPrivateKey(), UserText);
                        }
                        newitem = new KeyCollectionItem(ekp);
                        break;
                    case GenChoices.TwoFactor:
                        Bip38KeyPair tf = new Bip38KeyPair(intermediatesForGeneration[intermediateIdx++]);
                        if (intermediateIdx >= intermediatesForGeneration.Length) intermediateIdx = 0;
                        newitem = new KeyCollectionItem(tf);
                        break;
                }

                lock (GeneratedItems)
                {
                    GeneratedItems.Add(newitem);
                    RemainingToGenerate--;
                }
            }
            GeneratingEnded = true;
        }

        private List<Bip38Intermediate> ScanClipboardForIntermediateCodes()
        {
            string cliptext = System.Windows.Clipboard.GetText();
            List<object> objects = StringInterpreter.InterpretBatch(cliptext);
            return new List<Bip38Intermediate>(from c in objects where c is Bip38Intermediate select c as Bip38Intermediate);
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (GeneratingEnded)
            {
                Generating = false;
                GeneratingEnded = false;
                progressBar.Value = 0;
                progressBar.Visibility = Visibility.Collapsed;
                statusText.Text = "";

                btnGenerateAddresses.Content = "Generate Addresses";
                timer1.Stop();
                SetControlsEnabled(true);
                if (StopRequested == false)
                {
                    PermissionToCloseWindow = true;
                    DialogResult = true;
                    Close();
                }
                else if (GeneratedItems.Count > 0)
                {
                    statusText.Text = "Keys generated: " + GeneratedItems.Count;
                    if (PermissionToCloseWindow)
                    {
                        Close();
                        return;
                    }
                    else if (MessageBox.Show("Keep the " + GeneratedItems.Count + " generated keys?", "Cancel generation", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        GeneratedItems.Clear();
                    }
                    PermissionToCloseWindow = true;
                    DialogResult = true;
                    Close();
                }
                return;
            }

            if (Generating)
            {
                int generated;
                int totaltogenerate;
                lock (GeneratedItems)
                {
                    generated = GeneratedItems.Count;
                    totaltogenerate = generated + RemainingToGenerate;
                }

                if (generated == 0 && rdoEncrypted.IsChecked == true)
                {
                    statusText.Text = "Hashing the passphrase...";
                }
                else
                {
                    statusText.Text = "Keys generated: " + generated;
                    progressBar.Maximum = totaltogenerate;
                    progressBar.Value = generated;
                }
            }
        }
    }
}
