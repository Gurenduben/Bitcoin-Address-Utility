using System;
using System.Windows;
using System.Windows.Controls;

namespace BtcAddress.Views
{
    public partial class EscrowAgentView : UserControl
    {
        public EscrowAgentView()
        {
            InitializeComponent();
        }

        public string EscrowForPayer
        {
            get => txtEscrowForPayer.Text;
            set => txtEscrowForPayer.Text = value;
        }

        public string EscrowForPayee
        {
            get => txtEscrowForPayee.Text;
            set => txtEscrowForPayee.Text = value;
        }

        public event EventHandler GenerateRequested;

        private void BtnGenerateEscrowInvitation_Click(object sender, RoutedEventArgs e)
        {
            GenerateRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
