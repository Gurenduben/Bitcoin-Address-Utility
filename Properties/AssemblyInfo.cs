// Bitcoin Address Utility
// Copyright (C) 2012 Mike Caldwell
// Copyright (C) 2026 odolvlobo
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

// GenerateAssemblyInfo is disabled in BtcAddress.csproj, so assembly metadata
// is declared explicitly here. (Original attribution to "Microsoft" was wrong;
// this is Casascius / Mike Caldwell's Bitcoin Address Utility.)

// net10.0-windows normally injects this automatically, but GenerateAssemblyInfo=false
// suppresses it, leaving the assembly platform-neutral and tripping CA1416 on every
// WinForms/System.Drawing call. The app is Windows-only, so declare it explicitly.
[assembly: SupportedOSPlatform("windows")]

[assembly: AssemblyTitle("BtcAddress")]
[assembly: AssemblyDescription("Bitcoin Address Utility")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Casascius")]
[assembly: AssemblyProduct("BtcAddress")]
[assembly: AssemblyCopyright("Copyright (C) 2012 Mike Caldwell, (C) 2026 odolvlobo")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: Guid("ab22ce72-1f18-4bae-b68c-cab9e3958929")]

[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]
[assembly: AssemblyInformationalVersion("1.2.0")]
