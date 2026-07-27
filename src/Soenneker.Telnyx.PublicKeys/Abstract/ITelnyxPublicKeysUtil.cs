using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Telnyx.PublicKeys.Abstract;

/// <summary>
/// A .NET utility for retrieving and caching Telnyx public keys.
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
}
