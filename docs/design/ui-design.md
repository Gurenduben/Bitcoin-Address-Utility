# UI DESIGN DOCUMENT (UIDD) — Bitcoin Address Utility

> Source-of-truth for cross-platform UI migration. Source platform: Windows Forms (.NET 10, `net10.0-windows`, x64). Target-agnostic. Optimized for LLM context injection, not human reading. Domain/crypto logic (`Model/`, namespace `Casascius.Bitcoin`) is OUT OF SCOPE — preserve all calls into it verbatim; this doc covers only the UI layer (`Forms/`, `Reports/`, `Program.cs` window manager).
>
> Coordinates/sizes are source-platform absolute pixels at 96 DPI (AutoScaleMode=Font, AutoScaleDimensions 6×13). Treat them as RELATIVE proportions + reading order, NOT literal target pixels. Migrate to flow/grid layout; do not hardcode absolute positioning on the target.

---

## 0. GLOBAL APP MODEL

- **App type:** Multi-window desktop tool (MDI-less; independent top-level windows). Offline/air-gapped, zero network calls. Security-critical (handles private keys).
- **Entry window:** `KeyCollectionView` (NOT `Form1`). Launched at startup.
- **Window manager (`Program.cs`):** Static singleton registry. One live instance per tool window. Pattern `showForm<T>(current)`:
  - `if current == null || !current.Visible` → construct new, `Show()`, store ref.
  - `else` → `Focus()` existing (bring-to-front, no duplicate).
  - Singleton accessors: `ShowAddressUtility`, `ShowBase58Calc`, `ShowMofNcalc`, `ShowIntermediateGen` (PpecKeygen), `ShowKeyCombiner`, `ShowKeyDecrypter` (DecryptKey), `ShowConfValidator` (Bip38ConfValidator), `ShowEscrowTools`.
  - `Program.AddressUtility` is the live `Form1` ref; other tools call `Program.ShowAddressUtility()` then `Program.AddressUtility.DisplayKeyCollectionItem(item)` to push a result into it.
- **Modal vs modeless:** Tool windows = modeless (`Show()`). `AddressGen`, `PrintVouchers`, `AddSingleAddress` = modal dialogs (`ShowDialog()`, return `DialogResult` / `Result`).
- **Entropy harvesting (a11y/behavior note):** Several windows feed mouse-move coords, key codes, and tick counts into `ExtraEntropy` on `MouseMove`/`KeyDown`/`Timer.Tick`. MUST be preserved — it seeds the RNG. Wire equivalent global input/timer taps on target. Forms doing this: `KeyCollectionView` (menu MouseMove), `Form1` (MouseMove, KeyDown, 1 timer), `AddressGen` (timer).

---

## 1. PLATFORM-AGNOSTIC DESIGN TOKENS

WinForms carries no explicit design system; tokens below are reverse-engineered from observed constants. Source used OS-default control styling. Target SHOULD apply a coherent system; values here define semantic INTENT.

### 1.1 Color tokens

| Token | Value | Usage |
| --- | --- | --- |
| `color.text.default` | system `WindowText` (≈#000) | normal field/label text |
| `color.text.disabled` | system `GrayText` (≈#6D6D6D) | greyed/stale field text (see §3 cascade-grey) |
| `color.field.bg` | system `Window` (≈#FFF) | textbox background |
| `color.warning.fg` | rgb(192,0,0) | "Warning: Not safe" minikey label |
| `color.link.fg` | system blue, underline font | clickable LinkLabel ("why?", "(why?)", DISCLAIMER) |
| `color.success.fg` | Green | confirmation result labels (Bip38ConfValidator) |
| `color.validate.ok.bg` | LightGreen | M-of-N part textbox = valid part accepted |
| `color.validate.bad.bg` | Pink | M-of-N part textbox = invalid part |
| `color.validate.neutral.bg` | White | M-of-N part textbox = empty/reset |

### 1.2 Typography

| Token | Value | Usage |
| --- | --- | --- |
| `font.ui.default` | OS UI font ≈ 8.25pt (Segoe UI / MS Sans Serif) | all controls |
| `font.label.emphasis` | default + Bold | result labels (Bip38ConfValidator) |
| `font.label.link` | default + Underline | LinkLabels |
| `font.symbol.narrow` | Arial Narrow 8.25pt | ฿/Ƀ currency-symbol link buttons (PrintVouchers) |

Print-only fonts (Reports, §6): `Arial` 6/10/13; `Courier New` 4.5/10/13 (coin inserts); `Ubuntu` 6/9/17 (loaded from bundled `Ubuntu-R.ttf`, fallback if absent).

### 1.3 Spacing / layout scale

| Token | Value | Notes |
| --- | --- | --- |
| `space.window.margin` | 12px | dialog edge inset |
| `space.row.pitch` | ~26px | vertical gap between stacked label+field rows |
| `space.label.col.x` | 5–19px | left label column origin |
| `space.field.col.x` | ~100–151px | input column origin (label width ≈ 90–135px) |
| `space.arrow.btn` | 46×30px | conversion arrow buttons (Form1) |
| `border.radius` | 0 | source has no rounded corners (square controls) |
| `z.menu` | top | MenuStrip docked top |
| `z.status` | bottom | StatusStrip docked bottom |
| `z.content` | fill | center region |

### 1.4 Responsive breakpoints

**N/A — source is fixed-size desktop windows, single layout, no reflow, no mobile.** Target is desktop-only (per migration constraint: Windows desktop, no cross-platform/mobile). Do NOT invent breakpoints. Windows are either fixed-size dialogs or a single resizable main window (`KeyCollectionView`, where only the central list grows). Resize behavior is encoded per-control as Anchor (see §4).

---

## 2. ABSTRACT COMPONENT HIERARCHY & WIRE-FRAME

Syntax: `SemanticName [props/inputs | internal UI state | dispatched events]`. Window titles quoted. `→handler` = source method (keep logic). Each window is an independent top-level surface.

### 2.1 MainCollectionWindow — "Bitcoin Key Collection"  (entry; `KeyCollectionView`, resizable, 629×434)

```text
MainCollectionWindow [bound: KeyCollection model | sortOrder: Asc|Desc | onLoad, onMenuMouseMove→entropy]
├─ MenuBar [ | | ]
│  ├─ Menu "Tools"
│  │  ├─ Item "Address Utility"            →ShowAddressUtility
│  │  ├─ Item "Base58 Calculator"          →ShowBase58Calc
│  │  ├─ Item "Key Decrypter"              →ShowKeyDecrypter
│  │  ├─ Item "M-of-N Calculator"          →ShowMofNcalc
│  │  ├─ Submenu "Two-Factor Bitcoin Tools"
│  │  │  ├─ Item "Intermediate Code Generator"   →ShowIntermediateGen
│  │  │  ├─ Item "Confirmation Code Validator"   →ShowConfValidator
│  │  │  ├─ Item "Key Combiner"                  →ShowKeyCombiner
│  │  │  └─ Item "Key Decrypter"                 →ShowKeyDecrypter
│  │  └─ Item "Escrow Tools"               →ShowEscrowTools
│  ├─ Menu "&File"
│  │  ├─ Item "Clear All"                  →clearAll (confirm dialog)
│  │  ├─ Item "Save" [HIDDEN]
│  │  └─ Item "Exit"                       →close
│  ├─ Menu "&Address"
│  │  ├─ Item "New address"                →newAddress (creates KeyPair, appends)
│  │  ├─ Item "Generate addresses"         →generateKeys (opens AddressGen modal)
│  │  └─ Item "Enter an address/key"       →enterAddress (opens AddSingleAddress modal)
│  └─ Menu "&Selection"
│     ├─ Item "Select All"                 →checkAll
│     ├─ Item "Deselect All"               →uncheckAll
│     ├─ ─────
│     ├─ Item "Print Banknote Vouchers"            →printVouchers (opens PrintVouchers modal)
│     ├─ Item "Print Physical Bitcoin Inserts"     →printCoinInserts (CoinInsert)
│     ├─ Item "Print Physical Bitcoin Inserts - Dense" →printCoinInserts (CoinInsertDense; shared handler)
│     ├─ Item "Print Paper Wallets" [HIDDEN]
│     ├─ ─────
│     ├─ Item "Save Address List"              →saveAddrList (SaveFileDialog .txt)
│     ├─ Item "Save Address List with PrivKey" →saveAddrListPriv (SaveFileDialog .txt)
│     ├─ ─────
│     └─ Item "Delete Selected Items"          →deleteSelected (confirm dialog)
├─ DataGrid "listView1" [items: KeyCollectionItem[]; eachRow.Tag=item | checkedRows, sortColumn, sortOrder | onColumnClick→toggleSort, onItemActivate→openDetails]
│  ├─ col "Address"     width 400 (runtime; designer 480)
│  ├─ col "Private Key" width 100   (= PrivateKeyKind text)
│  └─ col "Balance"     width default
│  · row checkbox per item (CheckBoxes=true), MultiSelect, View=Details(grid), full-row activate
│  · ContextMenu: Item "Details" →openDetails (= ItemActivate)
└─ StatusBar [ | statusText | ] — default "Click Address to generate some addresses."; updates on add/delete
```

Model events (subscribed on load): `KeyCollection.ItemAdded` → append row; `ItemsAdded` → bulk append; `ItemsDeleted` → remove rows by Tag match. Target: bind grid to observable collection.

### 2.2 AddressUtilityWindow — "Bitcoin Address Utility by Casascius (Beta, No Warranty)"  (`Form1`, 670×391)

Core conversion workbench. Vertical stack of labeled field-rows; each row has a text field + one or two arrow buttons that transform DOWN (▼/▼▼) or UP (▲) the key-derivation chain. See §3 for the cascade-grey + change-flag mechanics.

```text
AddressUtilityWindow [ | changeFlag(int reentrancy guard), spaceBetweenHexBytes:bool, compress:bool | onKeyDown→entropy, onMouseMove→entropy, timerTick→entropy]
├─ MenuBar
│  ├─ Menu "Tools": "Base58 Calc"→new Base58Calc; "Key Combiner"→new KeyCombiner; "M-of-N Calc"→new MofNcalc; "PPEC Keygen"→new PpecKeygen
│  ├─ Menu "Edit": "Copy Minikey QR"; "Copy Private Key QR"; "Copy Public Hex QR"; "Copy Address QR"  (each →encode field as QR bitmap, put text+image on clipboard)
│  ├─ Menu "Options": "Space between hex bytes" [checkable] →toggle hex spacing reformat
│  └─ Menu "Public Key": "Compress" [checkable]; "Compress public key"→GetCompressed; "Uncompress public key"→GetUncompressed; "Show fields"→select X-coord substring
├─ Field "Minikey / key from SHA256 hash of a string" [txtMinikey | grey-state | onChange→cascadeGrey, onEnter→sha256ToPrivate]
│  ├─ Btn "▼▼" →sha256ToPrivate (derive hex from passphrase, then cascade down full chain)
│  ├─ Btn "Generate Minikey" →genMinikey (random MiniKeyPair → fill all)
│  ├─ Label "Warning: Not safe" [color.warning.fg, hidden until weak minikey]
│  └─ Link "Why not?" [hidden] →msgbox security explanation
├─ Field "Private Key (WIF)" [txtPrivWIF | grey-state | onChange→cascadeGrey, onEnter→wifToHex]
│  ├─ Field "Encryption phrase for private key" [txtPassphrase]   (used to BIP38-encrypt on Hex→WIF)
│  ├─ Btn "▲"  →privHexToWIF (up: hex→WIF, optionally encrypt w/ passphrase)
│  ├─ Btn "▼▼" →privWIFToHex (down: WIF→hex, decrypt if encrypted; derive pub+addr)
│  └─ Btn "Generate Address" →generate (random KeyPair → fill all)
├─ Field "Private Key (Hex)" [txtPrivHex | grey-state | onChange→cascadeGrey]
│  └─ Btn "▼▼" →privToPub (hex→pubhex+hash+addr)
├─ Field "Public Key (Hex)" [txtPubHex, MULTILINE | grey-state | onChange→cascadeGrey]
│  └─ Btn "▼▼" →pubHexToHash (pubhex→hash+addr)
├─ Field "Public Key (Hash)" [txtPubHash | grey-state | onChange→cascadeGrey]
│  ├─ Dropdown "cboCoinType" [items: Bitcoin|Testnet|Namecoin|Litecoin; default Bitcoin | | onChange→reDeriveAddress]  (type byte: 0|111|52|48)
│  ├─ Btn "▲" →addressToPubHash (up: validate addr checksum, extract hash; offer brute-force typo correction)
│  └─ Btn "▼" →pubHashToAddress (hash+coinType→address)
└─ Field "Address" [txtBtcAddr | grey-state | onChange→cascadeGrey]
```

### 2.3 Base58CalcWindow — "Base 58 Calculator"  (`Base58Calc`, 685×123)

```text
Base58CalcWindow [ | useChecksum:bool=true | ]
├─ MenuBar > Menu "Mode" > Item "Use Checksum" [checkable, default ON] →toggle + recompute focused field
├─ FieldRow Label "Hex"    + [txtHex    | onChange(if focused)→encode→txtBase58]
├─ FieldRow Label "Base58" + [txtBase58 | onChange(if focused)→decode→txtHex; null→"invalid"]
└─ Label "Byte count: 0  Base58 length: 0"  (live counts)
```

Behavior: live bidirectional conversion driven by which field has focus. Checksum ON → `Base58Check`; OFF → raw `Base58`.

### 2.4 AddressGenDialog — "Generate Addresses"  (`AddressGen`, MODAL, 384×254)

```text
AddressGenDialog [→ returns GeneratedItems[] | genChoice:enum, generating:bool, stopRequested:bool | onClosing→confirmAbort]
├─ Label "Number of addresses to generate" + Spinner "numGenCount" [min1 max99999 default8]
├─ RadioGroup "Private key type"
│  ├─ Radio "MiniKey" [default ON]
│  ├─ Radio "Full-length (WIF)"
│  ├─ Radio "Passphrase Encrypted"
│  ├─ Radio "Two-Factor Encrypted"
│  └─ Radio "Deterministic (WIF)"      →onChange: show/hide seed field + retain-priv checkbox; for Two-Factor scan clipboard for intermediate codes
├─ Checkbox "Retain unencrypted private key" [HIDDEN unless Encrypted selected]
├─ Label "Seed for deterministic generation" [HIDDEN] + [txtTextInput, HIDDEN]   (label text swaps per mode)
├─ Btn "Generate Addresses" →start/stop toggle (text → "Cancel"/"Stopping..." during run)
└─ StatusBar [statusText + ProgressBar(hidden until running)]
```

States: idle → generating (controls disabled, progress visible, button="Cancel", background thread + 250ms poll timer) → done (re-enable, reset). Status shows "Keys generated: N" or "Hashing passphrase…" during slow BIP38 scrypt.

### 2.5 MofNCalcWindow — "MofNcalc"  (`MofNcalc`, 618×365)

```text
MofNCalcWindow [ | targetPrivKey:bytes? | onLoad→experimentalWarning]
├─ Label "Parts to require (m)"  + Spinner "numPartsNeeded"     [min1 max8 default3]
├─ Label "Parts to generate (n)" + Spinner "numPartsToGenerate" [min1 max8 default5]
├─ Btn "Generate m-of-n with new random key" →generate (validate m≤n; fill parts 1..n; show privkey+addr)
├─ FieldList Part1..Part8  [txtPart1..8 | bg: White|LightGreen|Pink per validity | ]   (only n visible-populated)
├─ Btn "Generate m-of-n for specific private key" →genSpecific (parse txtPrivKey, reuse generate)
├─ Btn "Decode m-of-n" →decode (read non-empty parts, color each, decode if ≥m valid)
├─ Label "Private key" + [txtPrivKey]
└─ Label "Address"     + [txtAddress, read-only]
```

### 2.6 IntermediateGenWindow — "Intermediate Passphrase Generator"  (`PpecKeygen`, 832×204)

```text
IntermediateGenWindow [ | | ]
├─ Label (multiline explanation of BIP38 intermediate codes)
├─ Label "Passphrase" + [txtPassphrase, anchor L+R]
├─ Btn "Encode passphrase" + Label "Intermediate Passphrase Code" + [txtPassphraseCode, anchor L+R]   →encode (Bip38Intermediate; SLOW scrypt, blocks UI)
└─ Label "Generating passphrases and encrypting and decrypting keys is slow by design. Expect several seconds delay."
```

### 2.7 KeyCombinerWindow — "Key Combiner"  (`KeyCombiner`, 604×463)

```text
KeyCombinerWindow [ | | ]
├─ Label (multiline info)
├─ RadioGroup "Combining Method"
│  ├─ Radio "EC Multiplication (two-factor physical Bitcoins) (most secure)" [default ON]
│  ├─ Radio "EC Addition (for use only with Vanity Address Pool)"
│  └─ Link "(why?)" →msgbox add-vs-multiply explanation
├─ Label "Input Key 1" + [txtInput1, MULTILINE]
├─ Label "Input Key 2" + [txtInput2, MULTILINE]
├─ Btn "Combine" →combine (parse each as priv/pub; EC add|mul; populate outputs)
├─ Label "Resulting Bitcoin Address"  + [txtOutputAddress, read-only]
├─ Label "Resulting Public Key Hex"   + [txtOutputPubkey, MULTILINE read-only]
└─ Label "Resulting Private Key"       + [txtOutputPriv, read-only | "Only available when combining two private keys"]
```

### 2.8 DecryptKeyWindow — "Key Decrypter for Encrypted/Two-Factor Keys"  (`DecryptKey`, 744×104)

```text
DecryptKeyWindow [ | | ]
├─ Label "Enter an encrypted key"            + [txtEncrypted]   (strips "-" and " " on submit)
├─ Label "Enter the passphrase or second factor" + [txtPassphrase]
└─ Btn "Decrypt" →decrypt
```

Decrypt outcome routes a successful result into AddressUtilityWindow via `DisplayKeyCollectionItem`. Many branch dialogs (§3).

### 2.9 ConfValidatorWindow — "Confirmation Code Validator"  (`Bip38ConfValidator`, 815×268, AcceptButton=Confirm)

```text
ConfValidatorWindow [ | resultVisible:bool | ]
├─ Label (multiline info)
├─ Label "Passphrase"        + [txtPassphrase]
├─ Label "Confirmation code" + [txtConfCode]
├─ Btn "Confirm" →confirm (validate cfrm38 code vs passphrase via scrypt+EC)
├─ Label "Bitcoin address:"  [Bold, HIDDEN until success]
├─ Label <address>           [Bold, color.success.fg, HIDDEN until success]
└─ Label "It is confirmed that this Bitcoin address depends on this passphrase." [Bold, color.success.fg, HIDDEN until success]
```

### 2.10 EscrowToolsWindow — "Escrow Tools"  (`EscrowTools`, 755×452, TabControl)

```text
EscrowToolsWindow [ | activeTab | onLoad→load RTF into tabHowItWorks]
├─ Tab "How it works"   > RichText (read-only, borderless) + Link "DISCLAIMER OF WARRANTY" →msgbox
├─ Tab "Be a Payee"
│  ├─ [txtPayeeCode] + Btn "Generate Payment Invitation" →genPayee
│  ├─ [txtPayeeGeneratedInvite] + Btn "Save" + Btn "Print"
│  └─ [txtPayeeGeneratedAddress]
├─ Tab "Be a Payer"
│  ├─ [txtPayerCode1] + [txtPayerCode2]
│  ├─ Btn "Done"↔"Reset" →payerDone (toggle; reveal/hide result block)
│  └─ [REVEAL group, hidden until Done] Label + [txtPayerAddress] + Btn "Save" + Btn "Print"
├─ Tab "Be an Escrow Agent"
│  ├─ Btn "Generate Escrow Invitation" →genEscrow
│  ├─ [txtEscrowForPayer] + [txtEscrowForPayee]
│  └─ Btn "Save" + Btn "Print" [hidden until generated]
└─ Tab "Collect your funds"
   ├─ [txtRedeemCode1] + [txtRedeemCode2] + [txtRedeemCode3]
   ├─ Btn "Redeem" →redeem
   ├─ Label "The Bitcoin address is:" + [txtRedeemAddress]
   └─ Label "The code (private key)…" + [txtRedeemPrivKey]
```

Tab order in source: How it works is declared index 4 but presented first conceptually; preserve presentation order: How it works → Be a Payee → Be a Payer → Be an Escrow Agent → Collect your funds.

### 2.11 PaperWalletPrinterWindow — "Paper Wallet Printer"  (`PaperWalletPrinter`, 828×235, dual-panel)

```text
PaperWalletPrinterWindow [ | addresses[], currentlyGenerating:bool, currentSequence, currentPassphrase, selectionPrinted:bool, selectionSaved:bool | timerTick→genStep]
├─ Panel-LEFT GroupBox "Address Generation Options"
│  ├─ Label "Number of addresses to generate" + Spinner "numGenCount" [min1 max99999 default8]
│  ├─ Checkbox "Use MiniKey format" [default OFF]
│  ├─ RadioGroup: Radio "Randomly generated wallet" [default ON] | Radio "Deterministic wallet"  →onChange swap input-label + seed text
│  ├─ Label(dynamic: entropy prompt | "Passphrase") + [txtPassphrase]
│  ├─ Btn "Generate Addresses"↔"Stop generating" →genToggle (timer-driven async)
│  └─ Label "N addresses have been generated." (dynamic)
└─ Panel-RIGHT GroupBox "Printing Style"
   ├─ RadioGroup: Radio "8 wallets per page, Load/Spend QR codes" [default ON, =PubPrivQR] | Radio "16 wallets per page, Spend QR code only" [=PrivQR]
   ├─ Btn "Print the wallet" →print (PrintDialog → QRPrint)
   └─ Btn "Sort the keys by Bitcoin address" →sort
```

`LockButtons(true)` disables Sort + Print during generation.

### 2.12 PrintVouchersDialog — "Print {N} Vouchers"  (`PrintVouchers`, MODAL, 292×222)

```text
PrintVouchersDialog [in: Items:KeyCollectionItem[]; out: PrintAttempted:bool | | onLoad→title+default style+conditional checkbox]
├─ Label "Vouchers to print per page" + Spinner "numVouchersPerPage" [min1 max3 default3]
├─ Label "Artwork style" + Dropdown "cboArtworkStyle" [Yellow|Green|Blue|Purple|Greyscale; default Yellow]
├─ Label "Denomination to print" + [txtDenomination] + Link "฿" + Link "Ƀ"   (links append symbol to field)
├─ [HIDDEN controls: comboBox2 barcode-type, comboBox3 address-encoding, label1, label5 — present but invisible; do NOT render]
├─ Checkbox "Print unencrypted version of private keys" [shown only if any item has an unencrypted key]
└─ Btn "Print" →print (PrintDialog → QRPrint PsyBanknote; ImageFilename = note-{style}.png)
```

### 2.13 AddSingleAddressDialog — "Add Single Address" / "Add Multiple Addresses"  (`AddSingleAddress`, MODAL, 529×87 → grows, AcceptBtn=OK CancelBtn=Cancel)

```text
AddSingleAddressDialog [out: Result:object | multiMode:bool | ]
├─ Label "Enter an address or key (any format)"  (text swaps in multi mode)
├─ [textBox1 | single-line → multiline in multi mode | onOK validate+interpret]
├─ Btn "Add multiple addresses" →goMulti (textbox→multiline, hide self, grow window ≥500h, clear AcceptButton, retitle)
├─ Btn "OK"     [DialogResult=OK]   →interpret (single: Interpret; multi: InterpretBatch)
└─ Btn "Cancel" [DialogResult=Cancel]
```

---

## 3. INTERACTION & BEHAVIORAL LOGIC MATRIX

### 3.1 AddressUtilityWindow — cascade-grey + reentrancy guard (CRITICAL, replicate exactly)

- **Stale-marking:** When the USER edits any field, every OTHER field's text color → `color.text.disabled` (grey). Signals "these values no longer correspond to what you typed." A field returns to `color.text.default` only when the PROGRAM writes it (via a conversion).
- **Reentrancy guard:** An integer `changeFlag` counter brackets all programmatic field writes. `onChange` handlers no-op while `changeFlag > 0` (program is writing), preventing the grey-cascade from firing on its own updates. Target MUST replicate this guard or conversions will recursively grey/clear.
- **Hex spacing:** Hex fields strip spaces unless `Options ▸ Space between hex bytes` is checked; toggling reformats existing hex fields.
- **Arrow semantics:** ▼/▼▼ = derive downward (priv→pub→hash→addr). ▲ = derive upward (reverse, lossy where applicable). `sha256ToPrivate` runs the WHOLE downward chain in sequence.
- **Enter-key shortcuts:** Enter in `txtMinikey`→▼▼ sha256ToPrivate; Enter in `txtPrivWIF`→▼▼ wifToHex.
- **Minikey weak warning:** On minikey input, `IsValidMiniKey` → {1 valid, -1 invalid, 0 not-minikey}; `PassphraseTooSimple` toggles red "Warning: Not safe" + "Why not?" link visibility.

### 3.2 Conditional rendering / state machines

**AddressGen modal:**

```mermaid
stateDiagram-v2
  [*] --> Idle
  Idle --> Validating: click Generate
  Validating --> Idle: missing seed/passphrase/clipboard-code (warn dialog)
  Validating --> Generating: inputs OK (disable controls, start thread+timer, btn="Cancel")
  Generating --> Generating: timer 250ms → update "Keys generated: N" / "Hashing passphrase…" + progress
  Generating --> Stopping: click Cancel (stopRequested=true, btn="Stopping…")
  Stopping --> Done
  Generating --> Done: count reached
  Done --> Idle: re-enable controls, hide progress, btn="Generate Addresses"
  Generating --> ConfirmAbort: window close
  ConfirmAbort --> Generating: No
  ConfirmAbort --> [*]: Yes (join thread; offer keep-N)
```

**PaperWalletPrinter generation:** mirror — idle ↔ generating toggled by one button; timer tick generates one key per tick until `currentSequence ≥ total`; `LockButtons` gates Sort/Print.

**EscrowTools "Be a Payer" reveal:** Button toggles label "Done"↔"Reset". Done → compute address, reveal {result label, address field, Save, Print}, possibly same-party warning. Reset → hide block, relabel "Done".

**Bip38ConfValidator result:** Three result labels hidden by default; shown together only on successful confirmation. Any failure → MessageBox, labels stay hidden.

**KeyCombiner output:** Private-key output box shows actual key only when both inputs are private; otherwise literal "Only available when combining two private keys".

### 3.3 Validation & constraints

| Window | Constraint |
| --- | --- |
| MofNcalc | m∈[1,8], n∈[1,8] (spinner-bounded), enforce m≤n on generate; parts trimmed; ≥m valid parts to decode; part bg color reflects per-part validity |
| AddressGen | count∈[1,99999]; seed required if Deterministic; passphrase required if Encrypted; clipboard must contain valid intermediate code(s) if Two-Factor |
| PaperWalletPrinter | random mode requires passphrase length ≥30 (warn); deterministic warns if length <30 or unchanged-since-last; confirm before discarding unsaved generated addresses |
| PrintVouchers | perPage∈[1,3]; style∈{Yellow,Green,Blue,Purple,Greyscale} |
| Base58Calc | invalid hex → field shows "invalid"; no hard restriction |
| Form1 | address auto-correct: brute-force each Base58 char position to fix checksum (offered via Yes/No on invalid address); WIF decrypt requires passphrase if encrypted |
| DecryptKey | strip "-"/" " from key; both fields required; detects "cfrm38" prefix → suggests Confirmation Validator |
| Bip38ConfValidator | both fields required; code must Base58Check-decode to 51 bytes with magic prefix `64 3B F6 A8 9A`, byte[18]∈{0x02,0x03}; address-hash must match |
| AddSingleAddress | non-empty; Interpret (single) / InterpretBatch (multi) |
| All key-parse paths | wrap parse in try/catch → MessageBox(ex.Message); never crash on bad input |

### 3.4 Dialog inventory (MessageBox / file / print)

Preserve these as native target dialogs. Icons: Info / Warning(Exclamation) / Error / Question(Yes-No).

- **Confirmations (Yes/No or OK/Cancel):** Clear All; Delete Selected; discard-unsaved-addresses (PaperWalletPrinter); abort-generation (AddressGen close); deterministic passphrase weak/unchanged; address typo auto-correct (Form1); duplicate-key-hash continue (KeyCombiner/DecryptKey); "looks like a confirmation code, open validator?" (DecryptKey); reprint-already-printed (PaperWalletPrinter).
- **Errors/warnings (OK):** "No items … selected" (print/save with empty selection); "cannot be printed because the private key is not known" (partial); invalid private key / address / public key / minikey; "passphrase is incorrect"; "passphrase is required"; "Not enough valid parts…"; "Number of parts needed exceeds…"; experimental-feature warning (MofNcalc load); security explanations (Form1 "Why not?", KeyCombiner "(why?)", Escrow disclaimer); generic `ex.Message`.
- **Success (OK):** "Decryption successful"; "EC multiplication successful"; confirmation-valid (shown inline, not dialog).
- **File dialogs:** SaveFileDialog filter `Text files (*.txt)|*.txt|All files (*.*)|*.*` (Save Address List ±privkey).
- **Print:** native PrintDialog before every print job (paper wallets, vouchers, coin inserts).

---

## 4. LAYOUT MECHANICS & UX ALIGNMENT

### 4.1 Positioning blueprints

- **MainCollectionWindow:** Dock layout — Menu (top), StatusBar (bottom), DataGrid fills center. Only the grid resizes with window (anchored all sides). Target: BorderLayout / DockPanel equivalent.
- **AddressUtilityWindow:** Single vertical column of label+field rows; right-edge "gutter" hosts the ▲/▼ arrow buttons (46×30) and the two wide action buttons ("Generate Address/Minikey", 114-wide). Fields stretch full content width. Target: 3-zone grid per row — [label | field(grow) | button gutter].
- **Base58Calc / PpecKeygen / DecryptKey / Bip38ConfValidator:** Simple form — left label column + right field column, fields anchored L+R to stretch on horizontal resize.
- **MofNcalc:** Top control bar (m, n spinners + generate button) over a stacked list of 8 part fields, then private-key/address fields + decode/generate-specific buttons.
- **KeyCombiner:** Vertical: info → method radio group + link → Input1 (multiline) → Input2 (multiline) → Combine button → three result rows.
- **AddressGen:** Two-column: left = count + dynamic seed area; right = `GroupBox` radio group "Private key type"; bottom = action button + status/progress strip.
- **EscrowTools:** TabControl filling window with `space.window.margin`; RichText tab anchors all-sides; per-tab fields anchor L+R (stretch horizontally).
- **PaperWalletPrinter:** Two side-by-side `GroupBox` panels (left = generation, right = printing style). Target: 2-column grid, each column a titled group.
- **PrintVouchers / AddSingleAddress:** Compact modal; AddSingleAddress grows height (≥500) when switching to multi-line batch mode.

### 4.2 Alignment / overflow

- Labels left-aligned to a shared left column; inputs share a left field-edge.
- Multiline fields: `txtPubHex` (Form1), `txtInput1/2`/`txtOutputPubkey` (KeyCombiner), batch box (AddSingleAddress) — wrap/scroll vertically.
- DataGrid: column widths fixed at runtime (Address 400, PrivKey 100); horizontal overflow scrolls; rows checkable; sortable by header click (toggle Asc/Desc).
- Currency symbol links (฿/Ƀ) sit inline adjacent to the denomination field.
- Hidden controls (several in PrintVouchers, the HIDDEN menu items / "Save"/"Print Paper Wallets") MUST NOT be rendered — they are dead/disabled UI. Listed for fidelity only.

### 4.3 Accessibility parity (source had near-zero explicit a11y; target SHOULD add, at minimum match)

- **Roles:** menubar/menuitem; grid with row checkboxes (row role + checkbox); textbox/spinbutton/combobox/radiogroup/radio/checkbox/button/tab/tabpanel/statusbar/progressbar; link controls expose link role.
- **Labels:** every input has an adjacent visible label — associate programmatically (label-for / accessible name) on target; source relied on proximity only.
- **Keyboard:** menu mnemonics preserved (`&File`, `&Address`, `&Selection`). Enter submits per §3.1 (minikey/WIF rows) and dialog AcceptButton (AddSingleAddress=OK, Bip38ConfValidator=Confirm); Esc = CancelButton (AddSingleAddress=Cancel). Tab order follows declared TabIndex per window — preserve logical order. Spinners arrow-key increment within bounds.
- **Focus:** modal dialogs trap focus and return result to caller; tool windows are independent (no trap).
- **State announcement:** progress (AddressGen/PaperWalletPrinter), validation color changes (MofNcalc parts, Form1 grey), and result reveal (Bip38ConfValidator) should also convey state non-visually on target (color alone is insufficient — add text/aria-live).
- **Password fields:** NOTE source uses PLAIN textboxes for passphrases (no masking) — see §5 Don'ts. Target SHOULD offer optional masking but MUST keep paste working and not silently transform input.

---

## 5. MIGRATION & COHERENCE GUARDRAILS

### 5.1 Feature-parity checklist (target must satisfy ALL)

- [ ] Entry window = Key Collection (not the Address Utility).
- [ ] Singleton window manager: each tool opens at most one instance; re-invoke focuses existing.
- [ ] Tool results route back into Address Utility via a `DisplayKeyCollectionItem`-equivalent.
- [ ] All 14 windows present with identical titles, fields, labels, button captions, menu text/order/mnemonics.
- [ ] Address Utility cascade-grey + change-flag reentrancy guard reproduced (no recursive update loops).
- [ ] Arrow-button derivation directions (▼ down, ▲ up) and full-chain `sha256ToPrivate` reproduced.
- [ ] Coin-type dropdown {Bitcoin,Testnet,Namecoin,Litecoin} re-derives address; correct type bytes.
- [ ] Base58Calc live bidirectional convert keyed on focus; checksum toggle recomputes.
- [ ] MofNcalc m≤n enforcement; per-part bg validity colors (green/pink/white); decode threshold.
- [ ] AddressGen 5 key types; deterministic/encrypted seed gating; two-factor clipboard scan; background generation w/ progress + cancel + abort-confirm + keep-N.
- [ ] PaperWalletPrinter dual-panel; timer-driven gen; print-mode radios → QRPrint PubPrivQR (8/pg) / PrivQR (16/pg); sort; reprint warning; passphrase-length guards.
- [ ] EscrowTools 5 tabs; payer reveal toggle (Done/Reset); per-tab generate/save/print; RTF "how it works"; disclaimer link.
- [ ] DecryptKey + Bip38ConfValidator + KeyCombiner + PpecKeygen flows incl. all branch dialogs and inline success labels.
- [ ] Slow crypto (scrypt: PpecKeygen, AddressGen 2FA/encrypted, Bip38ConfValidator) keeps UI responsive OR shows explicit "slow by design" messaging; never appears hung silently.
- [ ] All key-parse paths wrapped → user-facing error dialog, never crash.
- [ ] Entropy taps (mouse-move/keydown/timer) into `ExtraEntropy` preserved.
- [ ] Edit-menu "Copy … QR" puts BOTH text and QR image on clipboard.
- [ ] Save Address List (±privkey) → .txt via save dialog, format unchanged.
- [ ] Printing (paper wallet 8/16-per-page, vouchers 1–3/page w/ artwork+denomination, coin inserts normal+dense) reproduced via target's document/printing API (see §6).
- [ ] Native PrintDialog shown before each print job.
- [ ] Hidden/dead controls NOT rendered.
- [ ] Offline: zero network calls introduced.

### 5.2 Strict don'ts (source anti-patterns — do NOT carry over)

- **No absolute pixel positioning.** Source uses fixed `Location`/`Size`. Target MUST use flow/grid/dock layout. Pixel values here are proportion hints only.
- **No business/crypto logic in print-page callbacks or event handlers blindly.** Source mixes layout + drawing + key handling in code-behind (`OnPrintPage`, button handlers). Keep crypto calls intact but separate presentation; do not introduce new logic into Model.
- **Don't run scrypt/keygen on the UI thread without feedback.** Source blocks synchronously in PpecKeygen/MofNcalc/Bip38ConfValidator (UI freezes). Prefer async + progress on target; at minimum keep the "slow by design" notice.
- **Don't drop the change-flag guard.** Without it the Address Utility's mutual field updates recurse.
- **Don't leave passphrase fields' behavior to chance** — source uses unmasked plain textboxes; do not "modernize" into a masked box that breaks paste or transforms input; if masking, make it toggleable and paste-safe.
- **Don't preserve dead UI.** Hidden controls (PrintVouchers barcode/encoding combos, hidden "Save"/"Print Paper Wallets" menu items) and empty no-op handlers (`label3/4/5_Click`, `textBox9_TextChanged`, `menuStrip1_ItemClicked`, empty `printPaperWallets`) must not be reimplemented.
- **Don't rely on `item.Tag` smuggling.** Source stashes `KeyCollectionItem` in each grid row's `Tag`. Target should bind rows to model objects directly.
- **Don't assume single shared static print state.** `PCPrint` uses a `static curChar` pagination cursor — not reentrant. Re-implement paginators as instance state.
- **Don't depend on clipboard for primary data flow** beyond the explicit Two-Factor intermediate-code scan and the Copy-QR actions.
- **Don't hardcode the Ubuntu font as required** — source falls back if `Ubuntu-R.ttf` absent; keep graceful fallback.

---

## 6. PRINTING / DOCUMENT-GENERATION LAYER (`Reports/`)

Source: subclasses of `System.Drawing.Printing.PrintDocument`, drawing per page in `OnPrintPage`. Migrate to target's document/paginator API. Each renders one logical "item" repeated in a grid; portrait; uses printer default margins/paper unless noted.

| Class | Mode / trigger | Items/page | Grid | Per-item content |
| --- | --- | --- | --- | --- |
| `QRPrint` PrivQR | PaperWallet "16/page, spend only" | 16 | 1 col × 16 (120px rows) | private-key QR 100×100, address text, privkey text, optional denomination |
| `QRPrint` PubPrivQR | PaperWallet "8/page, load+spend" | 8 | 2 col × 4 | public QR + private QR (100×100 each), address, right-justified privkey |
| `QRPrint` PsyBanknote | Print Vouchers | 1–3 (`NotesPerPage`) | stacked, 365px | banknote PNG bg `note-{style}.png` scaled to 550w; 90°-rotated address + privkey (Ubuntu fonts); public QR 128×128; private QR 145×147; denomination (Ubuntu big); Code128 barcode if MiniKey + flag; "Password Required" prefix if BIP38 |
| `CoinInsert` | "Physical Bitcoin Inserts" | 8 | 1 col × 8 (120px) | private key formatted inside a 7/16″ circle (line char-pattern 4-7-8-7-4 / blank / 4-7-8-7-4, "--" fold mark >30 chars, Courier 4.5pt centered, double-circle if long); address QR 100×100; address text; if BIP38: confirmation QR + right-justified conf text |
| `CoinInsertDense` : CoinInsert | "…Inserts - Dense" | 96 | 6 col × 16 (130×60) | same circle key; alignment rectangles at item 0 (laser-cut marks); public QR 50×50; 90°-rotated address (3×12 chars); no confirmation handling |
| `PCPrint` | generic text print | variable | full-page text | plain text pagination by `font.Height`; respects Landscape; **`static curChar` cursor — make instance state on migration** |

**Inputs forms set before printing:** `keys: KeyCollectionItem[]`, `PrintMode`, `Denomination`, `ImageFilename` (`note-{yellow|green|blue|purple|greyscale}.png`), `NotesPerPage`, `PrintMiniKeysWith1DBarcode`, `PreferUnencryptedPrivateKeys`. **Runtime assets to bundle:** `note-*.png`, `Ubuntu-R.ttf` (graceful fallback). QR generation via existing `Barcode/QR.cs` (QRCoder); Code128 via `Barcode/Barcode128b.cs`. Bitmap→target-image conversion is the main porting seam.

---

## 7. WINDOW INDEX (quick map: semantic ↔ source class ↔ namespace)

| Semantic window | Source class | Namespace | Modality | Size (px) |
| --- | --- | --- | --- | --- |
| MainCollectionWindow | `KeyCollectionView` | `BtcAddress.Forms` | main (resizable) | 629×434 |
| AddressUtilityWindow | `Form1` | `BtcAddress` | modeless | 670×391 |
| Base58CalcWindow | `Base58Calc` | `BtcAddress` | modeless | 685×123 |
| AddressGenDialog | `AddressGen` | `BtcAddress.Forms` | modal | 384×254 |
| MofNCalcWindow | `MofNcalc` | `BtcAddress` | modeless | 618×365 |
| IntermediateGenWindow | `PpecKeygen` | `BtcAddress` | modeless | 832×204 |
| KeyCombinerWindow | `KeyCombiner` | `BtcAddress` | modeless | 604×463 |
| DecryptKeyWindow | `DecryptKey` | `BtcAddress.Forms` | modeless | 744×104 |
| ConfValidatorWindow | `Bip38ConfValidator` | `BtcAddress.Forms` | modeless | 815×268 |
| EscrowToolsWindow | `EscrowTools` | `BtcAddress.Forms` | modeless | 755×452 |
| PaperWalletPrinterWindow | `PaperWalletPrinter` | `BtcAddress` | modeless | 828×235 |
| PrintVouchersDialog | `PrintVouchers` | `BtcAddress.Forms` | modal | 292×222 |
| AddSingleAddressDialog | `AddSingleAddress` | `BtcAddress.Forms` | modal | 529×87 (grows) |
| (printing) | `QRPrint`/`CoinInsert`/`CoinInsertDense`/`PCPrint` | `BtcAddress` / `PC` | — | — |

> NOTE: namespaces do not match folders (forms split across `BtcAddress` and `BtcAddress.Forms`). Target should consolidate under one view namespace.
