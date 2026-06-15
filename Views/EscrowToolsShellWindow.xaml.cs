using System;
using System.Windows;
using Casascius.Bitcoin;

namespace BtcAddress.Views
{
    public partial class EscrowToolsShellWindow : Window
    {
        public EscrowToolsShellWindow()
        {
            InitializeComponent();

            howItWorksView.DisclaimerClicked += DisclaimerLink_Click;
            payeeView.GenerateRequested += PayeeView_GenerateRequested;
            payerView.DoneRequested += PayerView_DoneRequested;
            agentView.GenerateRequested += AgentView_GenerateRequested;
            redeemView.RedeemRequested += RedeemView_RedeemRequested;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            howItWorksView.HowItWorksText = "How Three-Party Escrow Works\r\n\r\nEscrow allows two people to transact in Bitcoin while leaving their funds visible to everybody and accessible to nobody until somebody releases them. It allows the payer or the payee to release funds to one another, and also lets a third person decide for them if the two can't agree. The third person never has access to take the funds, and is only needed to release the funds if the original two can't agree who gets them. Whoever gets a copy of all three invitations gets access to the funds.\r\n\r\nLet's pretend that Alice wants to pay Bob, and they agree to use Eddie as their escrow agent.\r\n\r\nFirst, Eddie creates a pair of Escrow Invitation codes. This is a matched pair of codes representing a single invitation. These codes can be used by someone else in a future transaction to give Eddie the authority to act as the escrow agent. He gives one code to Alice and the other to Bob, and keeps a copy for himself.\r\n\r\nSecond, Bob creates a Payment Invitation and gives it only to Alice, but keeps a copy for himself. When Alice and Bob use the escrow tool to combine their individual Escrow Invitation codes with the Payment Invitation, they'll get the same Bitcoin address. Alice and Bob must agree they have generated the same address.\r\n\r\nThird, Alice sends Bitcoins to that address. Now, nobody can get them until someone releases them.\r\n\r\nAlice can release the Bitcoins to Bob by giving a copy of her Escrow Invitation code to Bob (so that he now has both halves, as well as his Payment Invitation). He'll use the \"Collect Your Funds\" tab to enter all three, and will receive the private key needed to claim the funds. The private key can be imported into a Bitcoin client or web wallet.\r\n\r\nBob can give a refund to Alice by giving her a copy of his Escrow Invitation code.\r\n\r\nEddie can also force the payment to be awarded to Alice or Bob by giving them both Escrow Invitation codes. Eddie can't claim the payment himself because he would also need the Payment Invitation, which he doesn't have.";
            payerView.IsInResetState = false;
            payerView.SetAddressVisible(false);
        }

        private void DisclaimerLink_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("BECAUSE THIS SOFTWARE IS LICENSED FREE OF CHARGE, THERE IS NO WARRANTY FOR THE SOFTWARE, TO THE EXTENT PERMITTED BY APPLICABLE LAW. EXCEPT WHEN OTHERWISE STATED IN WRITING THE COPYRIGHT HOLDERS AND/OR OTHER PARTIES PROVIDE THE SOFTWARE \"AS IS\" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. THE ENTIRE RISK AS TO THE QUALITY AND PERFORMANCE OF THE SOFTWARE IS WITH YOU. SHOULD THE SOFTWARE PROVE DEFECTIVE, YOU ASSUME THE COST OF ALL NECESSARY SERVICING, REPAIR, OR CORRECTION.\r\n\r\nIN NO EVENT UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN WRITING WILL ANY COPYRIGHT HOLDER, OR ANY OTHER PARTY WHO MAY MODIFY AND/OR REDISTRIBUTE THE SOFTWARE AS PERMITTED BY THE ABOVE LICENCE, BE LIABLE TO YOU FOR DAMAGES, INCLUDING ANY GENERAL, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES ARISING OUT OF THE USE OR INABILITY TO USE THE SOFTWARE (INCLUDING BUT NOT LIMITED TO LOSS OF DATA OR DATA BEING RENDERED INACCURATE OR LOSSES SUSTAINED BY YOU OR THIRD PARTIES OR A FAILURE OF THE SOFTWARE TO OPERATE WITH ANY OTHER SOFTWARE), EVEN IF SUCH HOLDER OR OTHER PARTY HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGES.",
                "Disclaimer of Warranty", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AgentView_GenerateRequested(object sender, EventArgs e)
        {
            EscrowCodeSet cs = new EscrowCodeSet();
            agentView.EscrowForPayer = cs.EscrowInvitationCodeA;
            agentView.EscrowForPayee = cs.EscrowInvitationCodeB;
        }

        private void PayeeView_GenerateRequested(object sender, EventArgs e)
        {
            try
            {
                payeeView.PayeeCode = Util.Base58Trim(payeeView.PayeeCode);
                EscrowCodeSet cs = new EscrowCodeSet(payeeView.PayeeCode);
                payeeView.PayeeGeneratedInvite = cs.PaymentInvitationCode;
                payeeView.PayeeGeneratedAddress = cs.BitcoinAddress;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PayerView_DoneRequested(object sender, EventArgs e)
        {
            if (payerView.IsInResetState)
            {
                payerView.SetAddressVisible(false);
                payerView.IsInResetState = false;
                return;
            }

            try
            {
                payerView.PayerCode1 = Util.Base58Trim(payerView.PayerCode1);
                payerView.PayerCode2 = Util.Base58Trim(payerView.PayerCode2);
                EscrowCodeSet cs = new EscrowCodeSet(payerView.PayerCode1, payerView.PayerCode2);
                payerView.PayerAddress = cs.BitcoinAddress;
                payerView.SetAddressVisible(true);
                payerView.IsInResetState = true;

                if (cs.SamePartyWarningApplies)
                {
                    MessageBox.Show("The Payment Invitation Code appears to have been generated from the same Escrow Invitation Code you entered, and not its mate.  You might be verifying a Payment Invitation you produced yourself, rather than one produced by your trading partner.",
                        "Are you verifying the wrong thing?", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RedeemView_RedeemRequested(object sender, EventArgs e)
        {
            try
            {
                redeemView.RedeemCode1 = Util.Base58Trim(redeemView.RedeemCode1);
                redeemView.RedeemCode2 = Util.Base58Trim(redeemView.RedeemCode2);
                redeemView.RedeemCode3 = Util.Base58Trim(redeemView.RedeemCode3);

                EscrowCodeSet cs = new EscrowCodeSet(redeemView.RedeemCode1, redeemView.RedeemCode2, redeemView.RedeemCode3);
                redeemView.RedeemAddress = cs.BitcoinAddress;
                redeemView.RedeemPrivKey = cs.PrivateKey;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
