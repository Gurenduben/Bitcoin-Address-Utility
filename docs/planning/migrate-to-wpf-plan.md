# Windows Forms → WPF Migration (IN PROGRESS)

Status: **in progress** (partially migrated; hybrid WPF/WinForms state).
This file is the LLM-optimized implementation plan for migrating Bitcoin Address Utility
from Windows Forms to WPF while maintaining Windows-only focus. For day-to-day guidance
see `CLAUDE.md`; for crypto gate see `test/golden-vectors.md`.

**UI specification:** `docs/design/ui-design.md` (UIDD) is the authoritative source of truth
for the target UI — window inventory, control hierarchy, behavioral logic, validation,
layout mechanics, accessibility, and the printing layer. This plan covers project/build
mechanics and migration sequencing; for any question about *what the UI must do or look
like*, defer to the UIDD. The XAML snippets in this plan are illustrative scaffolding only —
where they conflict with the UIDD, the UIDD wins.

## Current State Snapshot (feature/migrate-to-wpf)

Observed in repository at sync time:

- **Project file is hybrid**: `BtcAddress.csproj` contains both `<UseWPF>true</UseWPF>` and
  `<UseWindowsForms>true</UseWindowsForms>`.
- **WPF app bootstrapping exists**: `App.xaml` + `App.xaml.cs` are present and StartupUri points
  to `Views/KeyCollectionView.xaml`.
- **Program window manager migrated**: `Program.cs` uses WPF `Window` singletons and `showWindow<T>()`
  with `IsVisible`/`Activate()` and close callbacks.
- **Views directory created and populated**: WPF windows are present for major flows (`KeyCollectionView`,
  `MainWindow`, `Base58CalcWindow`, `AddressGenWindow`, `MofNcalcWindow`, `PpecKeygenWindow`,
  `KeyCombinerWindow`, `DecryptKeyWindow`, `Bip38ConfValidatorWindow`, escrow shell/views,
  `PaperWalletPrinterWindow`, `PrintVouchersWindow`, etc.).
- **Legacy WinForms directory still present**: `Forms/*.cs`, `*.Designer.cs`, and `*.resx` remain.
- **Printing layer not yet migrated**: `Reports/QRPrint.cs` still derives from
  `System.Drawing.Printing.PrintDocument` and uses `OnPrintPage`.
- **WPF windows still use WinForms interop in places**: examples include `System.Windows.Forms.MessageBox`
  and `System.Windows.Forms.PrintDialog` usage in WPF code-behind.
- **Collection serialization model unchanged**: `Model/KeyCollection.cs` still uses
  `List<KeyCollectionItem>` (no `ObservableCollection` conversion in model).
- **Build currently succeeds**: `dotnet build` is green in current branch state.
- **Release/docs/version not yet switched**: `README.md` still says WinForms, `CLAUDE.md` still
  describes WinForms architecture/entrypoint, and `AssemblyInfo` is still `1.1.2`.

Interpretation: migration is materially underway but not complete; this plan remains the execution
guide to finish parity and release prep.

## Objective

Migrate UI layer from Windows Forms to WPF targeting `net10.0-windows`, preserve exact
feature parity, maintain offline/security model, keep crypto core untouched. No
cross-platform or mobile support — Windows desktop only.

## Constraints

- **Windows-only**: Target remains `net10.0-windows`, no cross-platform code
- **Feature parity**: All 14+ forms functional, no features removed
- **UI layout unchanged**: Preserve exact control positions, sizes, labels (modernization deferred to Phase 2)
- **Security preserved**: Offline-only (zero network calls), GnuPG signing workflow maintained
- **Crypto untouched**: `Model/` layer unchanged, golden vectors pass without modification
- **Single-file publish**: `PublishSingleFile=true --self-contained true` support retained
- **Code-behind initially**: MVVM strict separation deferred to Phase 2

## Release Gate

**All golden vectors pass** (`test/GoldenVectors/` harness): 26 checks
including private key 0x01, BIP38 spec vectors, mini key, M-of-N, escrow round-trip,
QR boundary lengths. Exit code 0 = success.

```powershell
dotnet test test/GoldenVectors/GoldenVectors.csproj
```

## Architecture Changes

### Before (Windows Forms)
```
BtcAddress.csproj
  <UseWindowsForms>true</UseWindowsForms>
  <TargetFramework>net10.0-windows</TargetFramework>

Program.cs
  Application.EnableVisualStyles()
  Application.Run(new KeyCollectionView())

Forms/
  KeyCollectionView.cs + .Designer.cs + .resx
  Form1.cs + .Designer.cs + .resx
  Base58Calc.cs + .Designer.cs + .resx
  ... (14+ forms)

Reports/
  QRPrint.cs : PrintDocument
  OnPrintPage(PrintPageEventArgs e)
```

### After (WPF)
```
BtcAddress.csproj
  <UseWPF>true</UseWPF>
  <TargetFramework>net10.0-windows</TargetFramework>

App.xaml + App.xaml.cs
  <Application StartupUri="Views/KeyCollectionView.xaml" />

Views/
  KeyCollectionView.xaml + .xaml.cs
  MainWindow.xaml + .xaml.cs (renamed from Form1)
  Base58Calc.xaml + .xaml.cs
  ... (14+ windows)

Reports/
  QRPrint.cs : IDocumentPaginatorSource
  GetPage(int pageNumber) → DocumentPage
```

## Forms Inventory

Authoritative inventory + control trees: `docs/design/ui-design.md` §7 (window index) and §2
(component hierarchy). Summary below for orientation only.

From `Program.cs`, user guide, and codebase exploration:

1. **KeyCollectionView** — Main window (entry point), address list with checkboxes, menus (Tools/File/Address/Selection), status bar
2. **Form1** → **MainWindow** — Address Utility detail view, 20+ fields (WIF, hex, pubkey, address), arrow buttons, conversion logic
3. **Base58Calc** — Base58 encode/decode calculator
4. **AddressGen** — Bulk address generator with progress bar
5. **MofNcalc** — M-of-N key splitter (2-of-3, etc.)
6. **PpecKeygen** — BIP38 intermediate code generator (EC-multiply)
7. **KeyCombiner** — Two-factor key combiner
8. **DecryptKey** — BIP38 key decrypter (passphrase entry)
9. **Bip38ConfValidator** — Confirmation code validator
10. **EscrowTools** — Multi-tab escrow workflow (5+ tabs: How it works, Be a Payee, Be a Payer, Be an Escrow Agent, Redeem)
11. **PaperWalletPrinter** — Paper wallet print tool (dual-panel: generation left, print options right)
12. **Additional forms** — Physical coin insert printer, banknote voucher printer (to be discovered during inventory)

## Printing Layer Migration

### System.Drawing.Printing → WPF DocumentPaginator

**Before (Windows Forms)**:
```csharp
class QRPrint : System.Drawing.Printing.PrintDocument {
    protected override void OnPrintPage(PrintPageEventArgs e) {
        Graphics g = e.Graphics;
        g.DrawImage(qrBitmap, x, y, width, height);
        g.DrawString(text, font, brush, point);
        e.HasMorePages = (currentPage < totalPages);
    }
}
```

**After (WPF)**:
```csharp
class QRPrintDocument : IDocumentPaginatorSource {
    public DocumentPaginator DocumentPaginator => new QRPaginator(keys);
}

class QRPaginator : DocumentPaginator {
    public override DocumentPage GetPage(int pageNumber) {
        DrawingVisual visual = new DrawingVisual();
        using (DrawingContext dc = visual.RenderOpen()) {
            dc.DrawImage(qrBitmapSource, rect);
            dc.DrawText(formattedText, point);
        }
        return new DocumentPage(visual);
    }
}
```

### Runtime Assets
- Voucher artwork: `note-*.png` (yellow/green/blue/purple/greyscale)
- Font: `Ubuntu-R.ttf`
- Copied to output via `<None Include="note-*.png;Ubuntu-R.ttf" CopyToOutputDirectory="PreserveNewest" />`

## Migration Strategy

### Phase 1: Project Configuration & Infrastructure (Steps 1-7)
- Create `feature/migrate-to-wpf` branch
- Change `<UseWindowsForms>` → `<UseWPF>` in `.csproj`
- Create `App.xaml` + `App.xaml.cs`
- Refactor `Program.cs` for WPF application model
- Create `Views/` directory
- Inventory all forms

### Phase 2: XAML Conversion (Steps 8-10)
Drive every window conversion from `docs/design/ui-design.md` (UIDD) — §2 (hierarchy),
§3 (behavior/validation/state machines), §4 (layout/a11y), §5 (parity checklist + don'ts).
- Migrate `KeyCollectionView` (main window) first — UIDD §2.1
- Migrate `Form1` → `MainWindow` (complex layout) — UIDD §2.2 + §3.1 (cascade-grey guard)
- Migrate remaining 12+ forms in priority order — UIDD §2.3–§2.13

### Phase 3: Printing Layer (Steps 11-12)
Layout/content spec per print mode: `docs/design/ui-design.md` §6.
- Adapt `Reports/QRPrint.cs` → `IDocumentPaginatorSource`
- Adapt voucher/coin insert printing

### Phase 4: Data Binding & Serialization (Steps 13-14)
- Update `KeyCollection` for WPF `ObservableCollection`
- Adapt singleton form manager for WPF `Window` lifecycle

### Phase 5: Testing & Validation (Steps 15-27)
- Golden vectors (crypto gate)
- Unit tests
- Manual smoke testing (14+ forms)
- Printing tests (paper wallets, vouchers, coin inserts)
- File I/O tests
- Offline verification
- VM testing (clean Windows, no .NET installed)

### Phase 6: Release Preparation (Steps 28-38)
- Update documentation
- Version increment
- Git workflow (commit, PR, merge, tag)
- Build release artifact
- GnuPG signing
- GitHub release
- Post-migration validation

## Implementation Steps

### Step 1: Create feature branch
```powershell
git checkout develop
git pull origin develop
git checkout -b feature/migrate-to-wpf
```

### Step 2: Update BtcAddress.csproj
Change:
```xml
<UseWindowsForms>true</UseWindowsForms>
```
To:
```xml
<UseWPF>true</UseWPF>
```
Keep all other properties unchanged:
- `<TargetFramework>net10.0-windows</TargetFramework>`
- `<Platforms>x64</Platforms>`
- `<Nullable>disable</Nullable>`
- `<ImplicitUsings>disable</ImplicitUsings>`
- `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>`
- All `<PackageReference>` items
- `<None Include="note-*.png;Ubuntu-R.ttf" ... />`

### Step 3: Create App.xaml
```xml
<Application x:Class="BtcAddress.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="Views/KeyCollectionView.xaml">
    <Application.Resources>
        <!-- Global resources if needed -->
    </Application.Resources>
</Application>
```

### Step 4: Create App.xaml.cs
```csharp
using System;
using System.Windows;

namespace BtcAddress
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Any application-level initialization
        }
    }
}
```

### Step 5: Refactor Program.cs
- Remove `Application.EnableVisualStyles()`
- Remove `Application.SetCompatibleTextRenderingDefault(false)`
- Remove `Application.Run(new KeyCollectionView())`
- Keep static singleton fields (`AddressUtility`, `Base58Calc`, etc.)
- Adapt `showForm<T>()`:
  ```csharp
  private static T showForm<T>(T currentWindow) where T : Window, new()
  {
      if (currentWindow == null || !currentWindow.IsVisible)
      {
          T rv = new T();
          rv.Show();
          return rv;
      }
      else
      {
          currentWindow.Activate();
          return currentWindow;
      }
  }
  ```

### Step 6: Inventory all forms
**Already done — see `docs/design/ui-design.md` (UIDD).** It carries the full per-window
inventory: class name, namespace, control hierarchy, event handlers, menu structures,
validation, dialogs, and printing layer. Use it directly instead of re-parsing
`Forms/*.Designer.cs`. Treat the UIDD as the conversion spec for Steps 8–12; this step is
retained only as a pointer.

### Step 7: Create Views/ directory
```powershell
mkdir Views
```
Or rename `Forms/` → `Views/` (WPF convention).

### Step 8: Migrate KeyCollectionView (main window)
Convert `Forms/KeyCollectionView.cs` + `.Designer.cs` + `.resx` to XAML.

**XAML structure**:
```xml
<Window x:Class="BtcAddress.Views.KeyCollectionView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Bitcoin Key Collection" Height="600" Width="800">
    <DockPanel>
        <!-- Menu at top -->
        <Menu DockPanel.Dock="Top">
            <MenuItem Header="Tools">
                <MenuItem Header="Address Utility" Click="ShowAddressUtility_Click" />
                <MenuItem Header="Base58 Calculator" Click="ShowBase58Calc_Click" />
                <MenuItem Header="Key Decrypter" Click="ShowKeyDecrypter_Click" />
                <MenuItem Header="M-of-N Calculator" Click="ShowMofNcalc_Click" />
                <MenuItem Header="Two-Factor Bitcoin Tools">
                    <MenuItem Header="Intermediate Code Generator" Click="ShowIntermediateGen_Click" />
                    <MenuItem Header="Key Combiner" Click="ShowKeyCombiner_Click" />
                    <MenuItem Header="Confirmation Code Validator" Click="ShowConfValidator_Click" />
                </MenuItem>
                <MenuItem Header="Escrow Tools" Click="ShowEscrowTools_Click" />
            </MenuItem>
            <MenuItem Header="File">
                <MenuItem Header="Clear All" Click="ClearAll_Click" />
                <Separator />
                <MenuItem Header="Exit" Click="Exit_Click" />
            </MenuItem>
            <MenuItem Header="Address">
                <MenuItem Header="New address" Click="NewAddress_Click" />
                <MenuItem Header="Generate addresses" Click="GenerateAddresses_Click" />
                <MenuItem Header="Enter an address/key" Click="EnterAddress_Click" />
            </MenuItem>
            <MenuItem Header="Selection">
                <MenuItem Header="Select All" Click="SelectAll_Click" />
                <MenuItem Header="Deselect All" Click="DeselectAll_Click" />
                <Separator />
                <MenuItem Header="Print Paper Wallets (8 per page)" Click="PrintPaperWallets8_Click" />
                <MenuItem Header="Print Paper Wallets (16 per page)" Click="PrintPaperWallets16_Click" />
                <MenuItem Header="Print Banknote Vouchers" Click="PrintVouchers_Click" />
                <MenuItem Header="Print Physical Bitcoin Inserts" Click="PrintCoinInserts_Click" />
                <MenuItem Header="Print Physical Bitcoin Inserts - Dense" Click="PrintCoinInsertsDense_Click" />
                <Separator />
                <MenuItem Header="Save Address List" Click="SaveAddressList_Click" />
                <MenuItem Header="Delete Selected Items" Click="DeleteSelected_Click" />
            </MenuItem>
        </Menu>

        <!-- Status bar at bottom -->
        <StatusBar DockPanel.Dock="Bottom">
            <StatusBarItem>
                <TextBlock x:Name="statusLabel" Text="Click Address to generate some addresses." />
            </StatusBarItem>
        </StatusBar>

        <!-- ListView in center -->
        <ListView x:Name="listView1" SelectionMode="Multiple">
            <ListView.View>
                <GridView>
                    <GridViewColumn Header="Address" Width="400" DisplayMemberBinding="{Binding Address}" />
                    <GridViewColumn Header="Private Key" Width="100" DisplayMemberBinding="{Binding PrivateKeyKind}" />
                    <GridViewColumn Header="Balance" Width="100" DisplayMemberBinding="{Binding Balance}" />
                </GridView>
            </ListView.View>
        </ListView>
    </DockPanel>
</Window>
```

**Code-behind** (`.xaml.cs`):
- Keep existing event handler logic from `.cs` file
- Update control references (remove `this.listView1`, just use `listView1`)
- Update `KeyCollection.ItemAdded` event handlers
- Bind `ListView.ItemsSource` to `ObservableCollection<KeyCollectionItem>`

**Test**: Launch app, verify window opens, menu items clickable, status bar visible.

### Step 9: Migrate Form1 → MainWindow (Address Utility)
Complex form with 20+ fields, arrow buttons, real-time conversion logic.

**XAML layout** (use `Grid` with `RowDefinitions`/`ColumnDefinitions`):
```xml
<Window x:Class="BtcAddress.Views.MainWindow"
        Title="Address Utility" Height="700" Width="900">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <!-- ... more rows for each field group -->
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="150" />
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>

        <!-- Example row: Private Key WIF -->
        <Label Grid.Row="0" Grid.Column="0" Content="Private Key (WIF):" />
        <TextBox Grid.Row="0" Grid.Column="1" x:Name="txtPrivateKeyWIF" TextChanged="TxtPrivateKeyWIF_TextChanged" />
        <Button Grid.Row="0" Grid.Column="2" Content="→" Click="ConvertWIF_Click" />

        <!-- ... repeat for all fields: hex, pubkey, pubkey hash, address, etc. -->

        <!-- Buttons at bottom -->
        <StackPanel Grid.Row="20" Grid.Column="0" Grid.ColumnSpan="3" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="Generate Address" Click="GenerateAddress_Click" Margin="5" Padding="10,5" />
            <Button Content="Generate Minikey" Click="GenerateMinikey_Click" Margin="5" Padding="10,5" />
            <Button Content="Close" Click="Close_Click" Margin="5" Padding="10,5" />
        </StackPanel>
    </Grid>
</Window>
```

**Test**: Open via Tools → Address Utility, paste WIF key, verify address auto-fills.

### Step 10: Migrate remaining 12+ forms
Priority order (simple → complex):
1. **Base58Calc** — Simple calculator UI, 2-3 TextBoxes, encode/decode buttons
2. **AddressGen** — Progress bar, radio buttons for key type, number input
3. **DecryptKey** — PasswordBox for passphrase, encrypted key input, decrypt button
4. **MofNcalc** — Multiple TextBoxes (Parts 1-8), M/N spinners, generate/decode buttons
5. **PpecKeygen** — Passphrase input, slow operation warning, progress indicator
6. **KeyCombiner** — Two input fields, EC multiplication dropdown, combine button
7. **Bip38ConfValidator** — Passphrase, confirmation code inputs, validate button, result display
8. **EscrowTools** — `TabControl` with 5+ tabs (How it works, Be a Payee, Be a Payer, Be an Escrow Agent, Redeem), each tab has multiple fields, save/print buttons
9. **PaperWalletPrinter** — Dual-panel layout: left side (generation options, deterministic vs random, MiniKey checkbox), right side (print options, 8/16 per page radio buttons, sort button, print button)
10. **Discover remaining forms** — Search for references in `Selection` menu handlers, printing code

For each form:
- Read its UIDD section (`docs/design/ui-design.md` §2/§3) for control tree, behavior, and
  validation — do NOT re-derive from `.Designer.cs`; the UIDD already captured it
- Map Windows Forms controls → WPF equivalents:
  - `TextBox` → `TextBox`
  - `Button` → `Button`
  - `Label` → `Label`
  - `CheckBox` → `CheckBox`
  - `RadioButton` → `RadioButton`
  - `ComboBox` → `ComboBox`
  - `ProgressBar` → `ProgressBar`
  - `TabControl` → `TabControl`
  - `Panel` → `StackPanel` or `Grid`
  - `GroupBox` → `GroupBox`
  - `PasswordBox` (new for WPF, use for passphrase fields)
- Preserve layout (use `Grid.Row`/`Column` or `StackPanel` orientation)
- Convert event handlers (keep logic, update control references)
- Test: Open window, verify controls render, click buttons, type in fields

### Step 11: Adapt Reports/QRPrint.cs
Convert `PrintDocument` → `IDocumentPaginatorSource`.

**New classes**:
```csharp
namespace BtcAddress.Reports
{
    public class QRPrintDocument : IDocumentPaginatorSource
    {
        private List<KeyCollectionItem> _keys;
        private PrintModes _printMode;
        private string _denomination;
        private string _imageFilename;
        private int _notesPerPage;

        public QRPrintDocument(List<KeyCollectionItem> keys, PrintModes mode, /* other params */)
        {
            _keys = keys;
            _printMode = mode;
            // ... initialize
        }

        public DocumentPaginator DocumentPaginator => new QRPaginator(_keys, _printMode, /* ... */);
    }

    public class QRPaginator : DocumentPaginator
    {
        private List<KeyCollectionItem> _keys;
        private PrintModes _printMode;
        private int _pageCount;

        public override bool IsPageCountValid => true;
        public override int PageCount => _pageCount;
        public override Size PageSize { get; set; }
        public override IDocumentPaginatorSource Source => null;

        public override DocumentPage GetPage(int pageNumber)
        {
            DrawingVisual visual = new DrawingVisual();
            DrawingContext dc = visual.RenderOpen();

            // Render page content based on _printMode
            if (_printMode == PrintModes.PubPrivQR)
            {
                // 8 wallets per page: public + private QR codes
                RenderPubPrivQRPage(dc, pageNumber);
            }
            else if (_printMode == PrintModes.PrivQR)
            {
                // 16 wallets per page: private QR only
                RenderPrivQRPage(dc, pageNumber);
            }
            else if (_printMode == PrintModes.PsyBanknote)
            {
                // 3 banknote vouchers per page
                RenderBanknotePage(dc, pageNumber);
            }

            dc.Close();
            return new DocumentPage(visual);
        }

        private void RenderPubPrivQRPage(DrawingContext dc, int pageNumber)
        {
            // Port logic from OnPrintPage:
            // - Calculate positions (8 per page, 2 columns × 4 rows)
            // - Generate QR codes (use existing Barcode/QR.cs)
            // - Convert Bitmap → BitmapSource (BitmapImage or Imaging.CreateBitmapSourceFromHBitmap)
            // - dc.DrawImage(qrBitmapSource, rect)
            // - dc.DrawText(new FormattedText(address, ...), point)
        }

        private void RenderBanknotePage(DrawingContext dc, int pageNumber)
        {
            // Load voucher artwork: note-yellow.png, etc.
            BitmapImage artwork = new BitmapImage(new Uri("note-yellow.png", UriKind.Relative));
            // Position QR codes, denomination, address text on artwork
            // Load Ubuntu font via Typeface
        }
    }

    public enum PrintModes
    {
        PrivQR,
        PubPrivQR,
        PsyBanknote
    }
}
```

**Usage in forms**:
```csharp
// Old:
QRPrint qrPrint = new QRPrint();
qrPrint.keys = selectedKeys;
qrPrint.PrintMode = PrintModes.PubPrivQR;
PrintDialog pd = new PrintDialog();
pd.Document = qrPrint;
if (pd.ShowDialog() == DialogResult.OK) qrPrint.Print();

// New:
QRPrintDocument qrPrintDoc = new QRPrintDocument(selectedKeys, PrintModes.PubPrivQR, /* ... */);
System.Windows.Controls.PrintDialog pd = new System.Windows.Controls.PrintDialog();
if (pd.ShowDialog() == true)
{
    pd.PrintDocument(qrPrintDoc.DocumentPaginator, "Paper Wallet");
}
```

**Test**: Print paper wallet (8-per-page), verify QR codes, addresses, fonts.

### Step 12: Adapt voucher/coin insert printing
Extend `QRPaginator` or create separate `VoucherPrint`, `CoinInsertPrint` classes.

- Voucher: 3 per page, artwork selection (yellow/green/blue/purple/greyscale), denomination, QR positioning
- Coin insert: Dense layout, small font, address + private key miniaturized

**Test**: Print voucher (yellow style), verify artwork loads, QR positioned correctly.

### Step 13: Update KeyCollection serialization
If binding `ListView.ItemsSource` to `ObservableCollection<KeyCollectionItem>`:
- `KeyCollectionItem` implements `INotifyPropertyChanged` (if properties change at runtime)
- `KeyCollection.Items` → `ObservableCollection<KeyCollectionItem>` (instead of `List<>`)
- Verify XML serialization round-trip (`XmlSerializer`) unchanged

**Test**: Add address, save to XML, reload, verify list repopulates.

### Step 14: Update singleton form manager
Adapt `Program.showForm<T>()` for WPF:
```csharp
// Change Form → Window
private static T showForm<T>(T currentWindow) where T : Window, new()
{
    if (currentWindow == null || !currentWindow.IsVisible)
    {
        T rv = new T();
        rv.Show();
        return rv;
    }
    else
    {
        currentWindow.Activate(); // WPF: Activate instead of Focus
        return currentWindow;
    }
}
```

**Test**: Open Address Utility twice, verify second call activates existing window.

### Step 15: Verify runtime assets
Check `BtcAddress.csproj`:
```xml
<ItemGroup>
  <None Include="note-*.png;Ubuntu-R.ttf" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

**Test**: Build, check `bin/Debug/net10.0-windows/`, verify files present.

### Step 16: Remove obsolete Windows Forms files
After all XAML conversions confirmed working:
```powershell
Remove-Item Forms/*.Designer.cs
Remove-Item Forms/*.resx
```
Keep `.cs` code-behind if logic reused in `.xaml.cs`.

### Step 17: Update .editorconfig (optional)
Add XAML formatting:
```ini
[*.xaml]
indent_size = 2
```
Not CI-enforced initially; defer strict XAML linting to Phase 2.

### Step 18: Run golden vectors
```powershell
dotnet test test/GoldenVectors/GoldenVectors.csproj -c Release
```
Must exit 0. If failures, Model layer inadvertently changed → rollback.

### Step 19: Run unit tests
```powershell
dotnet test test/UnitTests/UnitTests.csproj
```
All must pass.

### Step 20: Manual smoke test all forms
Open each window, verify:
- **KeyCollectionView**: Add address (Address → New address), verify appears in list; delete (Selection → Delete Selected Items); sort by column (click header)
- **MainWindow**: Paste WIF key, verify address auto-fills; click arrow buttons, verify conversions
- **Base58Calc**: Encode text → Base58, decode back, verify round-trip
- **AddressGen**: Generate 10 addresses, verify progress bar, addresses added to collection
- **DecryptKey**: Decrypt BIP38 key (use test key from golden vectors: `6PRVWUbkzzsbcVac2qwfssoUJAN1Xhrg6bNk8J7Nzm5H7kxEbn2Nh2ZoGg`, passphrase `TestingOneTwoThree`), verify WIF recovered
- **MofNcalc**: Split 2-of-3, verify 3 parts generated; enter 2 parts, decode, verify address matches
- **EscrowTools**: Navigate all tabs, verify no crashes, text inputs work
- **PaperWalletPrinter**: Click "Generate Addresses", verify count increments; click "Print the wallet", verify print dialog opens

### Step 21: Print tests
Connect printer or use "Microsoft Print to PDF":
- Paper wallet 8-per-page: Verify QR codes scannable, WIF keys legible, addresses aligned
- Paper wallet 16-per-page: Verify compact layout, all 16 fit
- Voucher (yellow): Verify artwork loads, QR positioned, denomination printed
- Coin insert (dense): Verify small font legible, address fits

### Step 22: File I/O tests
- Save address list (Selection → Save Address List), verify `.txt` format unchanged
- If XML serialization used, save collection, reload, verify round-trip

### Step 23: Offline verification
```powershell
grep -r "HttpClient|WebRequest|Socket|new Uri(" --include="*.cs" --exclude-dir=test Model/ Views/ Reports/ Barcode/
```
Must return empty (no network calls).

### Step 24: Build release
```powershell
dotnet build BtcAddress.csproj -c Release
```

### Step 25: Publish single-file exe
```powershell
dotnet publish BtcAddress.csproj -r win-x64 -c Release -p:PublishSingleFile=true --self-contained true -o publish/
```
Verify `publish/BtcAddress.exe` exists, size ~80-120 MB.

### Step 26: VM test
- Copy `publish/BtcAddress.exe` to clean Windows 10 VM (no .NET installed)
- Run, verify all forms open
- Generate address, print to PDF
- Verify no missing DLL errors

### Step 27: Run dotnet format
```powershell
dotnet format whitespace BtcAddress.sln --verify-no-changes
dotnet format style BtcAddress.sln --verify-no-changes
dotnet format analyzers BtcAddress.sln --verify-no-changes
```
Fix any violations (CI will enforce).

### Step 28: Update CLAUDE.md
Add section after "Migration status":

```markdown
## WPF Migration (v1.2.0)

The Windows Forms → WPF retarget is **done** (branch `feature/migrate-to-wpf`): XAML-based
UI, WPF printing, same `net10.0-windows` target (Windows-only). Model layer untouched,
golden vectors pass.

### What changed
- `<UseWindowsForms>true` → `<UseWPF>true` in `.csproj`
- Forms converted to XAML (`Views/` directory)
- `Program.cs` adapted for WPF `Application` model
- `Reports/QRPrint.cs` migrated from `PrintDocument` → `IDocumentPaginatorSource`

### What stayed the same
- Target: `net10.0-windows` (Windows desktop only, no cross-platform)
- Dependencies: BouncyCastle, QRCoder (unchanged)
- Model layer: `Casascius.Bitcoin` namespace (untouched)
- Security: Offline-only, zero network calls
- Publish: Single-file self-contained exe

### Phase 2 (future)
- MVVM strict separation (ViewModels, ICommand, INotifyPropertyChanged)
- UI modernization (Material Design, Fluent UI, dark mode)
- Authenticode signing (in addition to GPG)
```

### Step 29: Update README.md
Change:
```markdown
- .NET 10, WinForms, x64. No cross-platform support.
```
To:
```markdown
- .NET 10, WPF, x64. No cross-platform support.
```

Keep build/publish commands unchanged (they still work).

### Step 30: Update version
Edit `Properties/AssemblyInfo.cs`:
```csharp
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]
[assembly: AssemblyInformationalVersion("1.2.0")]
```

### Step 31: Commit & push
```powershell
git add -A
git commit -m "Migrate Windows Forms to WPF (Windows-only, feature parity)"
git push origin feature/migrate-to-wpf
```

### Step 32: Create PR
- Open pull request `feature/migrate-to-wpf` → `develop`
- Link to `docs/planning/migrate-to-wpf-plan.md`
- Request review
- Wait for CI to pass (build + test + format + lint)

### Step 33: Merge to develop
After review approval and CI green:
```powershell
git checkout develop
git merge feature/migrate-to-wpf
git push origin develop
```

When ready for release:
```powershell
git checkout master
git merge develop
git push origin master
```

### Step 34: Tag release
```powershell
git tag -s v1.2.0 -m "WPF migration, Windows-only, feature parity"
git push origin v1.2.0
```

### Step 35: Build release artifact
```powershell
dotnet publish BtcAddress.csproj -r win-x64 -c Release -p:PublishSingleFile=true --self-contained true -o release/
```

Sign with GPG per `SIGNING.md` workflow:
```powershell
gpg --detach-sign --armor release/BtcAddress.exe
# Creates release/BtcAddress.exe.asc
```

### Step 36: GitHub release
- Go to https://github.com/odolvlobo/Bitcoin-Address-Utility/releases
- Click "Draft a new release"
- Tag: `v1.2.0`
- Title: `Bitcoin Address Utility v1.2.0 — WPF Migration`
- Description:
  ```
  Windows desktop Bitcoin key/address utility. WPF UI migration from Windows Forms.

  **Changes in v1.2.0:**
  - Migrated UI from Windows Forms to WPF (XAML-based)
  - Windows-only (net10.0-windows), no feature changes
  - Crypto core untouched, golden vectors pass
  - Same offline/security model, GnuPG signed

  **Verifying the download:**
  See README.md for GPG signature verification (key 6B6BC26599EC24EF7E29A405EAF050539D0B2925).
  ```
- Attach files: `release/BtcAddress.exe`, `release/BtcAddress.exe.asc`
- Publish

### Step 37: Post-migration validation
- Download release artifact from GitHub
- Verify GPG signature:
  ```powershell
  gpg --verify BtcAddress.exe.asc BtcAddress.exe
  ```
- Test on fresh Windows 11 machine (no .NET installed)
- Generate address, verify matches golden vector expectations
- Print paper wallet to PDF, verify QR codes

### Step 38: Plan Phase 2
Create `docs/planning/modernize-wpf-ui-plan.md` for future work:
- MVVM ViewModels (strict separation, ICommand, RelayCommand, INotifyPropertyChanged)
- UI modernization (MaterialDesignInXamlToolkit or ModernWpfUI, responsive layouts, dark mode)
- Accessibility (keyboard navigation, screen reader support, high contrast themes)
- Authenticode signing (in addition to GPG, for Windows SmartScreen bypass)
- Code coverage improvements (UI testing with FlaUI or Appium)

## Key Risks & Mitigations

### Risk: Printing complexity
**Issue**: `System.Drawing.Printing` → WPF `DocumentPaginator` is non-trivial. Voucher artwork, QR positioning, font rendering may break.

**Mitigation**:
- Port existing `OnPrintPage` logic incrementally
- Test each print mode separately (8-per-page, 16-per-page, vouchers)
- Keep `System.Drawing` for Bitmap→BitmapSource conversion if needed
- Golden vector QR boundary tests guard encoding

### Risk: Designer file complexity
**Issue**: 14+ `.Designer.cs` files with complex control hierarchies, anchoring, docking.

**Mitigation**:
- Parse `.Designer.cs` methodically, extract control tree
- Use WPF `Grid` with `RowDefinitions`/`ColumnDefinitions` for layout (more flexible than absolute positioning)
- Test each form individually before moving to next
- Keep original `.Designer.cs` until XAML confirmed working (easy rollback)

### Risk: ListView/DataGrid differences
**Issue**: Windows Forms `ListView` with checkboxes differs from WPF `ListView` item templates.

**Mitigation**:
- Use WPF `ListView` with `GridView` columns (similar to Windows Forms)
- CheckBoxes in WPF require item template binding (`IsSelected` or custom `IsChecked` property)
- Test selection model (single vs multiple) carefully

### Risk: Serialization compatibility
**Issue**: `KeyCollection` XML serialization may break with WPF data binding changes.

**Mitigation**:
- Keep `XmlSerializer` attributes unchanged
- Test round-trip (save → load) after each `ObservableCollection` change
- If `INotifyPropertyChanged` added, ensure serializer ignores events (mark `[XmlIgnore]`)

### Risk: Singleton form pattern
**Issue**: `Program.showForm<T>()` uses Windows Forms `Form.Visible`, `Form.Focus()` — WPF differs.

**Mitigation**:
- Adapt early (Step 5/14)
- WPF: `Window.IsVisible`, `Window.Activate()`
- Test each form singleton behavior (open twice, verify no duplicates)

### Risk: Breaking changes in dependencies
**Issue**: BouncyCastle or QRCoder behavior changes in WPF context.

**Mitigation**:
- Model layer unchanged (no dependency updates)
- Golden vectors guard crypto output (if vectors pass, dependencies OK)
- QR rendering tested explicitly (Step 11)

## Out of Scope (Phase 2)

Per user requirements, the following are **deferred**:

### UI Modernization
- Material Design or Fluent UI styles
- Responsive layouts (adaptive to window resize)
- Dark mode support
- Custom control templates
- Animations/transitions

### MVVM Strict Separation
- ViewModels with full `INotifyPropertyChanged`
- `ICommand` implementations (RelayCommand, DelegateCommand)
- Dependency injection (IoC container)
- Unit testing ViewModels

### Signing Improvements
- Authenticode signing (for Windows SmartScreen)
- Dual-signing (Authenticode + GPG)
- RFC 3161 timestamping
- SLSA provenance attestations

### Cross-Platform/Mobile
- Avalonia UI migration
- .NET MAUI
- Linux/macOS support
- iOS/Android apps

### Advanced Features
- Clipboard integration improvements
- Drag-and-drop address import
- Localization/internationalization
- Auto-update mechanism

## Success Criteria

UI parity is verified against the UIDD feature-parity checklist (`docs/design/ui-design.md`
§5.1) and its "strict don'ts" (§5.2) in addition to the gates below.

### Required (gate)
- [ ] Golden vectors pass (26 checks, exit code 0)
- [ ] Unit tests pass (all)
- [ ] All 14+ forms functional (manual smoke test)
- [ ] Printing works (paper wallets, vouchers, coin inserts)
- [ ] File I/O unchanged (save/load address lists)
- [ ] Offline verification (zero network calls)
- [ ] Single-file publish works (runs on clean Windows VM)
- [ ] GnuPG signing workflow preserved
- [ ] CI passes (build, test, format, lint)

### Nice-to-have (Phase 2)
- [ ] MVVM ViewModels
- [ ] Material Design UI
- [ ] Dark mode
- [ ] Authenticode signing
- [ ] Accessibility improvements

## Notes

- This plan follows the structure of `upgrade-to-dotnet-10-plan.md` (record of what shipped, deviations noted)
- After execution, mark status as **done** and document deviations in "What shipped" section
- Keep this file as historical record, update `CLAUDE.md` for day-to-day guidance
- Version increment: 1.1.2 → 1.2.0 (minor bump for UI framework change, no feature changes)
