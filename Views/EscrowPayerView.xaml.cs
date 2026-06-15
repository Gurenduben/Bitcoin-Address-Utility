using System;
using System.Windows;
using System.Windows.Controls;

namespace BtcAddress.Views
{
    public partial class EscrowPayerView : UserControl
    {
        public EscrowPayerView()
        {
            InitializeComponent();
        }

        public string PayerCode1
        {
            get => txtPayerCode1.Text;
            set => txtPayerCode1.Text = value;
        }

        public string PayerCode2
        {
            get => txtPayerCode2.Text;
            set => txtPayerCode2.Text = value;
        }

        public string PayerAddress
        {
            get => txtPayerAddress.Text;
            set => txtPayerAddress.Text = value;
        }

        public bool IsInResetState { get; set; }

        public void SetAddressVisible(bool visible)
        {
            var visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            lblPayerHereIs.Visibility = visibility;
            txtPayerAddress.Visibility = visibility;
            pnlPayerActions.Visibility = visibility;
        }

        public event EventHandler DoneRequested;
        public event EventHandler SaveRequested;
        public event EventHandler PrintRequested;

        private void BtnPayerDone_Click(object sender, RoutedEventArgs e)
        {
            DoneRequested?.Invoke(this, EventArgs.Empty);
            btnPayerDone.Content = IsInResetState ? "Reset" : "Done";
        }

        private void BtnPayerSave_Click(object sender, RoutedEventArgs e)
        {
            SaveRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnPayerPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
