[![](https://img.shields.io/nuget/v/soenneker.telnyx.publickeys.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.telnyx.publickeys/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.telnyx.publickeys/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.telnyx.publickeys/actions/workflows/publish-package.yml)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.telnyx.publickeys/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.telnyx.publickeys/actions/workflows/codeql.yml)
[![](https://img.shields.io/nuget/dt/soenneker.telnyx.publickeys.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.telnyx.publickeys/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Telnyx.PublicKeys
Retrieves and caches the Ed25519 public key used to verify Telnyx webhook signatures.

## Installation

```bash
dotnet add package Soenneker.Telnyx.PublicKeys
```

## Registration

```csharp
using Soenneker.Telnyx.PublicKeys.Registrars;

services.AddTelnyxPublicKeysUtilAsSingleton();
```

Singleton registration shares the cached key across consumers. Use `AddTelnyxPublicKeysUtilAsScoped()` when the utility should follow the current scope; its Telnyx HTTP provider remains singleton and is reused across scopes.

Configure the Telnyx API token used to retrieve the account's webhook-signing key:

```json
{
  "Telnyx": {
    "Token": "KEY..."
  }
}
```

## Usage

```csharp
using Soenneker.Telnyx.PublicKeys.Abstract;

public sealed class WebhookService
{
    private readonly ITelnyxPublicKeysUtil _publicKeys;

    public WebhookService(ITelnyxPublicKeysUtil publicKeys)
    {
        _publicKeys = publicKeys;
    }

    public async ValueTask<string> GetPublicKey(CancellationToken cancellationToken)
    {
        return await _publicKeys.Get(cancellationToken);
    }
}
```

`Get()` retrieves the Base64-encoded Ed25519 key from Telnyx and caches it for 24 hours. Use `Refresh()` when you need to bypass that cache immediately.

`RefreshIfCurrent()` supports signature-verification retry flows. It refreshes only when the caller's key is still current
and rate-limits conditional refreshes to one per minute.
