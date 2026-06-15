using System.Windows;
using System.Windows.Controls;

namespace BtcAddress.Views
{
    public partial class EscrowHowItWorksView : UserControl
    {
        public EscrowHowItWorksView()
        {
            InitializeComponent();
        }

        public string HowItWorksText
        {
            get => txtHowItWorks.Text;
            set => txtHowItWorks.Text = value;
        }

        public event RoutedEventHandler DisclaimerClicked;

        private void DisclaimerLink_Click(object sender, RoutedEventArgs e)
        {
            DisclaimerClicked?.Invoke(sender, e);
        }
    }
}
