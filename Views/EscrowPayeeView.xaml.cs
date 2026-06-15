using System;
using System.Windows;
using System.Windows.Controls;

namespace BtcAddress.Views
{
    public partial class EscrowPayeeView : UserControl
    {
        public EscrowPayeeView()
        {
            InitializeComponent();
        }

        public string PayeeCode
        {
            get => txtPayeeCode.Text;
            set => txtPayeeCode.Text = value;
        }

        public string PayeeGeneratedInvite
        {
            get => txtPayeeGeneratedInvite.Text;
            set => txtPayeeGeneratedInvite.Text = value;
        }

        public string PayeeGeneratedAddress
        {
            get => txtPayeeGeneratedAddress.Text;
            set => txtPayeeGeneratedAddress.Text = value;
        }

        public event EventHandler GenerateRequested;
        public event EventHandler CopyInviteRequested;
        public event EventHandler SaveInviteRequested;

        private void BtnGenPayee_Click(object sender, RoutedEventArgs e)
        {
            GenerateRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnCopyInvite_Click(object sender, RoutedEventArgs e)
        {
            CopyInviteRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnSaveInvite_Click(object sender, RoutedEventArgs e)
        {
            SaveInviteRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
