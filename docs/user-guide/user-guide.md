# Bitcoin Address Utility — User's Guide

Welcome! This guide walks you through the Bitcoin Address Utility, a free desktop tool for creating and managing your own Bitcoin keys, addresses, and paper wallets. Whether you're setting up cold storage, printing paper wallets to give as gifts, or splitting a key among trusted friends, this guide will show you how — one step at a time. No prior technical background required.

---

## Introduction & Core Value

### What this application does

The Bitcoin Address Utility lets you generate and manage the "keys" that control Bitcoin. Think of a Bitcoin address as a mailbox where money can be received, and the matching private key as the only key that opens that mailbox to spend what's inside. This tool helps you create those mailboxes and their keys safely on your own computer — without ever needing to be online.

### The problems it solves

- **Create Bitcoin storage you fully control.** Generate brand-new addresses and keys that only you hold. Nothing is stored on anyone else's server.
- **Print paper wallets.** Turn your keys into printed sheets with scannable codes — a popular, low-cost way to store Bitcoin offline ("cold storage").
- **Protect keys with a passphrase.** Lock a private key behind a password so that even someone who finds the printout can't spend the funds without your passphrase.
- **Share control safely.** Split a single key into several pieces so that, for example, any 3 out of 5 trusted people are needed to reassemble it.
- **Hold funds in escrow.** Set up a three-party arrangement (buyer, seller, and a neutral agent) for a transaction that needs a middleman.
- **Make physical Bitcoins and gift items.** Print vouchers, coin inserts, and other collectibles that carry real Bitcoin value.

### A word about safety

> **Important:** This program creates and handles private keys — the secret that controls your money. Anyone who sees a private key can take the funds. For maximum safety, run this tool on a computer that is disconnected from the internet ("air-gapped"), and never share or photograph a private key you intend to keep.

---

## Getting Started

### System requirements

- A **Windows 10 or Windows 11** PC (64-bit). This is a Windows-only desktop program; it does not run on Mac, phones, or tablets.
- A **printer**, if you plan to print paper wallets, vouchers, or coin inserts.
- That's it. There is no account to create, no subscription, and no internet connection required to use the core features.

### Installing and launching

1. Obtain the program file, named **BtcAddress.exe**, from a trusted source.
2. Double-click **BtcAddress.exe** to start it. There is nothing to install — it runs directly.
3. The first window to appear is the **Bitcoin Key Collection** — your home base. (You'll learn your way around it in the next section.)

> **Tip:** There is no login screen, username, or password to start the program. Anyone with access to your computer can open it, so keep your computer secure.

> **Heads-up:** Because the program works with cryptography, Windows or your antivirus may ask you to confirm that you want to run it. This is normal for a tool of this kind.

---

## Interface Walkthrough

When the program opens, you land on the **Bitcoin Key Collection** window. This is the main screen where your addresses live and where most journeys begin.

### The main list

The center of the window is a large list with three columns:

- **Address** — the public "mailbox" address.
- **Private Key** — shows what kind of key it is (for example, *MiniKey*) when known.
- **Balance** — starts at *0.00* (this tool does not check live balances).

Each row has a **checkbox** on the left. You'll use these checkboxes to pick which addresses an action applies to (for example, which ones to print or save).

- **Click a column heading** to sort the list by that column.
- **Right-click any row** and choose **Details** to open a full breakdown of that key in the Address Utility window.

At the bottom, a **status bar** shows how many addresses you currently have (for example, "8 addresses").

### The menus

Along the top are five menus:

- **Tools** — opens the specialist windows: **Address Utility**, **Base58 Calculator**, **Key Decrypter**, **M-of-N Calculator**, **Two-Factor Bitcoin Tools**, and **Escrow Tools**.
- **File** — **Clear All** (empties your list) and **Exit**.
- **Address** — the three ways to add keys: **New address**, **Generate addresses**, and **Enter an address/key**.
- **Selection** — actions for the checked rows: select/deselect all, the print options, the save/export options, and **Delete Selected Items**.

Don't worry about memorizing these — the workflows below tell you exactly which menu to use for each task.

---

## Core Workflows & How-To Guides

### How to create a single new address

1. Open the **Address** menu.
2. Click **New address**.
3. A freshly generated address appears in your list, with its checkbox already ticked.

That's it — you now hold a brand-new Bitcoin address and its private key.

### How to generate many addresses at once

1. Open the **Address** menu and click **Generate addresses**. The **Generate Addresses** window opens.
2. In **Number of addresses to generate**, type how many you want (anywhere from 1 to 99,999).
3. Choose a **private key type**:
   - **MiniKey** — a short, compact key format (a good simple default).
   - **Full-length (WIF)** — the standard long key format.
   - **Deterministic (WIF)** — keys built from a "seed" phrase you type, so the same seed always recreates the same keys.
   - **Passphrase Encrypted** — keys locked behind a password (see below).
   - **Two-Factor Encrypted** — keys that require two separate pieces to unlock.
4. If you picked **Deterministic**, type your seed phrase in the box that appears.
5. If you picked an encrypted type, you may tick **Retain unencrypted private key** if you want to keep an unlocked copy as well.
6. Click **Generate Addresses** and watch the progress bar fill.
7. Click **OK** to add them all to your collection.

### How to import or look up an existing key

1. Open the **Address** menu and click **Enter an address/key**. The **Add Single Address** window opens.
2. Paste or type any Bitcoin address or key — in almost any format. The program figures out what it is automatically.
3. Click **OK** to add it to your list, or click **Add multiple addresses** to paste a whole batch at once.

### How to inspect a key in detail and convert between formats

1. Choose **Tools → Address Utility** (or right-click a row and pick **Details**). The **Address Utility** window opens.
2. Paste a key into any field — for example, the **Private Key (WIF)** box — and the related fields fill in automatically: the hex version, the public key, the public key hash, and the final **Bitcoin Address**.
3. Use the small **arrow buttons** between fields to convert in either direction or to reveal each intermediate step.
4. To start fresh, click **Generate Address** for a new random key, or **Generate Minikey** for a short-format key.
5. You can also switch the **coin type** (Bitcoin, Testnet, Namecoin, or Litecoin) if you work with those.

### How to make a QR code

1. Open the **Address Utility** (see above) with the key you want.
2. Open the **Edit** menu.
3. Choose one of the **Copy ... to clipboard as QR code image** options — for the mini key, private key, public key, or address.
4. The QR image is now on your clipboard. Paste it into any document, email, or image editor.

### How to print paper wallets

1. The Paper Wallet Printer is a self-contained tool. Open it and you'll see generation options on the left and printing options on the right.
2. Set **Number of addresses to generate**.
3. Choose **Randomly generated wallet** (the safe default) or **Deterministic wallet**. For deterministic, type a passphrase — *enter some random text with your keyboard to add entropy* (this randomness keeps your keys secure).
4. Optionally tick **Use MiniKey format**.
5. Click **Generate Addresses**. The status line confirms how many were made.
6. On the right, pick a layout:
   - **8 wallets per page, Load/Spend QR codes** — includes codes for both depositing and spending.
   - **16 wallets per page, Spend QR code only** — fits more per page.
7. Click **Print the wallet** to send it to your printer. (You can also click **Sort the keys by Bitcoin address** first to tidy the order.)

### How to protect a key with a passphrase (BIP38)

Passphrase encryption locks a private key behind a password so a printout is useless to anyone who doesn't know it.

1. Use **Generate addresses** (above) and choose **Passphrase Encrypted** as the key type, or use the dedicated two-factor tools below.
2. To later **unlock** an encrypted key:
   - Choose **Tools → Key Decrypter**. The **Key Decrypter** window opens.
   - Paste the encrypted key into **Enter an encrypted key**.
   - Type your password into **Enter the passphrase or second factor**.
   - Click **Decrypt** to reveal the unlocked private key and its address.

### How to verify a confirmation code (BIP38)

1. Open the **Confirmation Code Validator** (under **Tools → Two-Factor Bitcoin Tools**).
2. Type the **Passphrase** you used.
3. Paste the **Confirmation code**.
4. Click **Confirm**. If they match, the Bitcoin address appears in green with the message *"It is confirmed that this Bitcoin address depends on this passphrase."*

### How to set up two-factor keys

This advanced feature lets one person create a key that only another person's passphrase can ultimately unlock — useful for physical Bitcoins.

1. **Make an intermediate code:** Open **Tools → Two-Factor Bitcoin Tools → Intermediate Code Generator**. Type a passphrase, click **Encode passphrase**, and copy the resulting **Intermediate Passphrase Code**. (This step is deliberately slow — expect a few seconds.)
2. **Combine the pieces:** Open **Tools → Two-Factor Bitcoin Tools → Key Combiner**. Paste the intermediate code into **Input Key 1** and the passphrase into **Input Key 2**, choose **EC Multiplication (most secure)**, and click **Combine**. The resulting address and private key appear.

### How to split a key among several people (M-of-N)

This lets you require, say, any 3 of 5 people to reassemble a key.

1. Choose **Tools → M-of-N Calculator**.
2. Set **Parts to require (m)** — the minimum number of pieces needed to rebuild the key.
3. Set **Parts to generate (n)** — the total number of pieces to create.
4. Click **Generate**. Distribute each part to a different trusted person.

To rebuild the key later:

1. Type any **M** of the parts into the **Part 1–8** boxes.
2. Click **Decode**. The original private key and its address appear.

### How to use the escrow tools

The **Escrow Tools** window (under **Tools → Escrow Tools**) guides a buyer, seller, and neutral agent through a protected transaction, using tabbed steps:

1. Start on the **How it works** tab to read the overview.
2. The **payee** (seller) generates a payment invitation on the **Be a Payee** tab and shares it.
3. The **payer** (buyer) enters that invitation plus the agent's code on the **Be a Payer** tab to get the address to fund.
4. The **escrow agent** issues codes on the **Be an Escrow Agent** tab.
5. When everyone agrees, the **Redeem** tab combines the codes to produce the final private key and address so the funds can be spent.

Each tab includes **Save** and **Print** buttons so you can keep a record at every step.

### How to print vouchers and physical coin inserts

1. In the main list, **tick the checkboxes** of the addresses you want.
2. Open the **Selection** menu and choose:
   - **Print Banknote Vouchers** — opens a window where you set **vouchers per page** (1–3), pick an **artwork style** (Yellow, Green, Blue, Purple, or Greyscale), enter a **denomination**, then click **Print**.
   - **Print Physical Bitcoin Inserts** or **Print Physical Bitcoin Inserts - Dense** — for inserts that go inside collectible coins.

### How to save or export your addresses

1. **Tick the checkboxes** of the addresses you want to export.
2. Open the **Selection** menu and choose:
   - **Save Address List** — saves a text file of addresses only.
   - **Save Address List with PrivKey** — saves a text file pairing each private key with its address.
3. Pick where to save the file when prompted.

> **Caution:** *Save Address List with PrivKey* writes your secret keys into a plain file. Anyone who opens that file can spend the funds. Store it somewhere very safe, or avoid it entirely for keys holding real Bitcoin.

### How to clear or delete addresses

- To remove some: tick them, then **Selection → Delete Selected Items**.
- To remove everything: **File → Clear All**.
- Either way, the program asks you to confirm first, because this can't be undone.

---

*You're all set! Start with **Address → New address** to create your first key, and explore the printing and security tools as your confidence grows. Remember: guard your private keys as carefully as you would cash.*
