using Microsoft.Extensions.Logging.Abstractions;
using Soenneker.Telnyx.PublicKeys.Abstract;
using Soenneker.Telnyx.Client.Abstract;
using Soenneker.Tests.HostedUnit;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Telnyx.PublicKeys.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class TelnyxPublicKeysUtilTests : HostedUnitTest
{
    private readonly ITelnyxPublicKeysUtil _util;

    public TelnyxPublicKeysUtilTests(Host host) : base(host)
    {
        _util = Resolve<ITelnyxPublicKeysUtil>(true);
    }

    [Test]
    public async Task Default()
    {
        await Assert.That(_util).IsNotNull();
    }

    [Test]
    public async Task Get_should_cache_the_public_key()
    {
        string key = Convert.ToBase64String(new byte[32]);
        var httpClient = new TestTelnyxHttpClient(_ => CreateResponse(key));
        var util = new TelnyxPublicKeysUtil(httpClient, NullLogger<TelnyxPublicKeysUtil>.Instance);

        string first = await util.Get();
        string second = await util.Get();

        await Assert.That(first).IsEqualTo(key);
        await Assert.That(second).IsEqualTo(key);
        await Assert.That(httpClient.RequestCount).IsEqualTo(1);
    }

    [Test]
    public async Task Refresh_should_replace_the_cached_public_key()
    {
        string firstKey = Convert.ToBase64String(new byte[32]);
        var secondBytes = new byte[32];
        Array.Fill(secondBytes, (byte) 1);
        string secondKey = Convert.ToBase64String(secondBytes);

        var httpClient = new TestTelnyxHttpClient(requestNumber => CreateResponse(requestNumber == 1 ? firstKey : secondKey));
        var util = new TelnyxPublicKeysUtil(httpClient, NullLogger<TelnyxPublicKeysUtil>.Instance);

        string first = await util.Get();
        string refreshed = await util.Refresh();
        string cached = await util.Get();

        await Assert.That(first).IsEqualTo(firstKey);
        await Assert.That(refreshed).IsEqualTo(secondKey);
        await Assert.That(cached).IsEqualTo(secondKey);
        await Assert.That(httpClient.RequestCount).IsEqualTo(2);
    }

    [Test]
    public async Task Get_should_reject_an_invalid_public_key()
    {
        var httpClient = new TestTelnyxHttpClient(_ => CreateResponse("not-a-public-key"));
        var util = new TelnyxPublicKeysUtil(httpClient, NullLogger<TelnyxPublicKeysUtil>.Instance);

        Exception? exception = null;

        try
        {
            await util.Get();
        }
        catch (Exception e)
        {
            exception = e;
        }

        await Assert.That(exception).IsTypeOf<System.Text.Json.JsonException>();
    }

    private static HttpResponseMessage CreateResponse(string publicKey)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{{\"data\":{{\"id\":\"key-id\",\"public\":\"{publicKey}\",\"record_type\":\"public_key\"}}}}")
        };
    }

    private sealed class TestTelnyxHttpClient : ITelnyxHttpClient
    {
        private readonly HttpClient _client;
        private int _requestCount;

        public int RequestCount => _requestCount;

        public TestTelnyxHttpClient(Func<int, HttpResponseMessage> responseFactory)
        {
            _client = new HttpClient(new TestHttpMessageHandler(() => responseFactory(Interlocked.Increment(ref _requestCount))))
            {
                BaseAddress = new Uri("https://api.telnyx.com/v2/")
            };
        }

        public ValueTask<HttpClient> Get(CancellationToken cancellationToken = default)
        {
            return new ValueTask<HttpClient>(_client);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            _client.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _responseFactory;

        public TestHttpMessageHandler(Func<HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory());
        }
    }
}
