using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Verbose.Application.PortasDeSaida;
using Verbose.Domain;

namespace Verbose.Infrastructure.Persistencia;

public sealed class RepositorioDeDestinatariosEmJson : IRepositorioDeDestinatarios
{
    private const string NomeArquivo = "destinatarios.json";

    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters =
        {
            new JsonStringEnumConverter<StatusDaInscricao>(namingPolicy: null, allowIntegerValues: false)
        }
    };

    private readonly string caminhoArquivo;
    private readonly SemaphoreSlim exclusaoMutua = new(1, 1);

    public RepositorioDeDestinatariosEmJson(string diretorioDados)
    {
        if (string.IsNullOrWhiteSpace(diretorioDados))
        {
            throw new ArgumentException("O diretório de dados deve ser informado.", nameof(diretorioDados));
        }

        caminhoArquivo = Path.Combine(Path.GetFullPath(diretorioDados), NomeArquivo);
    }

    public async Task<ResultadoDaPersistenciaDaInscricao> CriarOuRenovarPendenteAsync(
        Destinatario destinatario,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(destinatario);

        await exclusaoMutua.WaitAsync(tokenCancelamento);
        try
        {
            var destinatarios = (await LerTodosAsync(tokenCancelamento)).ToList();

            var destinatarioExistente = destinatarios.SingleOrDefault(item => string.Equals(
                item.Email,
                destinatario.Email,
                StringComparison.OrdinalIgnoreCase));

            if (destinatarioExistente?.Status == StatusDaInscricao.Confirmada)
            {
                return ResultadoDaPersistenciaDaInscricao.JaConfirmada;
            }

            if (destinatarioExistente is not null)
            {
                destinatarioExistente.RenovarConfirmacao(
                    destinatario.HashDoTokenDeConfirmacao!,
                    destinatario.DataDeExpiracaoDaConfirmacao!.Value,
                    destinatario.DataDeInscricao);
            }
            else
            {
                destinatarios.Add(destinatario);
            }

            await SalvarTodosAsync(destinatarios, tokenCancelamento);

            return ResultadoDaPersistenciaDaInscricao.PendenteDeConfirmacao;
        }
        finally
        {
            exclusaoMutua.Release();
        }
    }

    public async Task<IReadOnlyCollection<Destinatario>> ObterConfirmadosAsync(
        CancellationToken tokenCancelamento = default)
    {
        await exclusaoMutua.WaitAsync(tokenCancelamento);
        try
        {
            return (await LerTodosAsync(tokenCancelamento))
                .Where(item => item.Status == StatusDaInscricao.Confirmada)
                .ToArray();
        }
        finally
        {
            exclusaoMutua.Release();
        }
    }

    public async Task<ResultadoDaConfirmacaoDaInscricao> ConfirmarPeloHashDoTokenAsync(
        string hashDoToken,
        DateTimeOffset instanteAtual,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashDoToken);

        await exclusaoMutua.WaitAsync(tokenCancelamento);
        try
        {
            var destinatarios = (await LerTodosAsync(tokenCancelamento)).ToList();
            var destinatario = destinatarios.FirstOrDefault(item =>
                item.ConfirmarSeTokenCorresponder(hashDoToken, instanteAtual));

            if (destinatario is null)
            {
                return ResultadoDaConfirmacaoDaInscricao.InvalidaOuExpirada;
            }

            await SalvarTodosAsync(destinatarios, tokenCancelamento);
            return ResultadoDaConfirmacaoDaInscricao.Confirmada;
        }
        finally
        {
            exclusaoMutua.Release();
        }
    }

    public async Task DefinirTokenDeCancelamentoAsync(
        string email,
        string hashDoToken,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashDoToken);

        await exclusaoMutua.WaitAsync(tokenCancelamento);
        try
        {
            var destinatarios = (await LerTodosAsync(tokenCancelamento)).ToList();
            var destinatario = destinatarios.SingleOrDefault(item => string.Equals(
                item.Email,
                email,
                StringComparison.OrdinalIgnoreCase));

            if (destinatario is null)
            {
                throw new InvalidOperationException("O destinatário não foi encontrado.");
            }

            destinatario.DefinirTokenDeCancelamento(hashDoToken);
            await SalvarTodosAsync(destinatarios, tokenCancelamento);
        }
        finally
        {
            exclusaoMutua.Release();
        }
    }

    public async Task<ResultadoDoCancelamentoDaInscricao> CancelarPeloHashDoTokenAsync(
        string hashDoToken,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashDoToken);

        await exclusaoMutua.WaitAsync(tokenCancelamento);
        try
        {
            var destinatarios = (await LerTodosAsync(tokenCancelamento)).ToList();
            var destinatario = destinatarios.FirstOrDefault(item =>
                item.CancelarSeTokenCorresponder(hashDoToken));

            if (destinatario is null)
            {
                return ResultadoDoCancelamentoDaInscricao.Invalida;
            }

            await SalvarTodosAsync(destinatarios, tokenCancelamento);
            return ResultadoDoCancelamentoDaInscricao.Cancelada;
        }
        finally
        {
            exclusaoMutua.Release();
        }
    }

    private async Task<IReadOnlyCollection<Destinatario>> LerTodosAsync(CancellationToken tokenCancelamento)
    {
        if (!File.Exists(caminhoArquivo))
        {
            return Array.Empty<Destinatario>();
        }

        try
        {
            await using var fluxo = new FileStream(
                caminhoArquivo,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous);

            var arquivo = await JsonSerializer.DeserializeAsync<ArquivoDeDestinatarios>(
                fluxo,
                OpcoesJson,
                tokenCancelamento);

            if (arquivo is null)
            {
                throw new InvalidDataException("O arquivo de destinatários está vazio.");
            }

            return arquivo.Destinatarios.Select(ConverterParaDominio).ToArray();
        }
        catch (JsonException excecao)
        {
            throw new InvalidDataException("O arquivo de destinatários contém JSON inválido.", excecao);
        }
        catch (ArgumentException excecao)
        {
            throw new InvalidDataException("O arquivo de destinatários contém dados inválidos.", excecao);
        }
    }

    private async Task SalvarTodosAsync(
        IEnumerable<Destinatario> destinatarios,
        CancellationToken tokenCancelamento)
    {
        var arquivo = new ArquivoDeDestinatarios
        {
            Destinatarios = destinatarios.Select(ConverterParaJson).ToList()
        };

        var diretorioDados = Path.GetDirectoryName(caminhoArquivo)!;
        Directory.CreateDirectory(diretorioDados);

        var caminhoTemporario = Path.Combine(
            diretorioDados,
            $".{NomeArquivo}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var fluxo = new FileStream(
                caminhoTemporario,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(fluxo, arquivo, OpcoesJson, tokenCancelamento);
                await fluxo.FlushAsync(tokenCancelamento);
            }

            File.Move(caminhoTemporario, caminhoArquivo, overwrite: true);
        }
        finally
        {
            if (File.Exists(caminhoTemporario))
            {
                File.Delete(caminhoTemporario);
            }
        }
    }

    private static Destinatario ConverterParaDominio(DestinatarioJson destinatario)
    {
        return Destinatario.Reidratar(
            destinatario.Email,
            destinatario.DataDeInscricao,
            destinatario.Status,
            destinatario.HashDoTokenDeConfirmacao,
            destinatario.DataDeExpiracaoDaConfirmacao,
            destinatario.HashDoTokenDeCancelamento);
    }

    private static DestinatarioJson ConverterParaJson(Destinatario destinatario)
    {
        return new DestinatarioJson
        {
            Email = destinatario.Email,
            DataDeInscricao = destinatario.DataDeInscricao,
            Status = destinatario.Status,
            HashDoTokenDeConfirmacao = destinatario.HashDoTokenDeConfirmacao,
            DataDeExpiracaoDaConfirmacao = destinatario.DataDeExpiracaoDaConfirmacao,
            HashDoTokenDeCancelamento = destinatario.HashDoTokenDeCancelamento
        };
    }

    private sealed class ArquivoDeDestinatarios
    {
        public required List<DestinatarioJson> Destinatarios { get; init; }
    }

    private sealed class DestinatarioJson
    {
        public required string Email { get; init; }

        public required DateTimeOffset DataDeInscricao { get; init; }

        public required StatusDaInscricao Status { get; init; }

        public string? HashDoTokenDeConfirmacao { get; init; }

        public DateTimeOffset? DataDeExpiracaoDaConfirmacao { get; init; }

        public string? HashDoTokenDeCancelamento { get; init; }
    }
}
