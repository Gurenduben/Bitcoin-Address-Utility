using System.Windows;
using System.Windows.Documents;
using Casascius.Bitcoin;
using Org.BouncyCastle.Math;

namespace BtcAddress.Views
{
    public partial class KeyCombinerWindow : Window
    {
        public KeyCombinerWindow()
        {
            InitializeComponent();
        }

        private void BtnCombine_Click(object sender, RoutedEventArgs e)
        {
            string input1 = txtInput1.Text;
            string input2 = txtInput2.Text;
            PublicKey pub1 = null;
            PublicKey pub2 = null;
            KeyPair kp1 = null;
            KeyPair kp2 = null;

            if (KeyPair.IsValidPrivateKey(input1))
            {
                pub1 = kp1 = new KeyPair(input1);
            }
            else if (PublicKey.IsValidPublicKey(input1))
            {
                pub1 = new PublicKey(input1);
            }
            else
            {
                MessageBox.Show("Input key #1 is not a valid Public Key or Private Key Hex", "Can't combine", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (KeyPair.IsValidPrivateKey(input2))
            {
                pub2 = kp2 = new KeyPair(input2);
            }
            else if (PublicKey.IsValidPublicKey(input2))
            {
                pub2 = new PublicKey(input2);
            }
            else
            {
                MessageBox.Show("Input key #2 is not a valid Public Key or Private Key Hex", "Can't combine", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (kp1 == null && kp2 == null && rdoAdd.IsChecked != true)
            {
                MessageBox.Show("Can't multiply two public keys.  At least one of the keys must be a private key.",
                    "Can't combine", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (pub1.IsCompressedPoint != pub2.IsCompressedPoint)
            {
                MessageBox.Show("Can't combine a compressed key with an uncompressed key.", "Can't combine", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (pub1.AddressBase58 == pub2.AddressBase58)
            {
                if (MessageBox.Show("Both of the key inputs have the same public key hash.  You can continue, but the results are probably going to be wrong.  You might have provided the wrong information, such as two parts from the same side of the transaction, instead of one part from each side.  Continue anyway?", "Duplicate Key Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
                {
                    return;
                }
            }

            var ps = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");

            if (kp1 != null && kp2 != null)
            {
                BigInteger e1 = new BigInteger(1, kp1.PrivateKeyBytes);
                BigInteger e2 = new BigInteger(1, kp2.PrivateKeyBytes);
                BigInteger ecombined = (rdoAdd.IsChecked == true ? e1.Add(e2) : e1.Multiply(e2)).Mod(ps.N);

                KeyPair kpcombined = new KeyPair(Util.Force32Bytes(ecombined.ToByteArrayUnsigned()), compressed: kp1.IsCompressedPoint);
                txtOutputAddress.Text = kpcombined.AddressBase58;
                txtOutputPubkey.Text = kpcombined.PublicKeyHex.Replace(" ", "");
                txtOutputPriv.Text = kpcombined.PrivateKeyBase58;
            }
            else if (kp1 != null || kp2 != null)
            {
                KeyPair priv = (kp1 == null) ? kp2 : kp1;
                PublicKey pub = (kp1 == null) ? pub1 : pub2;

                ECPoint point = pub.GetECPoint();
                ECPoint combined = rdoAdd.IsChecked == true ? point.Add(priv.GetECPoint()) : point.Multiply(new BigInteger(1, priv.PrivateKeyBytes));
                PublicKey pkcombined = new PublicKey(combined.GetEncoded(priv.IsCompressedPoint));
                txtOutputAddress.Text = pkcombined.AddressBase58;
                txtOutputPubkey.Text = pkcombined.PublicKeyHex.Replace(" ", "");
                txtOutputPriv.Text = "Only available when combining two private keys";
            }
            else
            {
                ECPoint combined = pub1.GetECPoint().Add(pub2.GetECPoint());
                PublicKey pkcombined = new PublicKey(combined.GetEncoded(pub1.IsCompressedPoint));
                txtOutputAddress.Text = pkcombined.AddressBase58;
                txtOutputPubkey.Text = pkcombined.PublicKeyHex.Replace(" ", "");
                txtOutputPriv.Text = "Only available when combining two private keys";
            }
        }

        private void WhyLink_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("EC Addition should not be used for two-factor storage.  Use multiplication instead. Addition is safe when employing a vanity pool to generate vanity addresses, and is required for vanity address generators to achieve GPU-accelerated performance.  For some other uses, addition is unsafe due to its reversibility, so always use multiplication instead wherever possible.");
        }
    }
}
