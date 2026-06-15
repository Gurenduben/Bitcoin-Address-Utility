using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Casascius.Bitcoin;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing.Printing;

namespace BtcAddress.Views
{
    public partial class PrintVouchersWindow : Window
    {
        public List<KeyCollectionItem> Items = new List<KeyCollectionItem>();
        public bool PrintAttempted = false;

        public PrintVouchersWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = "Print " + Items.Count + " Vouchers";
            cboArtworkStyle.SelectedIndex = 0;

            bool canPreferUnencrypted = Items.Any(i => i.EncryptedKeyPair != null);
            chkPrintUnencrypted.Visibility = canPreferUnencrypted ? Visibility.Visible : Visibility.Collapsed;
            chkPrintUnencrypted.IsChecked = false;
        }

        private void DenominationSymbol_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tb)
            {
                txtDenomination.Text += tb.Text;
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            WinForms.PrintDialog pd = new WinForms.PrintDialog();
            Drawing.PrinterSettings ps = new Drawing.PrinterSettings();
            pd.PrinterSettings = ps;
            WinForms.DialogResult dr = pd.ShowDialog();

            if (dr == WinForms.DialogResult.OK)
            {
                QRPrint printer = new QRPrint();
                printer.PrintMode = QRPrint.PrintModes.PsyBanknote;

                if (!int.TryParse(txtVouchersPerPage.Text, out int notesPerPage))
                {
                    notesPerPage = 3;
                }
                if (notesPerPage < 1) notesPerPage = 1;
                if (notesPerPage > 3) notesPerPage = 3;
                printer.NotesPerPage = notesPerPage;

                string style = ((cboArtworkStyle.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Yellow").ToLowerInvariant();
                switch (style)
                {
                    case "yellow":
                    case "green":
                    case "blue":
                    case "purple":
                    case "greyscale":
                        printer.ImageFilename = "note-" + style + ".png";
                        break;
                }

                printer.Denomination = txtDenomination.Text;
                printer.keys = new List<KeyCollectionItem>(Items.Count);
                printer.PreferUnencryptedPrivateKeys = chkPrintUnencrypted.IsChecked == true;
                foreach (KeyCollectionItem a in Items) printer.keys.Add(a);
                printer.PrinterSettings = pd.PrinterSettings;
                try
                {
                    printer.Print();
                    PrintAttempted = true;
                }
                catch (Win32Exception ex)
                {
                    WinForms.MessageBox.Show("Printing failed because the printer output is currently locked by another process. Close any app using the target output and try again.\r\n\r\n" + ex.Message,
                        "Print error",
                        WinForms.MessageBoxButtons.OK,
                        WinForms.MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    WinForms.MessageBox.Show("Printing failed.\r\n\r\n" + ex.Message,
                        "Print error",
                        WinForms.MessageBoxButtons.OK,
                        WinForms.MessageBoxIcon.Error);
                }
            }
        }
    }
}
