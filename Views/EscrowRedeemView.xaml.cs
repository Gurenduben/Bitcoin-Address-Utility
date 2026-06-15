using System;
using System.Windows;
using System.Windows.Controls;

namespace BtcAddress.Views
{
    public partial class EscrowRedeemView : UserControl
    {
        public EscrowRedeemView()
        {
            InitializeComponent();
        }

        public string RedeemCode1
        {
            get => txtRedeemCode1.Text;
            set => txtRedeemCode1.Text = value;
        }

        public string RedeemCode2
        {
            get => txtRedeemCode2.Text;
            set => txtRedeemCode2.Text = value;
        }

        public string RedeemCode3
        {
            get => txtRedeemCode3.Text;
            set => txtRedeemCode3.Text = value;
        }

        public string RedeemPrivKey
        {
            get => txtRedeemPrivKey.Text;
            set => txtRedeemPrivKey.Text = value;
        }

        public string RedeemAddress
        {
            get => txtRedeemAddress.Text;
            set => txtRedeemAddress.Text = value;
        }

        public event EventHandler RedeemRequested;

        private void BtnRedeem_Click(object sender, RoutedEventArgs e)
        {
            RedeemRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
