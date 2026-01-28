using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using VoicevoxHelper.Core.Models;
using VoicevoxHelper.Infrastructure.VoicevoxApi;

namespace VoicevoxHelper.Tests.Infrastructure.VoicevoxApi;

[TestFixture]
public class VoicevoxApiClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Test]
    public async Task GetUserDictionaryAsync_ReturnsEntries()
    {
        var payload = JsonSerializer.Serialize(new Dictionary<string, VoicevoxDictionaryEntry>
        {
            { "uuid", new VoicevoxDictionaryEntry { Surface = "A", Pronunciation = "エー", AccentType = 1 } }
        });

        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        });
        var client = new HttpClient(handler);
        var api = new VoicevoxApiClient(client, Options.Create(new VoicevoxSettings { BaseUrl = "http://localhost" }), NullLogger<VoicevoxApiClient>.Instance);

        var result = await api.GetUserDictionaryAsync(CancellationToken.None);

        Assert.That(result, Contains.Key("uuid"));
    }
}
