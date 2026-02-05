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

    [Test]
    public async Task CreateWordAsync_SendsQueryParametersAndReturnsUuid()
    {
        var handler = new StubHandler(request =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.RequestUri?.AbsolutePath, Is.EqualTo("/user_dict_word"));

            var query = ParseQuery(request.RequestUri?.Query);
            Assert.That(query["surface"], Is.EqualTo("A"));
            Assert.That(query["pronunciation"], Is.EqualTo("エー"));
            Assert.That(query["accent_type"], Is.EqualTo("1"));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("\"generated-uuid\"", Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler);
        var api = new VoicevoxApiClient(client, Options.Create(new VoicevoxSettings { BaseUrl = "http://localhost" }), NullLogger<VoicevoxApiClient>.Instance);

        var uuid = await api.CreateWordAsync(new VoicevoxDictionaryEntry { Surface = "A", Pronunciation = "エー", AccentType = 1 }, CancellationToken.None);

        Assert.That(uuid, Is.EqualTo("generated-uuid"));
    }

    [Test]
    public async Task UpdateWordAsync_AppendsQueryParameters()
    {
        var handler = new StubHandler(request =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Put));
            Assert.That(request.RequestUri?.AbsolutePath, Is.EqualTo("/user_dict_word/uuid"));

            var query = ParseQuery(request.RequestUri?.Query);
            Assert.That(query["surface"], Is.EqualTo("B"));
            Assert.That(query["pronunciation"], Is.EqualTo("ビー"));
            Assert.That(query["accent_type"], Is.EqualTo("2"));

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var client = new HttpClient(handler);
        var api = new VoicevoxApiClient(client, Options.Create(new VoicevoxSettings { BaseUrl = "http://localhost" }), NullLogger<VoicevoxApiClient>.Instance);

        await api.UpdateWordAsync("uuid", new VoicevoxDictionaryEntry { Surface = "B", Pronunciation = "ビー", AccentType = 2 }, CancellationToken.None);
    }

    private static IReadOnlyDictionary<string, string> ParseQuery(string? query)
    {
        var trimmed = query?.TrimStart('?') ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
        {
            return new Dictionary<string, string>();
        }

        return trimmed
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(pair => pair.Length == 2)
            .ToDictionary(pair => Uri.UnescapeDataString(pair[0]), pair => Uri.UnescapeDataString(pair[1]));
    }
}
