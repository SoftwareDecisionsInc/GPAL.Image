# GPAL.Image

Image matching for [GPAL](https://gpal.sditx.com/), the Generally Positive Automation Library.

A GPAL selector can name an element by how it looks: a picture of a button, an icon, a logo. GPAL finds that
picture on the screen, or in the browser's own capture when the browser is headless or on a hidden desktop.
The matching itself lives in this package, so GPAL carries none of OpenCV's weight unless a workflow needs it.

## Use

Add the package, then register it once at startup, before the first image selector runs:

```csharp
using GenerallyPositive;
using GenerallyPositive.Image;

GPALImage.Register();

Selector logo = GPAL.Selector
    .WithImage((GPALFile)@"C:\templates\logo.png")
    .ToGPALObject();
```

Without it, an image selector finds nothing and GPAL publishes a warning saying the package is needed.

## QR codes

The same registration lets GPAL read QR codes, such as the one a site shows when two-factor sign-in is turned
on, or a Google Authenticator export holding every exported account. The secret it holds then makes the
6-digit sign-in code:

```csharp
ICredentials fidelity = GPAL.CredentialsFor(CredentialServiceType.None)
    .WithOtpQRCode((GPALFile)@"C:\secure\fidelity-qr.png")
    .ToGPALObject();
fidelity.GetOtpCode(out string code);
```

## Requirements

- .NET Framework 4.8 on Windows
- A 64-bit process: OpenCV ships its native library for 64-bit Windows only. For an AnyCPU program, turn off
  Prefer 32-bit in the build settings, or target x64
- GPAL at the same version, installed with this package

Documentation: https://gpal.sditx.com/docs/concepts

## License

GPAL.Image is source available under the **Business Source License 1.1**, the same terms as GPAL. Full
terms: [LICENSE.md](./LICENSE.md).

Copyright 2026 Software Decisions, Inc. All rights reserved.

*This repository is a read only public release mirror. Development happens privately, and pull requests
are not accepted here.*
