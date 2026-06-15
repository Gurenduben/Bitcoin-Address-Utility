using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Casascius.Bitcoin;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace BtcAddress.Views
{
    public partial class Bip38ConfValidatorWindow : Window
    {
        public Bip38ConfValidatorWindow()
        {
            InitializeComponent();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            lblAddressHeader.Visibility = Visibility.Collapsed;
            lblAddressItself.Visibility = Visibility.Collapsed;
            lblResult.Visibility = Visibility.Collapsed;

            if (txtPassphrase.Text == "")
            {
                MessageBox.Show("Passphrase is required.", "Passphrase required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (txtConfCode.Text == "")
            {
                MessageBox.Show("Confirmation code is required.", "Confirmation code required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            byte[] confbytes = Util.Base58CheckToByteArray(txtConfCode.Text.Trim());
            if (confbytes == null)
            {
                if (txtConfCode.Text.StartsWith("cfrm38"))
                {
                    MessageBox.Show("This is not a valid confirmation code.  It has the right prefix, but doesn't contain valid confirmation data.  Possible typo or incomplete?",
                        "Invalid confirmation code", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                MessageBox.Show("This is not a valid confirmation code.", "Invalid confirmation code", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (confbytes.Length != 51 || confbytes[0] != 0x64 || confbytes[1] != 0x3B || confbytes[2] != 0xF6 ||
                confbytes[3] != 0xA8 || confbytes[4] != 0x9A || confbytes[18] < 0x02 || confbytes[18] > 0x03)
            {
                object result = StringInterpreter.Interpret(txtConfCode.Text.Trim());
                if (result != null)
                {
                    if (result is PassphraseKeyPair)
                    {
                        PassphraseKeyPair ppkp = result as PassphraseKeyPair;
                        if (ppkp.DecryptWithPassphrase(txtPassphrase.Text))
                        {
                            ConfirmIsValid(ppkp.GetAddress().AddressBase58);
                            MessageBox.Show("What you provided contains a private key, not just a confirmation. Confirmation is successful, and with this correct passphrase, you are also able to spend the funds from the address.",
                                "This is actually a private key", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        else
                        {
                            MessageBox.Show("This is not a valid confirmation code.  It looks like an encrypted private key.  Decryption was attempted but the passphrase couldn't decrypt it",
                                "Invalid confirmation code", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
                    }

                    string objectKind = result.GetType().Name;
                    if (objectKind == "AddressBase")
                    {
                        objectKind = "an Address";
                    }
                    else
                    {
                        objectKind = "a " + objectKind;
                    }

                    MessageBox.Show("This is not a valid confirmation code.  Instead, it looks like " + objectKind +
                      ".  Perhaps you entered the wrong thing?  Confirmation codes start with \"cfrm\".",
                      "Invalid confirmation code", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                MessageBox.Show("This is not a valid confirmation code.", "Invalid confirmation code", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            byte[] ownersalt = new byte[8];
            Array.Copy(confbytes, 10, ownersalt, 0, 8);

            bool includeHashStep = (confbytes[5] & 0x04) == 0x04;
            Bip38Intermediate intermediate = new Bip38Intermediate(txtPassphrase.Text, ownersalt, includeHashStep);

            PublicKey pk = new PublicKey(intermediate.passpoint);

            byte[] addresshashplusownersalt = new byte[12];
            Array.Copy(confbytes, 6, addresshashplusownersalt, 0, 4);
            Array.Copy(intermediate.ownerentropy, 0, addresshashplusownersalt, 4, 8);

            byte[] derived = SCrypt.Generate(intermediate.passpoint, addresshashplusownersalt, 1024, 1, 1, 64);

            byte[] derivedhalf2 = new byte[32];
            Array.Copy(derived, 32, derivedhalf2, 0, 32);

            byte[] unencryptedpubkey = new byte[33];
            unencryptedpubkey[0] = (byte)(confbytes[18] ^ (derived[63] & 0x01));

            var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.ECB;
            aes.Key = derivedhalf2;
            ICryptoTransform decryptor = aes.CreateDecryptor();

            decryptor.TransformBlock(confbytes, 19, 16, unencryptedpubkey, 1);
            decryptor.TransformBlock(confbytes, 19 + 16, 16, unencryptedpubkey, 17);

            for (int i = 0; i < 32; i++)
            {
                unencryptedpubkey[i + 1] ^= derived[i];
            }

            var ps = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
            try
            {
                ECPoint point = ps.Curve.DecodePoint(unencryptedpubkey);
                ECPoint pubpoint = point.Multiply(new BigInteger(1, intermediate.passfactor));

                byte flagbyte = confbytes[5];
                bool wantCompressed = (flagbyte & 0x20) != 0x00;
                byte[] pubpointbytes = pubpoint.GetEncoded(wantCompressed);

                PublicKey generatedaddress = new PublicKey(pubpointbytes);

                UTF8Encoding utf8 = new UTF8Encoding(false);
                Sha256Digest sha256 = new Sha256Digest();
                byte[] generatedaddressbytes = utf8.GetBytes(generatedaddress.AddressBase58);
                sha256.BlockUpdate(generatedaddressbytes, 0, generatedaddressbytes.Length);
                byte[] addresshashfull = new byte[32];
                sha256.DoFinal(addresshashfull, 0);
                sha256.BlockUpdate(addresshashfull, 0, 32);
                sha256.DoFinal(addresshashfull, 0);

                for (int i = 0; i < 4; i++)
                {
                    if (addresshashfull[i] != confbytes[i + 6])
                    {
                        MessageBox.Show("This passphrase is wrong or does not belong to this confirmation code.", "Invalid passphrase", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }

                ConfirmIsValid(generatedaddress.AddressBase58);
            }
            catch
            {
                MessageBox.Show("This passphrase is wrong or does not belong to this confirmation code.", "Invalid passphrase", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void ConfirmIsValid(string address)
        {
            lblAddressHeader.Visibility = Visibility.Visible;
            lblAddressItself.Text = address;
            lblAddressItself.Visibility = Visibility.Visible;
            lblResult.Visibility = Visibility.Visible;
        }
    }
}
