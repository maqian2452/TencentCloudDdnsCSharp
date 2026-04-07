using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TencentCloudDdnsCSharp.DnsPod;

internal sealed class TencentDnsPodClient : IDnsPodClient
{
    private const string Service = "dnspod";
    private const string Version = "2021-03-23";
    private const string Host = "dnspod.tencentcloudapi.com";
    private const string Algorithm = "TC3-HMAC-SHA256";
    private const string ContentType = "application/json; charset=utf-8";

    private static readonly Uri Endpoint = new($"https://{Host}/");
    private static readonly HttpClient HttpClient = CreateHttpClient();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string secretId;
    private readonly string secretKey;

    public TencentDnsPodClient(string secretId, string secretKey)
    {
        this.secretId = secretId;
        this.secretKey = secretKey;
    }

    public async Task<IReadOnlyList<DnsPodRecord>> DescribeRecordsAsync(
        string domain,
        string subDomain,
        string recordType,
        string recordLine,
        CancellationToken cancellationToken)
    {
        var request = new DescribeRecordListRequest
        {
            Domain = domain,
            Subdomain = subDomain,
            RecordType = recordType,
            RecordLine = recordLine,
            Limit = 100
        };

        DescribeRecordListResponse response;
        try
        {
            response = await SendAsync<DescribeRecordListRequest, DescribeRecordListResponse>(
                "DescribeRecordList",
                request,
                cancellationToken);
        }
        catch (DnsPodApiException ex) when (string.Equals(ex.Code, "ResourceNotFound.NoDataOfRecord", StringComparison.Ordinal))
        {
            return [];
        }

        return response.RecordList?.Select(record => new DnsPodRecord
        {
            RecordId = record.RecordId,
            Name = record.Name ?? string.Empty,
            Line = record.Line ?? string.Empty,
            LineId = record.LineId ?? string.Empty,
            Type = record.Type ?? string.Empty,
            Value = record.Value ?? string.Empty,
            Ttl = record.TTL
        }).ToArray() ?? [];
    }

    public async Task<long> CreateRecordAsync(
        string domain,
        string subDomain,
        string recordType,
        string recordLine,
        string value,
        int ttl,
        CancellationToken cancellationToken)
    {
        var request = new CreateRecordRequest
        {
            Domain = domain,
            SubDomain = subDomain,
            RecordType = recordType,
            RecordLine = recordLine,
            Value = value,
            TTL = ttl
        };

        var response = await SendAsync<CreateRecordRequest, CreateRecordResponse>(
            "CreateRecord",
            request,
            cancellationToken);

        return response.RecordId;
    }

    public async Task ModifyDynamicDnsAsync(
        long recordId,
        string domain,
        string subDomain,
        string recordLine,
        string value,
        int ttl,
        CancellationToken cancellationToken)
    {
        var request = new ModifyDynamicDnsRequest
        {
            Domain = domain,
            SubDomain = subDomain,
            RecordId = recordId,
            RecordLine = recordLine,
            Value = value,
            TTL = ttl
        };

        _ = await SendAsync<ModifyDynamicDnsRequest, ModifyDynamicDnsResponse>(
            "ModifyDynamicDNS",
            request,
            cancellationToken);
    }

    private async Task<TResponse> SendAsync<TRequest, TResponse>(
        string action,
        TRequest request,
        CancellationToken cancellationToken)
        where TResponse : DnsPodResponseBase
    {
        var payload = JsonSerializer.Serialize(request, JsonOptions);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var requestDate = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var authorization = BuildAuthorization(action, payload, timestamp, requestDate);

        using var message = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        message.Headers.TryAddWithoutValidation("Authorization", authorization);
        message.Headers.TryAddWithoutValidation("X-TC-Action", action);
        message.Headers.TryAddWithoutValidation("X-TC-Timestamp", timestamp.ToString(CultureInfo.InvariantCulture));
        message.Headers.TryAddWithoutValidation("X-TC-Version", Version);

        using var response = await HttpClient.SendAsync(message, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"DNSPod API returned {(int)response.StatusCode} {response.ReasonPhrase}: {json}");
        }

        return Deserialize<TResponse>(json);
    }

    private string BuildAuthorization(string action, string payload, long timestamp, string requestDate)
    {
        const string signedHeaders = "content-type;host";

        var canonicalRequest = string.Join(
            "\n",
            "POST",
            "/",
            string.Empty,
            $"content-type:{ContentType}",
            $"host:{Host}",
            string.Empty,
            signedHeaders,
            Sha256Hex(payload));

        var credentialScope = $"{requestDate}/{Service}/tc3_request";
        var stringToSign = string.Join(
            "\n",
            Algorithm,
            timestamp.ToString(CultureInfo.InvariantCulture),
            credentialScope,
            Sha256Hex(canonicalRequest));

        var secretDate = HmacSha256(Encoding.UTF8.GetBytes($"TC3{secretKey}"), requestDate);
        var secretService = HmacSha256(secretDate, Service);
        var secretSigning = HmacSha256(secretService, "tc3_request");
        var signature = HexEncode(HmacSha256(secretSigning, stringToSign));

        return $"{Algorithm} Credential={secretId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10),
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };
    }

    private static TResponse Deserialize<TResponse>(string json)
        where TResponse : DnsPodResponseBase
    {
        var envelope = JsonSerializer.Deserialize<DnsPodResponseEnvelope<TResponse>>(json, JsonOptions);
        if (envelope?.Response is null)
        {
            throw new InvalidOperationException("DNSPod response is empty.");
        }

        if (envelope.Response.Error is not null)
        {
            throw new DnsPodApiException(
                envelope.Response.Error.Code,
                envelope.Response.Error.Message);
        }

        return envelope.Response;
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string Sha256Hex(string data)
    {
        return HexEncode(SHA256.HashData(Encoding.UTF8.GetBytes(data)));
    }

    private static string HexEncode(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed class DnsPodApiException(string code, string message) : Exception($"{code}: {message}")
    {
        public string Code { get; } = code;
    }

    private sealed class DnsPodResponseEnvelope<TResponse>
        where TResponse : DnsPodResponseBase
    {
        public TResponse? Response { get; set; }
    }

    private abstract class DnsPodResponseBase
    {
        public DnsPodError? Error { get; set; }

        public string? RequestId { get; set; }
    }

    private sealed class DnsPodError
    {
        public string Code { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }

    private sealed class DescribeRecordListRequest
    {
        public string Domain { get; set; } = string.Empty;

        [JsonPropertyName("Subdomain")]
        public string Subdomain { get; set; } = string.Empty;

        public string RecordType { get; set; } = string.Empty;

        public string RecordLine { get; set; } = string.Empty;

        public int Offset { get; set; }

        public int Limit { get; set; }
    }

    private sealed class DescribeRecordListResponse : DnsPodResponseBase
    {
        public List<RecordListItem>? RecordList { get; set; }
    }

    private sealed class RecordListItem
    {
        public long RecordId { get; set; }

        public string? Name { get; set; }

        public string? Line { get; set; }

        public string? LineId { get; set; }

        public string? Type { get; set; }

        public string? Value { get; set; }

        public int TTL { get; set; }
    }

    private sealed class CreateRecordRequest
    {
        public string Domain { get; set; } = string.Empty;

        public string SubDomain { get; set; } = string.Empty;

        public string RecordType { get; set; } = string.Empty;

        public string RecordLine { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public int TTL { get; set; }
    }

    private sealed class CreateRecordResponse : DnsPodResponseBase
    {
        public long RecordId { get; set; }
    }

    private sealed class ModifyDynamicDnsRequest
    {
        public string Domain { get; set; } = string.Empty;

        public string SubDomain { get; set; } = string.Empty;

        public long RecordId { get; set; }

        public string RecordLine { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("Ttl")]
        public int TTL { get; set; }
    }

    private sealed class ModifyDynamicDnsResponse : DnsPodResponseBase
    {
    }
}
