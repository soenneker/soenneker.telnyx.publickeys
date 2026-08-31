using Microsoft.Extensions.Logging;
using Soenneker.Asyncs.Locks;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Telnyx.PublicKeys.Abstract;
using Soenneker.Telnyx.Client.Abstract;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Telnyx.PublicKeys;

public sealed class TelnyxPublicKeysUtil : ITelnyxPublicKeysUtil
{
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan _conditionalRefreshCooldown = TimeSpan.FromMinutes(1);
    private const string _publicKeyPath = "public_key";
    private const int _ed25519PublicKeyLength = 32;

    private readonly ITelnyxHttpClient _telnyxHttpClient;
    private readonly ILogger<TelnyxPublicKeysUtil> _logger;
    private readonly AsyncLock _refreshLock = new();

    private CacheEntry? _cached;
    private DateTimeOffset _lastConditionalRefreshAt = DateTimeOffset.MinValue;

    public TelnyxPublicKeysUtil(ITelnyxHttpClient telnyxHttpClient, ILogger<TelnyxPublicKeysUtil> logger)
    {
        _telnyxHttpClient = telnyxHttpClient;
        _logger = logger;
    }

    public ValueTask<string> Get(CancellationToken cancellationToken = default)
    {
        CacheEntry? cached = Volatile.Read(ref _cached);

        if (cached is not null && cached.ExpiresAt > DateTimeOffset.UtcNow)
            return new ValueTask<string>(cached.PublicKey);

        return GetSlow(cancellationToken);
    }

    public ValueTask<string> Refresh(CancellationToken cancellationToken = default)
    {
        return RefreshSlow(cancellationToken);
    }

    public ValueTask<string> RefreshIfCurrent(string expectedPublicKey, CancellationToken cancellationToken = default)
    {
        return RefreshIfCurrentSlow(expectedPublicKey, cancellationToken);
    }

    private async ValueTask<string> GetSlow(CancellationToken cancellationToken)
    {
        using (await _refreshLock.Lock(cancellationToken).NoSync())
        {
            CacheEntry? cached = Volatile.Read(ref _cached);

            if (cached is not null && cached.ExpiresAt > DateTimeOffset.UtcNow)
                return cached.PublicKey;

            return await RetrieveAndCache(cancellationToken).NoSync();
        }
    }

    private async ValueTask<string> RefreshSlow(CancellationToken cancellationToken)
    {
        using (await _refreshLock.Lock(cancellationToken).NoSync())
        {
            return await RetrieveAndCache(cancellationToken).NoSync();
        }
    }

    private async ValueTask<string> RefreshIfCurrentSlow(string expectedPublicKey, CancellationToken cancellationToken)
    {
        using (await _refreshLock.Lock(cancellationToken).NoSync())
        {
            CacheEntry? cached = Volatile.Read(ref _cached);

            if (cached is not null && !string.Equals(cached.PublicKey, expectedPublicKey, StringComparison.Ordinal))
                return cached.PublicKey;

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (cached is not null && now - _lastConditionalRefreshAt < _conditionalRefreshCooldown)
                return cached.PublicKey;

            _lastConditionalRefreshAt = now;
            return await RetrieveAndCache(cancellationToken).NoSync();
        }
    }

    private async ValueTask<string> RetrieveAndCache(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving the Telnyx webhook-signing public key");

        HttpClient client = await _telnyxHttpClient.Get(cancellationToken).NoSync();

        using HttpResponseMessage response =
            await client.GetAsync(_publicKeyPath, HttpCompletionOption.ResponseHeadersRead, cancellationToken).NoSync();

        response.EnsureSuccessStatusCode();

        await using System.IO.Stream content = await response.Content.ReadAsStreamAsync(cancellationToken).NoSync();
        using JsonDocument document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken).NoSync();

        if (!document.RootElement.TryGetProperty("data", out JsonElement data) ||
            !data.TryGetProperty("public", out JsonElement publicKeyElement) ||
            publicKeyElement.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("The Telnyx public-key response did not contain a string at 'data.public'.");
        }

        string? publicKey = publicKeyElement.GetString()?.Trim();

        if (publicKey is null || !IsValidEd25519PublicKey(publicKey))
            throw new JsonException("The Telnyx public-key response did not contain a valid Base64-encoded Ed25519 public key.");

        Volatile.Write(ref _cached, new CacheEntry(publicKey, DateTimeOffset.UtcNow.Add(_cacheDuration)));
        _logger.LogDebug("Cached the Telnyx webhook-signing public key for {CacheDuration}", _cacheDuration);

        return publicKey;
    }

    private static bool IsValidEd25519PublicKey(string publicKey)
    {
        Span<byte> bytes = stackalloc byte[_ed25519PublicKeyLength];
        return Convert.TryFromBase64String(publicKey, bytes, out int bytesWritten) && bytesWritten == _ed25519PublicKeyLength;
    }

    private sealed record CacheEntry(string PublicKey, DateTimeOffset ExpiresAt);
}
