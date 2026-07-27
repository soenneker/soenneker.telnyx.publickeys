using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Telnyx.Client.Registrars;
using Soenneker.Telnyx.PublicKeys.Abstract;

namespace Soenneker.Telnyx.PublicKeys.Registrars;

/// <summary>
/// A .NET utility for retrieving and caching Telnyx public keys.
/// </summary>
public static class TelnyxPublicKeysUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="ITelnyxPublicKeysUtil"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddTelnyxPublicKeysUtilAsSingleton(this IServiceCollection services)
    {
        services.AddTelnyxHttpClientAsSingleton()
                .TryAddSingleton<ITelnyxPublicKeysUtil, TelnyxPublicKeysUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="ITelnyxPublicKeysUtil"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddTelnyxPublicKeysUtilAsScoped(this IServiceCollection services)
    {
        services.AddTelnyxHttpClientAsSingleton()
                .TryAddScoped<ITelnyxPublicKeysUtil, TelnyxPublicKeysUtil>();

        return services;
    }
}
