using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Verbose.Application.PortasDeSaida;
using Verbose.Domain;

namespace Verbose.Infrastructure.Persistencia;

public sealed class RepositorioDePalavrasEmJson : IRepositorioDePalavrasDoDia
{
    private const string NomeArquivo = "used-words.json";

    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters =
        {
            new JsonStringEnumConverter<StatusDeEnvio>(namingPolicy: null, allowIntegerValues: false)
        }
    };

    private readonly string caminhoArquivo;

    public RepositorioDePalavrasEmJson(string diretorioDados)
    {
        if (string.IsNullOrWhiteSpace(diretorioDados))
        {
            throw new ArgumentException("O diretório de dados deve ser informado.", nameof(diretorioDados));
        }

        caminhoArquivo = Path.Combine(Path.GetFullPath(diretorioDados), NomeArquivo);
    }

    public async Task<RegistroPalavraDoDia?> ObterPorDataAsync(
        DateOnly dataReferencia,
        CancellationToken tokenCancelamento = default)
    {
        var registros = await LerTodosAsync(tokenCancelamento);

        return registros.SingleOrDefault(registro => registro.DataReferencia == dataReferencia);
    }

    public async Task<bool> PalavraJaFoiUsadaAsync(
        string palavra,
        CancellationToken tokenCancelamento = default)
    {
        if (string.IsNullOrWhiteSpace(palavra))
        {
            throw new ArgumentException("A palavra deve ser informada.", nameof(palavra));
        }

        var palavraNormalizada = palavra.Trim();
        var registros = await LerTodosAsync(tokenCancelamento);

        return registros.Any(registro => string.Equals(
            registro.PalavraDoDia.Palavra,
            palavraNormalizada,
            StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyCollection<string>> ObterPalavrasUsadasAsync(
        CancellationToken tokenCancelamento = default)
    {
        var registros = await LerTodosAsync(tokenCancelamento);

        return registros.Select(registro => registro.PalavraDoDia.Palavra).ToArray();
    }

    public async Task SalvarAsync(
        RegistroPalavraDoDia registro,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(registro);

        var registros = (await LerTodosAsync(tokenCancelamento)).ToList();
        registros.Add(registro);

        await SalvarTodosAsync(registros, tokenCancelamento);
    }

    public async Task AtualizarAsync(
        RegistroPalavraDoDia registro,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(registro);

        var registros = (await LerTodosAsync(tokenCancelamento)).ToList();
        var indice = registros.FindIndex(item => item.DataReferencia == registro.DataReferencia);

        if (indice < 0)
        {
            throw new InvalidOperationException(
                $"Não existe registro para a data {registro.DataReferencia:yyyy-MM-dd}.");
        }

        registros[indice] = registro;
        await SalvarTodosAsync(registros, tokenCancelamento);
    }

    private async Task<IReadOnlyCollection<RegistroPalavraDoDia>> LerTodosAsync(
        CancellationToken tokenCancelamento = default)
    {
        if (!File.Exists(caminhoArquivo))
        {
            return Array.Empty<RegistroPalavraDoDia>();
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

            var arquivo = await JsonSerializer.DeserializeAsync<ArquivoDePalavrasUsadas>(
                fluxo,
                OpcoesJson,
                tokenCancelamento);

            if (arquivo is null)
            {
                throw new InvalidDataException("O arquivo de palavras usadas está vazio.");
            }

            return arquivo.Palavras.Select(ConverterParaDominio).ToArray();
        }
        catch (JsonException excecao)
        {
            throw new InvalidDataException(
                "O arquivo de palavras usadas contém JSON inválido.",
                excecao);
        }
    }

    private async Task SalvarTodosAsync(
        IEnumerable<RegistroPalavraDoDia> registros,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(registros);

        var arquivo = new ArquivoDePalavrasUsadas
        {
            Palavras = registros.Select(ConverterParaJson).ToList()
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

    private static RegistroPalavraDoDia ConverterParaDominio(RegistroPalavraDoDiaJson registro)
    {
        var palavraDoDia = new PalavraDoDia(registro.Palavra, registro.Definicao, registro.Exemplo);
        var registroDoDominio = new RegistroPalavraDoDia(registro.DataReferencia, palavraDoDia);

        if (registro.StatusDeEnvio == StatusDeEnvio.Enviada)
        {
            registroDoDominio.MarcarComoEnviada();
        }

        return registroDoDominio;
    }

    private static RegistroPalavraDoDiaJson ConverterParaJson(RegistroPalavraDoDia registro)
    {
        return new RegistroPalavraDoDiaJson
        {
            DataReferencia = registro.DataReferencia,
            Palavra = registro.PalavraDoDia.Palavra,
            Definicao = registro.PalavraDoDia.Definicao,
            Exemplo = registro.PalavraDoDia.Exemplo,
            StatusDeEnvio = registro.StatusDeEnvio
        };
    }

    private sealed class ArquivoDePalavrasUsadas
    {
        public required List<RegistroPalavraDoDiaJson> Palavras { get; init; }
    }

    private sealed class RegistroPalavraDoDiaJson
    {
        public required DateOnly DataReferencia { get; init; }

        public required string Palavra { get; init; }

        public required string Definicao { get; init; }

        public required string Exemplo { get; init; }

        public required StatusDeEnvio StatusDeEnvio { get; init; }
    }
}
