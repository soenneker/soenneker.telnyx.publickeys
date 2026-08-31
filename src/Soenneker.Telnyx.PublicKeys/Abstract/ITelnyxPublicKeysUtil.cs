using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Telnyx.PublicKeys.Abstract;

/// <summary>
/// Retrieves and caches the Ed25519 public key used to verify Telnyx webhook signatures.
/// </summary>
public interface ITelnyxPublicKeysUtil
{
    /// <summary>
    /// Gets the current Telnyx webhook-signing public key.
    /// </summary>
    /// <remarks>
    /// The key is retrieved from Telnyx on the first call and cached for a short period.
    /// </remarks>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Base64-encoded Ed25519 public key.</returns>
    ValueTask<string> Get(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current Telnyx webhook-signing public key and replaces the cached value.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Base64-encoded Ed25519 public key.</returns>
    ValueTask<string> Refresh(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the public key only when <paramref name="expectedPublicKey"/> is still the cached value.
    /// </summary>
    /// <remarks>
    /// Conditional refreshes are rate-limited to prevent invalid webhook signatures from causing an unbounded number of Telnyx API calls.
    /// </remarks>
    /// <param name="expectedPublicKey">The public key used by the caller before it determined that a refresh may be needed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current Base64-encoded Ed25519 public key.</returns>
    ValueTask<string> RefreshIfCurrent(string expectedPublicKey, CancellationToken cancellationToken = default);
}
