using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Verbose.Application.Mensagens;
using Verbose.Application.PortasDeSaida;

namespace Verbose.Infrastructure.Mensageria;

public sealed class PublicadorDeSolicitacaoNoRabbitMq : IPublicadorDeSolicitacaoDePalavraDoDia, IAsyncDisposable
{
    private readonly ConnectionFactory fabricaDeConexoes;
    private readonly SemaphoreSlim bloqueioDaConexao = new(1, 1);
    private readonly OpcoesDoRabbitMq opcoes;
    private IConnection? conexao;

    public PublicadorDeSolicitacaoNoRabbitMq(OpcoesDoRabbitMq opcoes)
    {
        this.opcoes = opcoes;
        fabricaDeConexoes = new ConnectionFactory
        {
            HostName = opcoes.Host,
            Port = opcoes.Porta,
            UserName = opcoes.Usuario,
            Password = opcoes.Senha,
            ClientProvidedName = "verbose:publicador-palavra-do-dia"
        };
    }

    public async Task PublicarAsync(
        SolicitacaoDeGeracaoDaPalavraDoDia solicitacao,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(solicitacao);

        var corpoDaMensagem = JsonSerializer.SerializeToUtf8Bytes(
            solicitacao,
            JsonSerializerOptions.Web);
        await PublicarCorpoDaMensagemAsync(corpoDaMensagem, tokenCancelamento);
    }

    public Task PublicarMensagemInvalidaParaTesteAsync(CancellationToken tokenCancelamento = default)
    {
        var corpoDaMensagemInvalida = Encoding.UTF8.GetBytes("{\"dataReferencia\":");

        return PublicarCorpoDaMensagemAsync(corpoDaMensagemInvalida, tokenCancelamento);
    }

    public async ValueTask DisposeAsync()
    {
        bloqueioDaConexao.Dispose();

        if (conexao is not null)
        {
            await conexao.DisposeAsync();
        }
    }

    private async Task PublicarCorpoDaMensagemAsync(
        ReadOnlyMemory<byte> corpoDaMensagem,
        CancellationToken tokenCancelamento)
    {
        var conexaoAberta = await ObterConexaoAsync(tokenCancelamento);
        await using var canal = await conexaoAberta.CreateChannelAsync(cancellationToken: tokenCancelamento);

        await TopologiaDeGeracaoDaPalavraDoDiaNoRabbitMq.DeclararAsync(
            canal,
            opcoes,
            tokenCancelamento);

        var propriedades = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await canal.BasicPublishAsync(
            exchange: opcoes.NomeDaExchange,
            routingKey: opcoes.ChaveDeRoteamento,
            mandatory: true,
            basicProperties: propriedades,
            body: corpoDaMensagem,
            cancellationToken: tokenCancelamento);
    }

    private async Task<IConnection> ObterConexaoAsync(CancellationToken tokenCancelamento)
    {
        if (conexao is { IsOpen: true })
        {
            return conexao;
        }

        await bloqueioDaConexao.WaitAsync(tokenCancelamento);

        try
        {
            if (conexao is { IsOpen: true })
            {
                return conexao;
            }

            if (conexao is not null)
            {
                await conexao.DisposeAsync();
            }

            conexao = await fabricaDeConexoes.CreateConnectionAsync(
                "verbose:publicador-palavra-do-dia",
                tokenCancelamento);

            return conexao;
        }
        finally
        {
            bloqueioDaConexao.Release();
        }
    }
}
