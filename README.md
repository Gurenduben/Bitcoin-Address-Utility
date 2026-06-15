# Bitcoin Address Utility

[![CI](https://github.com/odolvlobo/Bitcoin-Address-Utility/actions/workflows/ci.yml/badge.svg)](https://github.com/odolvlobo/Bitcoin-Address-Utility/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/odolvlobo/Bitcoin-Address-Utility/branch/develop/graph/badge.svg?token=WY90VV6GST)](https://codecov.io/gh/odolvlobo/Bitcoin-Address-Utility)

A Windows desktop tool for generating and manipulating Bitcoin keys and
addresses — paper wallets, BIP38-encrypted keys, Casascius mini private keys,
M-of-N split keys, and escrow codes. Originally by Mike Caldwell (Casascius).

**Security:** this app generates and handles private keys. Run it **offline /
air-gapped**. Verify your download (below) before trusting it with key material.

License: GPLv3.

## Build

- .NET 10, WPF, x64. No cross-platform support.
- Dependencies restore automatically from NuGet
  (`BouncyCastle.Cryptography`, `QRCoder`) — no manual DLLs required.

```powershell
dotnet build BtcAddress.csproj -c Release
```

Produce a single-file, self-contained exe (bundles the .NET runtime; runs on a
clean Windows with nothing installed):

```powershell
dotnet publish BtcAddress.csproj -r win-x64 -c Release -p:PublishSingleFile=true --self-contained true
```

Output: `bin\Release\net10.0-windows\win-x64\publish\BtcAddress.exe`.

## Verifying the download

Releases are signed with a GnuPG **detached signature**. Each `BtcAddress.exe`
ships with a `BtcAddress.exe.asc` next to it. Verify before running:

```text
# 1. Get the signing public key (once)
gpg --recv-keys 6B6BC26599EC24EF7E29A405EAF050539D0B2925

# 2. Verify the exe against its signature
gpg --verify BtcAddress.exe.asc BtcAddress.exe
```

A good result shows:

```text
gpg: Good signature from "odolvlobo <odolvlobo@bitcointalk.com>"
```

The signing key fingerprint is:

```text
6B6BC26599EC24EF7E29A405EAF050539D0B2925
```

Confirm that full fingerprint from a trusted source — a short key id can be
forged. If `gpg --verify` reports anything other than a good signature with this
fingerprint, **do not run the binary.**

This is an OpenPGP signature, not Windows Authenticode, so Windows may still show
an "unknown publisher" prompt — that is expected. Signing details and the
maintainer workflow are in [SIGNING.md](SIGNING.md).

## Validation

Crypto output is checked against published known-answer vectors (private key
`0x01`, BIP38 spec vectors, mini key, Base58Check, M-of-N, escrow). See
[test/golden-vectors.md](test/golden-vectors.md). The harness is an xUnit project;
run the full test suite (unit tests + golden vectors) with:

```powershell
dotnet test BtcAddress.sln
```
