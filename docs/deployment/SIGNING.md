# Release signing (plan Step 9)

Releases are signed with **GnuPG detached signatures** (OpenPGP), the same
mechanism used by Bitcoin Core, Electrum, and Tails. A detached `.asc` rides
alongside the `.exe`; users verify it against the published public key.

This is **not** Windows Authenticode — it does not remove the SmartScreen
"unknown publisher" prompt. It proves the binary came from the holder of the
signing key and was not altered. For this air-gapped, key-handling tool aimed at
a crypto-literate audience, that is the relevant integrity guarantee. An
Authenticode cert (self-signed for local use, or Azure Trusted Signing / a CA
for public trust) would be a separate, additional step — see the end of this
file.

## Signing key

```text
pub   rsa4096/EAF050539D0B2925  2020-10-27  [SC]
      6B6BC26599EC24EF7E29A405EAF050539D0B2925
uid   odolvlobo <odolvlobo@bitcointalk.com>
```

Managed in Kleopatra (Gpg4win). The private key never leaves the GnuPG keyring;
`gpg-agent` handles the passphrase prompt.

Tools used:

- `gpg.exe` — `C:\Program Files (x86)\GnuPG\bin\gpg.exe` (GnuPG 2.4.5, Gpg4win).
  Note: do **not** use the Git-bundled `gpg` (`...\Git\usr\bin\gpg.exe`) — it has
  a separate, empty keyring and cannot see this key.

## Target file

```text
bin\Release\net10.0-windows\win-x64\publish\BtcAddress.exe
```

Re-sign after **every** republish — the signature binds to the exact bytes, so
any rebuild voids the prior `.asc`. Order: **publish -> sign -> distribute**.

## Sign

```powershell
$gpg = "C:\Program Files (x86)\GnuPG\bin\gpg.exe"
$exe = "bin\Release\net10.0-windows\win-x64\publish\BtcAddress.exe"

& $gpg --local-user 6B6BC26599EC24EF7E29A405EAF050539D0B2925 `
       --armor --detach-sign $exe
```

Produces `BtcAddress.exe.asc` next to the exe. Kleopatra/pinentry prompts for the
passphrase unless `gpg-agent` has it cached.

## Verify (locally, after signing)

```powershell
& $gpg --verify "$exe.asc" $exe
```

Expect:

```text
gpg: Good signature from "odolvlobo <odolvlobo@bitcointalk.com>" [ultimate]
```

## Distribute

Publish **both** files together:

```text
BtcAddress.exe
BtcAddress.exe.asc
```

## Publish the public key (so others can verify)

Export and host the public key (your website, the repo, a GitHub release), and/or
push it to a keyserver:

```powershell
# Export to a file to host alongside releases.
# Use gpg's --output, NOT PowerShell '>' (which writes UTF-16 and corrupts the armor).
& $gpg --armor --output odolvlobo.asc --export 6B6BC26599EC24EF7E29A405EAF050539D0B2925

# Optional: publish to a keyserver
& $gpg --keyserver hkps://keys.openpgp.org `
       --send-keys 6B6BC26599EC24EF7E29A405EAF050539D0B2925
```

Always publish the **full fingerprint** `6B6BC26599EC24EF7E29A405EAF050539D0B2925`
out-of-band so users pin the right key (a short key id is forgeable).

### What a downloader runs

```text
gpg --recv-keys 6B6BC26599EC24EF7E29A405EAF050539D0B2925   # or import odolvlobo.asc
gpg --verify BtcAddress.exe.asc BtcAddress.exe
```

A `Good signature` line with that fingerprint means the exe is authentic and
intact.

## Security notes

- **Never** export or commit the secret key. Keep it in the keyring; sign locally
  on a trusted machine. A leaked signing key lets an attacker ship a trojaned
  build under this identity — and this tool generates private keys.
- Build and sign on your own machine, not in cloud CI, to keep the key and the
  build off third-party infrastructure.
- The `.asc` and `.exe` are release artifacts (under `bin\`), not source — they
  are not committed to the repo; attach them to the release/download instead.

## Optional: Windows Authenticode (separate, not done here)

To also drop the SmartScreen "unknown publisher" warning, sign with an X.509
code-signing certificate using `signtool` (Windows SDK). This needs a cert the
GPG key cannot provide:

- **Self-signed** — free, immediate; trusted only on machines where you install
  the cert into Trusted Root + Trusted Publishers. Fine for personal use.
- **Azure Trusted Signing** (~$10/mo, Microsoft-rooted) or an OV/EV cert from a
  CA — required for public trust; subject to identity validation.

Authenticode and GPG are independent and can both be applied to the same exe.
