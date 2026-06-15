using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Casascius.Bitcoin;
using Drawing = System.Drawing;

namespace BtcAddress.Views
{
    public partial class MainWindow : Window
    {
        private int ChangeFlag = 0;
        private readonly DispatcherTimer _entropyTimer;

        public MainWindow()
        {
            InitializeComponent();
            _entropyTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _entropyTimer.Tick += EntropyTimer_Tick;
        }

        public void DisplayKeyCollectionItem(KeyCollectionItem item)
        {
            try
            {
                ChangeFlag++;
                if (item.EncryptedKeyPair != null)
                {
                    SetText(txtPrivWIF, item.EncryptedKeyPair.EncryptedPrivateKey);
                    UpdateMinikeyDescription();
                    SetText(txtPassphrase, "");
                    SetText(txtPrivHex, item.EncryptedKeyPair.IsUnencryptedPrivateKeyAvailable() ? item.EncryptedKeyPair.GetUnencryptedPrivateKey().PublicKeyHex : "");
                    SetText(txtPubHex, item.EncryptedKeyPair.IsPublicKeyAvailable() ? item.EncryptedKeyPair.GetPublicKey().PublicKeyHex : "");
                    if (item.EncryptedKeyPair.IsAddressAvailable())
                    {
                        AddressBase addr = item.EncryptedKeyPair.GetAddress();
                        SetText(txtPubHash, addr.Hash160Hex);
                        SetText(txtBtcAddr, addr.AddressBase58);
                    }
                    else
                    {
                        SetText(txtPubHash, "");
                        SetText(txtBtcAddr, "");
                    }
                    return;
                }

                SetText(txtMinikey, item.Address is MiniKeyPair mkp ? mkp.MiniKey : "");
                UpdateMinikeyDescription();

                if (item.Address != null)
                {
                    if (item.Address is KeyPair kp)
                    {
                        SetText(txtPrivWIF, kp.PrivateKeyBase58);
                        SetText(txtPrivHex, kp.PrivateKeyHex);
                    }
                    else
                    {
                        SetText(txtPrivWIF, "");
                        SetText(txtPrivHex, "");
                    }

                    if (item.Address is PublicKey pub)
                    {
                        SetText(txtPubHex, pub.PublicKeyHex);
                    }
                    else
                    {
                        SetText(txtPubHex, "");
                    }

                    SetText(txtPubHash, item.Address.Hash160Hex);
                    SetText(txtBtcAddr, item.Address.AddressBase58);
                }
            }
            finally
            {
                ChangeFlag--;
            }
        }

        private void UpdateMinikeyDescription()
        {
            int isminikey = MiniKeyPair.IsValidMiniKey(txtMinikey.Text);
            if (isminikey == 1)
            {
                lblWhyNot.Visibility = Visibility.Collapsed;
                lblNotSafe.Visibility = Visibility.Visible;
                lblNotSafe.Text = "Valid mini key";
                lblNotSafe.Foreground = Brushes.DarkGreen;
            }
            else if (isminikey == -1)
            {
                lblWhyNot.Visibility = Visibility.Collapsed;
                lblNotSafe.Visibility = Visibility.Visible;
                lblNotSafe.Text = "Invalid mini key";
                lblNotSafe.Foreground = Brushes.Red;
            }
            else if ((txtMinikey.Text != "" && txtMinikey.Text.Length < 20) || Util.PassphraseTooSimple(txtMinikey.Text))
            {
                lblWhyNot.Visibility = Visibility.Visible;
                lblNotSafe.Visibility = Visibility.Visible;
                lblNotSafe.Text = "Warning - Not Safe";
                lblNotSafe.Foreground = Brushes.Red;
            }
            else
            {
                lblWhyNot.Visibility = Visibility.Collapsed;
                lblNotSafe.Visibility = Visibility.Collapsed;
                lblNotSafe.Text = "Warning - Not Safe";
                lblNotSafe.Foreground = Brushes.Red;
            }
        }

        private void btnSha256ToPrivate_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlag++;
            try
            {
                SetText(txtPrivHex, RemoveSpacesIf(Util.PassphraseToPrivHex(txtMinikey.Text)));
                UpdateMinikeyDescription();
                btnPrivHexToWIF_Click(null, null);
                btnPrivToPub_Click(null, null);
                btnPubHexToHash_Click(null, null);
                btnPubHashToAddress_Click(null, null);
                SetText(txtMinikey, txtMinikey.Text);
            }
            catch (Exception ae)
            {
                MessageBox.Show(ae.Message);
            }
            finally
            {
                ChangeFlag--;
            }
        }

        private void btnPrivHexToWIF_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlag++;
            try
            {
                if (txtPrivHex.Text.StartsWith("\"") && txtPrivHex.Text.EndsWith("\"") && txtPrivHex.Text.Length > 2)
                {
                    UTF8Encoding utf8 = new UTF8Encoding(false);
                    byte[] str = Util.Force32Bytes(utf8.GetBytes(txtPrivHex.Text.Substring(1, txtPrivHex.Text.Length - 2)));
                    txtPrivHex.Text = RemoveSpacesIf(Util.ByteArrayToString(str));
                }

                KeyPair ba = new KeyPair(txtPrivHex.Text, compressed: compressToolStripMenuItem.IsChecked == true);
                SetText(txtPrivWIF, txtPassphrase.Text != "" ? new Bip38KeyPair(ba, txtPassphrase.Text).EncryptedPrivateKey : ba.PrivateKeyBase58);
                SetText(txtPrivHex, ba.PrivateKeyHex);
                SetText(txtPubHex, ba.PublicKeyHex);
                SetText(txtPubHash, ba.Hash160Hex);
                SetText(txtBtcAddr, new AddressBase(ba, AddressTypeByte).AddressBase58);
            }
            catch (Exception ae)
            {
                MessageBox.Show(ae.Message);
            }
            finally
            {
                ChangeFlag--;
            }
        }

        private void btnPrivWIFToHex_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlag++;
            try
            {
                object interpretation = StringInterpreter.Interpret(txtPrivWIF.Text, compressed: compressToolStripMenuItem.IsChecked == true, addressType: AddressTypeByte);
                KeyPair kp = null;
                if (interpretation is PassphraseKeyPair ppkp)
                {
                    if (txtPassphrase.Text == "")
                    {
                        MessageBox.Show("This is an encrypted key. A passphrase is required.");
                        return;
                    }
                    if (!ppkp.DecryptWithPassphrase(txtPassphrase.Text))
                    {
                        MessageBox.Show("The passphrase is incorrect.");
                        return;
                    }
                    kp = ppkp.GetUnencryptedPrivateKey();
                }
                else if (interpretation is KeyPair)
                {
                    kp = (KeyPair)interpretation;
                }

                if (kp == null)
                {
                    MessageBox.Show("Not a valid private key.");
                    return;
                }

                SetText(txtPrivHex, kp.PrivateKeyHex);
                SetText(txtPubHex, kp.PublicKeyHex);
                SetText(txtPubHash, kp.Hash160Hex);
                SetText(txtBtcAddr, new AddressBase(kp, AddressTypeByte).AddressBase58);
            }
            catch (Exception ae)
            {
                MessageBox.Show(ae.Message);
            }
            finally
            {
                ChangeFlag--;
            }
        }

        private void btnPrivToPub_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlag++;
            try
            {
                KeyPair kp = new KeyPair(txtPrivHex.Text, compressed: compressToolStripMenuItem.IsChecked == true);
                SetText(txtPubHex, kp.PublicKeyHex);
                SetText(txtPubHash, kp.Hash160Hex);
                SetText(txtBtcAddr, new AddressBase(kp, AddressTypeByte).AddressBase58);
            }
            catch (Exception ae)
            {
                MessageBox.Show(ae.Message);
            }
            finally
            {
                ChangeFlag--;
            }
        }

        private void btnPubHexToHash_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlag++;
            try
            {
                PublicKey pub = new PublicKey(txtPubHex.Text);
                SetText(txtPubHash, pub.Hash160Hex);
                SetText(txtBtcAddr, new AddressBase(pub, AddressTypeByte).AddressBase58);
            }
            catch (Exception ae)
            {
                MessageBox.Show(ae.Message);
            }
            finally
            {
                ChangeFlag--;
            }
        }

        private void btnPubHashToAddress_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlag++;
            try
            {
                SetText(txtBtcAddr, Util.PubHashToAddress(txtPubHash.Text, cboCoinType.Text));
            }
            catch (Exception ae)
            {
                MessageBox.Show(ae.Message);
            }
            finally
            {
                ChangeFlag--;
            }
        }

        private void btnAddressToPubHash_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlag++;
            try
            {
                byte[] hex = Util.Base58CheckToByteArray(txtBtcAddr.Text);
                if (hex == null || hex.Length != 21)
                {
                    int l = txtBtcAddr.Text.Length;
                    if (l >= 33 && l <= 34)
                    {
                        if (MessageBox.Show("Address is not valid.  Attempt to correct?", "Invalid address", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            CorrectBitcoinAddress();
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Address is not valid.");
                    }
                    return;
                }
                SetText(txtPubHash, RemoveSpacesIf(Util.ByteArrayToString(hex, 1, 20)));
            }
            finally
            {
                ChangeFlag--;
            }
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlag++;
            try
            {
                lblNotSafe.Visibility = Visibility.Collapsed;
                lblWhyNot.Visibility = Visibility.Collapsed;
                SetText(txtMinikey, "");

                KeyPair kp = KeyPair.Create(ExtraEntropy.GetEntropy(), compressToolStripMenuItem.IsChecked == true);
                SetText(txtPrivWIF, txtPassphrase.Text != "" ? new Bip38KeyPair(kp, txtPassphrase.Text).EncryptedPrivateKey : kp.PrivateKeyBase58);
                SetText(txtPrivHex, kp.PrivateKeyHex);
                SetText(txtPubHex, kp.PublicKeyHex);
                SetText(txtPubHash, kp.Hash160Hex);
                SetText(txtBtcAddr, new AddressBase(kp, AddressTypeByte).AddressBase58);
            }
            finally
            {
                ChangeFlag--;
            }
        }

        private void CorrectBitcoinAddress() => txtBtcAddr.Text = Correction(txtBtcAddr.Text);
        private void CorrectWIF() => txtPrivWIF.Text = Correction(txtPrivWIF.Text);

        private string Correction(string btcaddr)
        {
            int btcaddrlen = btcaddr.Length;
            string b58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            for (int i = 0; i < btcaddrlen; i++)
            {
                for (int j = 0; j < 58; j++)
                {
                    string attempt = btcaddr.Substring(0, i) + b58.Substring(j, 1) + btcaddr.Substring(i + 1);
                    byte[] bytes = Util.Base58CheckToByteArray(attempt);
                    if (bytes != null)
                    {
                        MessageBox.Show("Correction was successful.  Try your request again.");
                        return attempt;
                    }
                }
            }
            return btcaddr;
        }

        private void btnShacode_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlag++;
            try
            {
                MiniKeyPair mkp = MiniKeyPair.CreateRandom(ExtraEntropy.GetEntropy());
                SetText(txtMinikey, mkp.MiniKey);
                SetText(txtPrivWIF, txtPassphrase.Text != "" ? new Bip38KeyPair(new KeyPair(mkp.PrivateKeyBytes), txtPassphrase.Text).EncryptedPrivateKey : new KeyPair(mkp.PrivateKeyBytes).PrivateKeyBase58);
                SetText(txtPrivHex, mkp.PrivateKeyHex);
                SetText(txtPubHex, mkp.PublicKeyHex);
                SetText(txtPubHash, mkp.Hash160Hex);
                SetText(txtBtcAddr, new AddressBase(mkp, AddressTypeByte).AddressBase58);
            }
            finally
            {
                ChangeFlag--;
            }
        }

        private byte AddressTypeByte
        {
            get
            {
                string cointype = (cboCoinType.Text ?? "bitcoin").ToLowerInvariant();
                switch (cointype)
                {
                    case "bitcoin": return 0;
                    case "namecoin": return 52;
                    case "testnet": return 111;
                    case "litecoin": return 48;
                }
                return byte.TryParse(cointype, out byte b) ? b : (byte)0;
            }
        }

        private void walletGeneratorToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new WalletgenWindow().Show();
        }

        private void lblWhyNot_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Bitcoins are vulnerable to theft from hackers when sent to addresses generated from short or non-complex passphrases.  A longer one, or one that uses a good mix of uppercase, lowercase, numbers, and symbols is recommended.", "Security Warning", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ChangeFlag > 0) return;
            TextBox txtSender = sender as TextBox;
            TextBox[] textboxes = new[] { txtMinikey, txtPrivWIF, txtPrivHex, txtPubHex, txtPubHash, txtBtcAddr };
            foreach (TextBox t in textboxes)
            {
                t.Foreground = t == txtSender ? Brushes.Black : Brushes.Gray;
            }
            if (txtSender == txtMinikey && lblNotSafe.Visibility == Visibility.Visible)
            {
                lblNotSafe.Visibility = Visibility.Collapsed;
                lblWhyNot.Visibility = Visibility.Collapsed;
            }
        }

        private void SetText(TextBox thebox, string theText)
        {
            thebox.Foreground = Brushes.Black;
            if (ReferenceEquals(thebox, txtPrivHex) || ReferenceEquals(thebox, txtPubHex) || ReferenceEquals(thebox, txtPubHash))
            {
                thebox.Text = RemoveSpacesIf(theText);
            }
            else
            {
                thebox.Text = theText;
            }
        }

        private void cboCoinType_SelectionChangeCommitted(object sender, SelectionChangedEventArgs e)
        {
            txtBtcAddr.Foreground = Brushes.Gray;
            ChangeFlag++;
            try
            {
                AddressBase addr = new AddressBase(new AddressBase(txtBtcAddr.Text), AddressTypeByte);
                txtBtcAddr.Text = addr.AddressBase58;
            }
            catch { }
            finally
            {
                ChangeFlag--;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                TextBox txtSender = sender as TextBox;
                if (txtSender == txtMinikey) btnSha256ToPrivate_Click(null, null);
                if (txtSender == txtPrivWIF) btnPrivWIFToHex_Click(null, null);
            }
        }

        private void base58CalcToolStripMenuItem_Click(object sender, RoutedEventArgs e) => Program.ShowBase58Calc();
        private void mofNCalcToolStripMenuItem_Click(object sender, RoutedEventArgs e) => Program.ShowMofNcalc();
        private void paperWalletPrinterToolStripMenuItem_Click(object sender, RoutedEventArgs e) => Program.ShowPaperWalletPrinter();
        private void pPECKeygenToolStripMenuItem_Click(object sender, RoutedEventArgs e) => Program.ShowIntermediateGen();
        private void keyCombinerToolStripMenuItem_Click(object sender, RoutedEventArgs e) => Program.ShowKeyCombiner();

        private void spaceBetweenHexBytesToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlag++;
            txtPrivHex.Text = RemoveSpacesIf(Util.ByteArrayToString(Util.HexStringToBytes(txtPrivHex.Text)));
            txtPubHex.Text = RemoveSpacesIf(Util.ByteArrayToString(Util.HexStringToBytes(txtPubHex.Text)));
            txtPubHash.Text = RemoveSpacesIf(Util.ByteArrayToString(Util.HexStringToBytes(txtPubHash.Text)));
            ChangeFlag--;
        }

        private string RemoveSpacesIf(string what) => spaceBetweenHexBytesToolStripMenuItem.IsChecked == true ? what : what.Replace(" ", "");

        private void compressPublicKeyToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlag++;
            try
            {
                PublicKey pub = new PublicKey(txtPubHex.Text);
                pub = new PublicKey(pub.GetCompressed());
                SetText(txtPubHex, pub.PublicKeyHex);
                SetText(txtPubHash, pub.Hash160Hex);
                SetText(txtBtcAddr, new AddressBase(pub, AddressTypeByte).AddressBase58);
            }
            catch (Exception ae)
            {
                MessageBox.Show(ae.Message);
            }
            finally
            {
                ChangeFlag--;
            }
        }

        private void uncompressPublicKeyToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlag++;
            try
            {
                PublicKey pub = new PublicKey(txtPubHex.Text);
                pub = new PublicKey(pub.GetUncompressed());
                SetText(txtPubHex, pub.PublicKeyHex);
                SetText(txtPubHash, pub.Hash160Hex);
                SetText(txtBtcAddr, new AddressBase(pub, AddressTypeByte).AddressBase58);
            }
            catch (Exception ae)
            {
                MessageBox.Show(ae.Message);
            }
            finally
            {
                ChangeFlag--;
            }
        }

        private void compressToolStripMenuItem_Click(object sender, RoutedEventArgs e) { }

        private void showFieldsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            txtPubHex.Focus();
            if (txtPubHex.Text.Length == 130 || txtPubHex.Text.Length == 66)
            {
                txtPubHex.Select(2, 64);
            }
            else if (txtPubHex.Text.Length == 194 || txtPubHex.Text.Length == 98)
            {
                txtPubHex.Select(2, 95);
            }
            else
            {
                MessageBox.Show("Enter a public key first.");
            }
        }

        private void copyPrivateKeyQRMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CopyQrToClipboard(txtPrivWIF.Text, "Enter or create a valid private key first.");
        }

        private void copyMinikeyQRMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CopyQrToClipboard(txtMinikey.Text, "Enter or create a valid minikey first.");
        }

        private void copyAddressQRMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CopyQrToClipboard(txtBtcAddr.Text, "Enter or create a valid address first.");
        }

        private void copyPublicHexQRMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CopyQrToClipboard(txtPubHex.Text.Replace(" ", ""), "Enter or create a valid public key first.");
        }

        private void CopyQrToClipboard(string value, string invalidValueMessage)
        {
            Drawing.Bitmap bitmap = QR.EncodeQRCode(value);
            if (bitmap == null)
            {
                MessageBox.Show(invalidValueMessage);
                return;
            }

            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                BitmapSource image = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                DataObject data = new DataObject();
                data.SetText(value ?? string.Empty);
                data.SetImage(image);
                Clipboard.SetDataObject(data, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to copy QR data to the clipboard. " + ex.Message);
            }
            finally
            {
                DeleteObject(hBitmap);
                bitmap.Dispose();
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private void MainWindow_KeyDown(object sender, KeyEventArgs e) => ExtraEntropy.AddExtraEntropy(e.Key + DateTime.Now.Ticks.ToString());
        private void MainWindow_MouseMove(object sender, MouseEventArgs e) => ExtraEntropy.AddExtraEntropy(DateTime.Now.Ticks + e.GetPosition(this).ToString());
        private void EntropyTimer_Tick(object sender, EventArgs e) => ExtraEntropy.AddExtraEntropy(DateTime.Now.Ticks.ToString());

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            cboCoinType.SelectedIndex = 0;
            _entropyTimer.Start();
        }
    }
}
