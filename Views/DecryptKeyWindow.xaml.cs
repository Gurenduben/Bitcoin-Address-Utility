using System.Windows;
using Casascius.Bitcoin;
using Org.BouncyCastle.Math;

namespace BtcAddress.Views
{
    public partial class DecryptKeyWindow : Window
    {
        public DecryptKeyWindow()
        {
            InitializeComponent();
        }

        private void BtnDecrypt_Click(object sender, RoutedEventArgs e)
        {
            txtEncrypted.Text = txtEncrypted.Text.Replace("-", "").Replace(" ", "");

            if (txtEncrypted.Text == "" || txtPassphrase.Text == "")
            {
                MessageBox.Show("Enter an encrypted key and its passphrase.", "Entries Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            object encrypted = StringInterpreter.Interpret(txtEncrypted.Text);

            if (encrypted == null)
            {
                if (txtEncrypted.Text.StartsWith("cfrm38"))
                {
                    var r = MessageBox.Show("This is not a private key.  This looks like a confirmation code.  Do you want to open the Confirmation Code Validator?", "Invalid private key", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                    if (r == MessageBoxResult.Yes)
                    {
                        Program.ShowConfValidator();
                    }
                    return;
                }

                string containsL = "";
                if (txtEncrypted.Text.Contains("l"))
                {
                    containsL = " Your entry contains the lowercase letter l.  Private keys are far more likely to contain the digit 1, and not the lowercase letter l.";
                }

                MessageBox.Show("The private key entry (top box) was invalid.  Please verify the private key was properly typed." + containsL, "Invalid private key", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (encrypted is PassphraseKeyPair)
            {
                PassphraseKeyPair pkp = encrypted as PassphraseKeyPair;
                if (!pkp.DecryptWithPassphrase(txtPassphrase.Text))
                {
                    MessageBox.Show("The passphrase is incorrect.", "Could not decrypt", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                MessageBox.Show("Decryption successful.", "Decryption", MessageBoxButton.OK, MessageBoxImage.Information);
                Program.ShowAddressUtility(new KeyCollectionItem(pkp.GetUnencryptedPrivateKey()));
                return;
            }
            else if (encrypted is KeyPair)
            {
                object encrypted2 = StringInterpreter.Interpret(txtPassphrase.Text);
                if (encrypted2 == null)
                {
                    var r = MessageBox.Show("Does the key you entered belong to the following address?: " + (encrypted as KeyPair).AddressBase58,
                        "Key appears unencrypted",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.None);

                    if (r == MessageBoxResult.Yes)
                    {
                        r = MessageBox.Show("Then this key is already unencrypted and you don't need to decrypt it.  Would you like to open it in the Address Utility screen to see its various forms?", "Key is not encrypted", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        if (r == MessageBoxResult.Yes)
                        {
                            Program.ShowAddressUtility(new KeyCollectionItem(encrypted as KeyPair));
                        }
                    }
                    else
                    {
                        MessageBox.Show("The passphrase or secondary key is incorrect.  Please verify it was properly typed.", "Second entry is not a valid private key", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                    return;
                }

                BigInteger n1 = new BigInteger(1, (encrypted as KeyPair).PrivateKeyBytes);
                BigInteger n2 = new BigInteger(1, (encrypted2 as KeyPair).PrivateKeyBytes);
                var ps = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
                BigInteger privatekey = n1.Multiply(n2).Mod(ps.N);
                MessageBox.Show("Keys successfully combined using EC multiplication.", "EC multiplication successful", MessageBoxButton.OK, MessageBoxImage.Information);
                if (n1.Equals(n2))
                {
                    MessageBox.Show("The two key entries have the same public hash.  The results you see might be wrong.", "Duplicate key hash", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                KeyPair kp = new KeyPair(privatekey);
                Program.ShowAddressUtility(new KeyCollectionItem(kp));
            }
            else if (encrypted is AddressBase)
            {
                MessageBox.Show("This is not a private key.  It looks like an address or a public key.  Private keys usually start with 5, 6, or S.", "Not a private key", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                MessageBox.Show("This is not a private key that this program can decrypt.", "Not a recognized private key", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
    }
}
