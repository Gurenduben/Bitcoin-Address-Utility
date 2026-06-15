using System;
using System.Windows;
using Casascius.Bitcoin;

namespace BtcAddress.Views
{
    public partial class EscrowToolsWindow : Window
    {
        public EscrowToolsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtHowItWorks.Text = "How Three-Party Escrow Works\r\n\r\nEscrow allows two people to transact in Bitcoin while leaving their funds visible to everybody and accessible to nobody until somebody releases them. Whoever gets a copy of all three invitations gets access to the funds.";
            SetPayerElementsVisible(false);
        }

        private void DisclaimerLink_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("BECAUSE THIS SOFTWARE IS LICENSED FREE OF CHARGE, THERE IS NO WARRANTY FOR THE SOFTWARE, TO THE EXTENT PERMITTED BY APPLICABLE LAW.",
                "Disclaimer of Warranty", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnGenerateEscrowInvitation_Click(object sender, RoutedEventArgs e)
        {
            EscrowCodeSet cs = new EscrowCodeSet();
            txtEscrowForPayer.Text = cs.EscrowInvitationCodeA;
            txtEscrowForPayee.Text = cs.EscrowInvitationCodeB;
        }

        private void BtnGenPayee_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtPayeeCode.Text = Util.Base58Trim(txtPayeeCode.Text);
                EscrowCodeSet cs = new EscrowCodeSet(txtPayeeCode.Text);
                txtPayeeGeneratedInvite.Text = cs.PaymentInvitationCode;
                txtPayeeGeneratedAddress.Text = cs.BitcoinAddress;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SetPayerElementsVisible(bool visible)
        {
            var v = visible ? Visibility.Visible : Visibility.Collapsed;
            lblPayerHereIs.Visibility = v;
            txtPayerAddress.Visibility = v;
            btnPayerSave.Visibility = v;
            btnPayerPrint.Visibility = v;
        }

        private void BtnPayerDone_Click(object sender, RoutedEventArgs e)
        {
            if (btnPayerDone.Content?.ToString() == "Reset")
            {
                SetPayerElementsVisible(false);
                btnPayerDone.Content = "Done";
                return;
            }

            try
            {
                txtPayerCode1.Text = Util.Base58Trim(txtPayerCode1.Text);
                txtPayerCode2.Text = Util.Base58Trim(txtPayerCode2.Text);
                EscrowCodeSet cs = new EscrowCodeSet(txtPayerCode1.Text, txtPayerCode2.Text);
                txtPayerAddress.Text = cs.BitcoinAddress;
                SetPayerElementsVisible(true);
                btnPayerDone.Content = "Reset";
                if (cs.SamePartyWarningApplies)
                {
                    MessageBox.Show("The Payment Invitation Code appears to have been generated from the same Escrow Invitation Code you entered, and not its mate.",
                        "Are you verifying the wrong thing?", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnRedeem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtRedeemCode1.Text = Util.Base58Trim(txtRedeemCode1.Text);
                txtRedeemCode2.Text = Util.Base58Trim(txtRedeemCode2.Text);
                txtRedeemCode3.Text = Util.Base58Trim(txtRedeemCode3.Text);

                EscrowCodeSet cs = new EscrowCodeSet(txtRedeemCode1.Text, txtRedeemCode2.Text, txtRedeemCode3.Text);
                txtRedeemAddress.Text = cs.BitcoinAddress;
                txtRedeemPrivKey.Text = cs.PrivateKey;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
