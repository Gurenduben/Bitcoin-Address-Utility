using System.Windows;
using Casascius.Bitcoin;

namespace BtcAddress.Views
{
    public partial class AddSingleAddressWindow : Window
    {
        public AddSingleAddressWindow()
        {
            InitializeComponent();
            textBox1.Focus();
        }

        public object Result { get; private set; }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show("Enter a key first.");
                return;
            }

            if (btnGoMulti.Visibility == Visibility.Visible)
            {
                Result = StringInterpreter.Interpret(textBox1.Text);
                if (Result == null)
                {
                    MessageBox.Show("Unrecognized or invalid string");
                    return;
                }
            }
            else
            {
                Result = StringInterpreter.InterpretBatch(textBox1.Text);
                if (Result == null)
                {
                    MessageBox.Show("Unrecognized or invalid string");
                    return;
                }
            }

            DialogResult = true;
            Close();
        }

        private void BtnGoMulti_Click(object sender, RoutedEventArgs e)
        {
            textBox1.Focus();
            textBox1.AcceptsReturn = true;
            textBox1.TextWrapping = TextWrapping.Wrap;
            btnGoMulti.Visibility = Visibility.Collapsed;
            lblEnterWhat.Text = "Enter or paste text. Addresses and keys will be picked out.";
            Title = "Add Multiple Addresses";
            if (Height < 500)
            {
                Height = 500;
            }
        }
    }
}
