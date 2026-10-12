using System.Text.Json;
using System.Text.Encodings.Web;
using Verbose.Application.PortasDeSaida;
using Verbose.Domain;

namespace Verbose.Infrastructure.Persistencia;

public sealed class RepositorioDeEnviosDaPalavraDoDiaEmJson(string diretorioDados) : IRepositorioDeEnviosDaPalavraDoDia
{
    private const string NomeArquivo = "envios-palavras.json";
    private static readonly JsonSerializerOptions Opcoes = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private readonly string caminho = Path.Combine(Path.GetFullPath(diretorioDados), NomeArquivo);
    private readonly SemaphoreSlim trava = new(1, 1);

    public async Task<EnvioDaPalavraDoDia> ObterOuCriarAsync(DateOnly data, string email, CancellationToken ct = default)
    {
        await trava.WaitAsync(ct); try { var todos = (await Ler(ct)).ToList(); var item = todos.SingleOrDefault(x => x.DataReferencia == data && x.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)); if (item is not null) return item; item = new EnvioDaPalavraDoDia(data, email); todos.Add(item); await Salvar(todos, ct); return item; } finally { trava.Release(); }
    }
    public async Task AtualizarAsync(EnvioDaPalavraDoDia envio, CancellationToken ct = default)
    {
        await trava.WaitAsync(ct); try { var todos = (await Ler(ct)).ToList(); var indice = todos.FindIndex(x => x.DataReferencia == envio.DataReferencia && x.Email.Equals(envio.Email, StringComparison.OrdinalIgnoreCase)); if (indice < 0) throw new InvalidOperationException("O envio não foi encontrado."); todos[indice] = envio; await Salvar(todos, ct); } finally { trava.Release(); }
    }
    private async Task<IReadOnlyCollection<EnvioDaPalavraDoDia>> Ler(CancellationToken ct)
    {
        if (!File.Exists(caminho)) return Array.Empty<EnvioDaPalavraDoDia>();
        try { await using var fluxo = File.OpenRead(caminho); var arquivo = await JsonSerializer.DeserializeAsync<Arquivo>(fluxo, Opcoes, ct) ?? throw new InvalidDataException("O arquivo de envios está vazio."); return arquivo.Envios.Select(x => { var item = new EnvioDaPalavraDoDia(x.DataReferencia, x.Email); if (x.FoiEnviado) item.MarcarComoEnviado(); return item; }).ToArray(); }
        catch (JsonException ex) { throw new InvalidDataException("O arquivo de envios contém JSON inválido.", ex); }
    }
    private async Task Salvar(IEnumerable<EnvioDaPalavraDoDia> envios, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!); var temporario = $"{caminho}.{Guid.NewGuid():N}.tmp";
        try { await using (var fluxo = new FileStream(temporario, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough)) { await JsonSerializer.SerializeAsync(fluxo, new Arquivo { Envios = envios.Select(x => new Item { DataReferencia = x.DataReferencia, Email = x.Email, FoiEnviado = x.FoiEnviado }).ToList() }, Opcoes, ct); await fluxo.FlushAsync(ct); } File.Move(temporario, caminho, true); } finally { if (File.Exists(temporario)) File.Delete(temporario); }
    }
    private sealed class Arquivo { public required List<Item> Envios { get; init; } }
    private sealed class Item { public required DateOnly DataReferencia { get; init; } public required string Email { get; init; } public required bool FoiEnviado { get; init; } }
}
