# Troubleshooting

## Frequently Asked Questions & Common Errors

### "No items with printable private keys are selected."

You tried to print, but none of the ticked rows have a usable private key. Tick rows that include a known private key (the ones you generated in this program), then try again.

### "X of the selected items cannot be printed because the private key is not known. These items will be skipped."

Some of your ticked rows are addresses without their private key (for example, addresses you imported by name only). The program will simply skip those and print the rest — no action needed unless you expected them included.

### "No items are selected" / "Nothing to delete"

You started an action before ticking any checkboxes. Tick at least one row, then repeat the action.

### "Do you want to clear (delete) these keys? This cannot be undone."

This is a safety confirmation before deleting. Click **OK** only if you're sure — there is no way to recover deleted keys afterward. If you have any doubt, click **Cancel** and back up first.

### "Failed to save file"

The program couldn't write your export file. Common causes: the folder is read-only, the disk is full, or the file is open in another program. Choose a different folder (such as your Documents folder) and try again.

### My decryption or confirmation says the passphrase is wrong

Passphrases are exact — including capital letters and spaces. Re-type it carefully and try once more. Keys can only be unlocked with the precise passphrase used to create them; there is no recovery if it's lost.

### The program seems to "hang" for a few seconds when I make encrypted or two-factor keys

This is normal and intentional. The encryption is deliberately slow to make it much harder for an attacker to guess passphrases. Give it a few seconds to finish.

### Why does the Balance column always say 0.00?

This tool works offline and does not check live balances on the Bitcoin network. The column is a placeholder. To see a real balance, look up the address in a blockchain explorer on an internet-connected device.

### Is my computer's internet connection used?

No internet connection is required to generate or manage keys. For the strongest security, keep the computer offline while creating keys you intend to fund.

### I closed the program — are my addresses saved?

Treat anything in the list as temporary unless you've explicitly exported it (see *How to save or export your addresses*) or printed it. Always back up keys that hold real value.
