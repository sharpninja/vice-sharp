using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using RomM.Client;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.RomM;

/// <summary>
/// FR-ROMM-COLLECT-001. The RomM adapter implementation of <see cref="IRomMCollectionsGateway"/> over the
/// untyped /api/collections endpoints, reached through the generic <see cref="IRomMTransport"/> and
/// deserialized/serialized reflection-free via <see cref="RomMJsonContext"/> (TR-ROMM-JSON-001). List and
/// create/rename use RomM's multipart form endpoints; add/remove roms use the JSON payload endpoints.
/// </summary>
public sealed class RomMCollectionsGateway : IRomMCollectionsGateway
{
    private static readonly JsonTypeInfo<List<RomMCollectionDto>> ListInfo =
        (JsonTypeInfo<List<RomMCollectionDto>>)RomMJsonContext.Default.GetTypeInfo(typeof(List<RomMCollectionDto>))!;

    private static readonly JsonTypeInfo<RomMCollectionDto> DtoInfo =
        (JsonTypeInfo<RomMCollectionDto>)RomMJsonContext.Default.GetTypeInfo(typeof(RomMCollectionDto))!;

    private static readonly JsonTypeInfo<RomMCollectionRomsPayload> PayloadInfo =
        (JsonTypeInfo<RomMCollectionRomsPayload>)RomMJsonContext.Default.GetTypeInfo(typeof(RomMCollectionRomsPayload))!;

    private readonly IRomMClient _client;

    /// <summary>Creates the collections gateway over a constructed RomM client.</summary>
    /// <param name="client">The authenticated RomM client.</param>
    public RomMCollectionsGateway(IRomMClient client) =>
        _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc />
    public async Task<IReadOnlyList<LibraryCollection>> GetCollectionsAsync(
        bool includeSmartVirtual,
        CancellationToken cancellationToken = default)
    {
        var result = new List<LibraryCollection>();
        result.AddRange(await GetListAsync("api/collections", readOnly: false, cancellationToken).ConfigureAwait(false));

        if (includeSmartVirtual)
        {
            result.AddRange(await GetListAsync("api/collections/smart", readOnly: true, cancellationToken).ConfigureAwait(false));
            result.AddRange(await GetListAsync("api/collections/virtual", readOnly: true, cancellationToken).ConfigureAwait(false));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<LibraryCollection> CreateCollectionAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var form = new MultipartFormDataContent
        {
            { new StringContent(name), "name" },
            { new StringContent(string.Empty), "description" },
        };

        using HttpResponseMessage response = await _client.Transport
            .SendAsync(HttpMethod.Post, "api/collections", form, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        RomMCollectionDto dto = JsonSerializer.Deserialize(json, DtoInfo)
            ?? throw new InvalidOperationException("Empty create-collection response.");
        return Map(dto, readOnly: false);
    }

    /// <inheritdoc />
    public async Task RenameCollectionAsync(int id, string newName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        // RomM's PUT requires the full rom_ids set; fetch the current membership first.
        LibraryCollection current = await GetOneAsync(id, cancellationToken).ConfigureAwait(false);

        using var form = new MultipartFormDataContent
        {
            { new StringContent(newName), "name" },
            { new StringContent(string.Join(",", current.RomIds)), "rom_ids" },
        };

        using HttpResponseMessage response = await _client.Transport
            .SendAsync(HttpMethod.Put, $"api/collections/{id}", form, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteCollectionAsync(int id, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _client.Transport
            .SendAsync(HttpMethod.Delete, $"api/collections/{id}", cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task AddRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default) =>
        SendRomsAsync(HttpMethod.Post, id, romIds, cancellationToken);

    /// <inheritdoc />
    public Task RemoveRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default) =>
        SendRomsAsync(HttpMethod.Delete, id, romIds, cancellationToken);

    private async Task SendRomsAsync(HttpMethod method, int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(romIds);

        var payload = new RomMCollectionRomsPayload { RomIds = romIds.ToList() };
        string json = JsonSerializer.Serialize(payload, PayloadInfo);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await _client.Transport
            .SendAsync(method, $"api/collections/{id}/roms", content, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<LibraryCollection>> GetListAsync(string url, bool readOnly, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _client.Transport
            .SendAsync(HttpMethod.Get, url, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        List<RomMCollectionDto> dtos = JsonSerializer.Deserialize(json, ListInfo) ?? new List<RomMCollectionDto>();
        return dtos.Select(d => Map(d, readOnly)).ToList();
    }

    private async Task<LibraryCollection> GetOneAsync(int id, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _client.Transport
            .SendAsync(HttpMethod.Get, $"api/collections/{id}", cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        RomMCollectionDto dto = JsonSerializer.Deserialize(json, DtoInfo)
            ?? throw new InvalidOperationException("Empty collection response.");
        return Map(dto, dto.IsSmart || dto.IsVirtual);
    }

    private static LibraryCollection Map(RomMCollectionDto dto, bool readOnly) =>
        new(
            dto.Id,
            dto.Name ?? string.Empty,
            dto.RomCount,
            readOnly || dto.IsSmart || dto.IsVirtual,
            dto.RomIds ?? new List<int>());
}
