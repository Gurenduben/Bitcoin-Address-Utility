using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Casascius.Bitcoin;
using WinForms = System.Windows.Forms;

namespace BtcAddress.Views
{
    public partial class KeyCollectionView : Window
    {
        private sealed class KeyCollectionRow
        {
            public KeyCollectionItem Item { get; set; }
            public bool IsChecked { get; set; }
            public string AddressText { get; set; }
            public string PrivateKeyKind { get; set; }
            public string BalanceText { get; set; }
        }

        public KeyCollection KeyCollection = new KeyCollection();

        private readonly ObservableCollection<KeyCollectionRow> _rows = new ObservableCollection<KeyCollectionRow>();
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;

        public KeyCollectionView()
        {
            InitializeComponent();
            listView1.ItemsSource = _rows;
            statusText.Text = "Click Address to generate some addresses.";

            KeyCollection.ItemAdded += KeyCollection_ItemAdded;
            KeyCollection.ItemsAdded += KeyCollection_ItemsAdded;
            KeyCollection.ItemsDeleted += KeyCollection_ItemsDeleted;
        }

        private void KeyCollection_ItemAdded(KeyCollectionItem item)
        {
            _rows.Add(CreateRow(item));
            UpdateStatusLabel();
        }

        private void KeyCollection_ItemsAdded(IEnumerable<KeyCollectionItem> items)
        {
            foreach (var item in items)
            {
                _rows.Add(CreateRow(item));
            }
            UpdateStatusLabel();
        }

        private void KeyCollection_ItemsDeleted(IEnumerable<KeyCollectionItem> items)
        {
            var set = new HashSet<KeyCollectionItem>(items);
            for (int i = _rows.Count - 1; i >= 0; i--)
            {
                if (set.Contains(_rows[i].Item))
                {
                    _rows.RemoveAt(i);
                }
            }
            UpdateStatusLabel();
        }

        private static KeyCollectionRow CreateRow(KeyCollectionItem item)
        {
            return new KeyCollectionRow
            {
                Item = item,
                IsChecked = true,
                AddressText = item.ToString(),
                PrivateKeyKind = item.PrivateKeyKind,
                BalanceText = "0.00"
            };
        }

        private void UpdateStatusLabel()
        {
            statusText.Text = _rows.Count == 1 ? "1 address" : _rows.Count + " addresses";
        }

        private List<KeyCollectionRow> GetCheckedRows()
        {
            return _rows.Where(r => r.IsChecked).ToList();
        }

        private List<KeyCollectionItem> GetEncryptedItemsToPrint()
        {
            List<KeyCollectionItem> itemsToPrint = new List<KeyCollectionItem>();
            int unprintables = 0;

            foreach (var row in _rows)
            {
                if (!row.IsChecked)
                {
                    continue;
                }

                KeyCollectionItem item = row.Item;
                if (item.EncryptedKeyPair != null || (item.Address != null && item.Address is KeyPair))
                {
                    itemsToPrint.Add(item);
                }
                else
                {
                    unprintables++;
                }
            }

            if (itemsToPrint.Count == 0)
            {
                WinForms.MessageBox.Show("No items with printable private keys are selected.",
                    "Can't print encrypted keys",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
                return null;
            }

            if (unprintables != 0)
            {
                WinForms.MessageBox.Show(unprintables + " of the selected items cannot be printed because the private key is not known.  These items will be skipped.",
                    "Can't print some items",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
            }

            return itemsToPrint;
        }

        private void OpenDetailsForSelectedRow()
        {
            if (listView1.SelectedItem is not KeyCollectionRow selected)
            {
                return;
            }

            selected.IsChecked = true;
            listView1.Items.Refresh();

            Program.ShowAddressUtility();
            Program.AddressUtility.DisplayKeyCollectionItem(selected.Item);
        }

        private void MenuMain_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var position = e.GetPosition(menuMain);
            ExtraEntropy.AddExtraEntropy(DateTime.Now.Ticks + position.X + "," + position.Y);
        }

        private void AddressUtility_Click(object sender, RoutedEventArgs e)
        {
            Program.ShowAddressUtility();
        }

        private void Base58Calculator_Click(object sender, RoutedEventArgs e)
        {
            Program.ShowBase58Calc();
        }

        private void KeyDecrypter_Click(object sender, RoutedEventArgs e)
        {
            Program.ShowKeyDecrypter();
        }

        private void MofNCalculator_Click(object sender, RoutedEventArgs e)
        {
            Program.ShowMofNcalc();
        }

        private void IntermediateGenerator_Click(object sender, RoutedEventArgs e)
        {
            Program.ShowIntermediateGen();
        }

        private void ConfirmationCodeValidator_Click(object sender, RoutedEventArgs e)
        {
            Program.ShowConfValidator();
        }

        private void KeyCombiner_Click(object sender, RoutedEventArgs e)
        {
            Program.ShowKeyCombiner();
        }

        private void EscrowTools_Click(object sender, RoutedEventArgs e)
        {
            Program.ShowEscrowTools();
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            WinForms.DialogResult result = WinForms.MessageBox.Show(
                "Do you want to clear (delete) these keys?  This cannot be undone.",
                "Clear keys?",
                WinForms.MessageBoxButtons.OKCancel, WinForms.MessageBoxIcon.Exclamation);

            if (result != WinForms.DialogResult.OK)
            {
                return;
            }

            KeyCollection.DeleteItemRange(new List<KeyCollectionItem>(KeyCollection.Items));
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NewAddress_Click(object sender, RoutedEventArgs e)
        {
            KeyPair kp = KeyPair.Create(ExtraEntropy.GetEntropy());
            KeyCollectionItem item = new KeyCollectionItem(kp);
            KeyCollection.AddItem(item);
        }

        private void GenerateAddresses_Click(object sender, RoutedEventArgs e)
        {
            var genform = new BtcAddress.Forms.AddressGen();
            genform.ShowDialog();
            if (genform.GeneratedItems != null && genform.GeneratedItems.Count > 0)
            {
                KeyCollection.AddItemRange(genform.GeneratedItems);
            }
        }

        private void EnterAddress_Click(object sender, RoutedEventArgs e)
        {
            var asa = new BtcAddress.Forms.AddSingleAddress();
            asa.ShowDialog();
            if (asa.Result != null)
            {
                if (asa.Result is EncryptedKeyPair)
                {
                    KeyCollection.AddItem(new KeyCollectionItem(asa.Result as EncryptedKeyPair));
                }
                else if (asa.Result is List<object>)
                {
                    List<object> tmpList = (List<object>)asa.Result;
                    foreach (object tmpObj in tmpList)
                    {
                        KeyCollection.AddItem(new KeyCollectionItem(tmpObj as AddressBase));
                        WinForms.Application.DoEvents();
                    }
                }
                else
                {
                    KeyCollection.AddItem(new KeyCollectionItem(asa.Result as AddressBase));
                }
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var row in _rows)
            {
                row.IsChecked = true;
            }
            listView1.Items.Refresh();
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var row in _rows)
            {
                row.IsChecked = false;
            }
            listView1.Items.Refresh();
        }

        private void PrintBanknoteVouchers_Click(object sender, RoutedEventArgs e)
        {
            List<KeyCollectionItem> itemsToPrint = GetEncryptedItemsToPrint();
            if (itemsToPrint == null)
            {
                return;
            }

            var printform = new BtcAddress.Forms.PrintVouchers();
            printform.Items = itemsToPrint;
            printform.ShowDialog();
            if (printform.PrintAttempted)
            {
                foreach (var row in _rows)
                {
                    if (row.IsChecked)
                    {
                        row.IsChecked = false;
                    }
                }
                listView1.Items.Refresh();
            }
        }

        private void PrintPhysicalBitcoinInserts_Click(object sender, RoutedEventArgs e)
        {
            PrintCoinInserts(dense: false);
        }

        private void PrintPhysicalBitcoinInsertsDense_Click(object sender, RoutedEventArgs e)
        {
            PrintCoinInserts(dense: true);
        }

        private void PrintCoinInserts(bool dense)
        {
            List<KeyCollectionItem> itemsToPrint = GetEncryptedItemsToPrint();
            if (itemsToPrint == null)
            {
                return;
            }

            WinForms.PrintDialog pd = new WinForms.PrintDialog();
            System.Drawing.Printing.PrinterSettings ps = new System.Drawing.Printing.PrinterSettings();
            pd.PrinterSettings = ps;
            WinForms.DialogResult dr = pd.ShowDialog();

            if (dr == WinForms.DialogResult.OK)
            {
                CoinInsert printer = dense ? new CoinInsertDense() : new CoinInsert();
                printer.keys = itemsToPrint;
                printer.PrinterSettings = pd.PrinterSettings;
                printer.DenseMode = true;
                printer.Print();

                foreach (var row in _rows)
                {
                    if (row.IsChecked)
                    {
                        row.IsChecked = false;
                    }
                }
                listView1.Items.Refresh();
            }
        }

        private void PrintPaperWallets_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SaveAddressList_Click(object sender, RoutedEventArgs e)
        {
            List<KeyCollectionItem> selected = GetCheckedRows().Select(r => r.Item).ToList();
            if (selected.Count == 0)
            {
                WinForms.MessageBox.Show("No items are selected", "Empty selection",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
                return;
            }

            try
            {
                WinForms.SaveFileDialog saveFileDialog1 = new WinForms.SaveFileDialog();
                saveFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                if (WinForms.DialogResult.OK == saveFileDialog1.ShowDialog())
                {
                    if (saveFileDialog1.FileName != "")
                    {
                        using StreamWriter w = File.CreateText(saveFileDialog1.FileName);
                        foreach (var k in selected)
                        {
                            w.WriteLine(k.GetAddressBase58());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.Message, "Failed to save file", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Exclamation);
            }
        }

        private void SaveAddressListWithPrivKey_Click(object sender, RoutedEventArgs e)
        {
            List<KeyCollectionItem> selected = GetCheckedRows().Select(r => r.Item).ToList();
            if (selected.Count == 0)
            {
                WinForms.MessageBox.Show("No items are selected", "Empty selection",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
                return;
            }

            try
            {
                WinForms.SaveFileDialog saveFileDialog1 = new WinForms.SaveFileDialog();
                saveFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                if (WinForms.DialogResult.OK == saveFileDialog1.ShowDialog())
                {
                    if (saveFileDialog1.FileName != "")
                    {
                        using StreamWriter w = File.CreateText(saveFileDialog1.FileName);
                        foreach (var k in selected)
                        {
                            w.WriteLine(k.PrivateKey + " " + k.GetAddressBase58());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.Message, "Failed to save file", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Exclamation);
            }
        }

        private void DeleteSelectedItems_Click(object sender, RoutedEventArgs e)
        {
            WinForms.DialogResult result = WinForms.MessageBox.Show(
                "Do you want to clear (delete) the selected keys?  This cannot be undone.",
                "Clear keys?",
                WinForms.MessageBoxButtons.OKCancel, WinForms.MessageBoxIcon.Exclamation);

            if (result != WinForms.DialogResult.OK)
            {
                return;
            }

            List<KeyCollectionItem> itemsToDelete = GetCheckedRows().Select(r => r.Item).ToList();
            if (itemsToDelete.Count == 0)
            {
                WinForms.MessageBox.Show("No items selected.",
                    "Nothing to delete",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
                return;
            }

            KeyCollection.DeleteItemRange(itemsToDelete);
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            OpenDetailsForSelectedRow();
        }

        private void ListView1_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenDetailsForSelectedRow();
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not GridViewColumnHeader header || header.Column == null || header.Column.Header == null)
            {
                return;
            }

            string headerName = header.Column.Header.ToString();
            if (headerName != "Address" && headerName != "Private Key" && headerName != "Balance")
            {
                return;
            }

            _sortDirection = _sortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

            ICollectionView view = CollectionViewSource.GetDefaultView(listView1.ItemsSource);
            view.SortDescriptions.Clear();

            string propertyName = headerName == "Private Key" ? "PrivateKeyKind" : (headerName == "Balance" ? "BalanceText" : "AddressText");
            view.SortDescriptions.Add(new SortDescription(propertyName, _sortDirection));
            view.Refresh();
        }
    }
}
