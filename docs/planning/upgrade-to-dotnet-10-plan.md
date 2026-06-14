# .NET 4.0 → .NET 10 migration (COMPLETE)

Status: **done** on branch `feature/upgrade-to-dotnet-10` (commit `feb589d`, released as v1.1.0).
This file was the original agent plan; it is now a record of what shipped, including
where the result diverged from the plan. For day-to-day guidance see `CLAUDE.md`;
for the crypto gate see `test/golden-vectors.md`.

## Objective (met)

Retargeted `BtcAddress.csproj` to `net10.0-windows`, kept WinForms, swapped the dead
unmanaged DLL refs for NuGet, and produced a single-file self-contained x64 `.exe`
that runs offline.

## Constraints (held)

- WinForms kept. No UI redesign, no new features, no WPF.
- Minimal diff. `Nullable=disable`, `ImplicitUsings=disable` (legacy code).
- No logic extracted from forms (still deferred — see Out of scope).

## Release gate — CHANGED from the original plan

Original plan: crypto output **byte-identical to the original 4.0 binary**.

Reality: the 4.0 binary cannot be rebuilt on a modern toolchain, so there is no
golden binary to diff. The gate became **all golden vectors pass**, anchored to
public known-answer vectors (objective truth) for standardized paths plus
round-trip self-consistency for the Casascius-custom paths. This is enforced by an
automated harness, not a manual comparison:

```powershell
dotnet run --project test/GoldenVectors/GoldenVectors.csproj -c Release
```

Exit code = number of failing vectors (0 = `ALL VECTORS PASSED`). Spec: `test/golden-vectors.md`.

## Dependency swaps (done)

| Removed | Added | Notes |
| --- | --- | --- |
| `BouncyCastle.Crypto.dll` ref | `BouncyCastle.Cryptography` 2.* | `Org.BouncyCastle.*` v1→v2 compile-fix; `ECPoint.GetEncoded(bool)` compression args re-derived |
| `ThoughtWorks.QRCode.dll` ref | `QRCoder` 1.* | `Barcode/QR.cs` rewritten |
| dead BCL refs (`System.Deployment`, etc.) | — | dropped |
| `System.Windows.Forms` BCL ref | `<UseWindowsForms>true` | — |

Note: `System.Drawing.Common` (in the original plan) was **not** needed — `net10.0-windows`
+ `UseWindowsForms` already provides `System.Drawing` incl. `System.Drawing.Printing`.

## What shipped

### csproj

SDK-style, `net10.0-windows`, `x64`, `AllowUnsafeBlocks` (CryptSharp), `GenerateAssemblyInfo=false`.
Diverged from the plan's snippet:

- No `System.Drawing.Common` package (see above).
- `Version` = `1.1.0.0`; `Copyright` = `Copyright (C) 2012 Mike Caldwell, (C) 2026 odolvlobo`.
- Added `<PathMap>$(MSBuildProjectDirectory)=.</PathMap>` — normalizes embedded source/pdb paths for reproducible builds.
- Added an `ItemGroup` removing `test\**` from the app's compile/resource/none globs (the harness is a separate project under `test/`).

> **Superseded (post-v1.1.0):** `AllowUnsafeBlocks` has since been removed. It existed
> only for the bundled `CryptSharp/` scrypt, which was replaced by BouncyCastle
> `Org.BouncyCastle.Crypto.Generators.SCrypt.Generate`; no `unsafe` code remains. See `CLAUDE.md`.

### Config / metadata

- Deleted `app.config`, `Properties/Settings.settings`, `Properties/Settings.Designer.cs`.
- `Properties/AssemblyInfo.cs` was **kept**, not deleted (plan suggested deleting). It holds
  the explicit assembly attributes (with `GenerateAssemblyInfo=false`), fixes the bogus
  original `AssemblyCompany("Microsoft")` attribution, and carries the GPL header + `[Guid]`/`ComVisible`.

### Test harness

`test/GoldenVectors/` — separate `net10.0-windows` console project, `ProjectReference` to
`BtcAddress.csproj`. 26 checks: priv `0x01` (address/WIF/pubkey, compressed + uncompressed),
Base58Check leading-zero, mini key, BIP38 no-EC-multiply (spec byte-match) + EC-multiply
(round-trip), M-of-N 2-of-3, escrow round-trip, QR payload lengths.

### Release signing

Releases ship a GnuPG **detached signature** (`BtcAddress.exe.asc`); key fingerprint
`6B6BC26599EC24EF7E29A405EAF050539D0B2925`. Authenticode signing is documented as an
optional extra. Full workflow: `SIGNING.md` (user-facing summary in `README.md`).

## Build & publish (verified)

```powershell
dotnet build BtcAddress.csproj -c Release
dotnet publish BtcAddress.csproj -r win-x64 -c Release -p:PublishSingleFile=true --self-contained true
```

Output: `bin\Release\net10.0-windows\win-x64\publish\BtcAddress.exe` — runs on a clean
Windows with no .NET runtime and no network.

## Hardening

- Grep for `HttpClient`/`WebRequest`/`Socket`/`new Uri(` over the source: **zero** network calls. Confirms offline operation.
- GPG release signing in place (above).

## Still out of scope (future work)

- Extracting print/keygen logic out of form code-behind (Phase 1.5).
- WPF migration (Phase 2; same project via `<UseWPF>true</UseWPF>`).
- Build warnings remain (CS8981 lowercase type names in `MofN.cs`, unused-variable CS0168/CS0219 in `Reports/*` and `Forms/Form1.cs`) — cosmetic, not addressed.
