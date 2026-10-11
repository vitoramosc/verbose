using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Verbose.Infrastructure.Email;
using Verbose.Application.Mensagens;

namespace Verbose.Infrastructure.Mensageria;

public sealed class ConsumidorDeSolicitacoesNoRabbitMq : IAsyncDisposable
{
    private readonly ConnectionFactory fabricaDeConexoes;
    private readonly ILogger<ConsumidorDeSolicitacoesNoRabbitMq> logger;
    private readonly OpcoesDoRabbitMq opcoes;
    private IConnection? conexao;
    private IChannel? canal;
    private string? identificadorDoConsumidor;
    private bool foiDescartado;

    public ConsumidorDeSolicitacoesNoRabbitMq(
        OpcoesDoRabbitMq opcoes,
        ILogger<ConsumidorDeSolicitacoesNoRabbitMq> logger)
    {
        this.opcoes = opcoes;
        this.logger = logger;
        fabricaDeConexoes = new ConnectionFactory
        {
            HostName = opcoes.Host,
            Port = opcoes.Porta,
            UserName = opcoes.Usuario,
            Password = opcoes.Senha,
            ClientProvidedName = "verbose:consumidor-palavra-do-dia"
        };
    }

    public async Task IniciarAsync(
        Func<SolicitacaoDeGeracaoDaPalavraDoDia, CancellationToken, Task> processarSolicitacaoAsync,
        CancellationToken tokenCancelamento = default)
    {
        ObjectDisposedException.ThrowIf(foiDescartado, this);
        ArgumentNullException.ThrowIfNull(processarSolicitacaoAsync);

        if (canal is { IsOpen: true })
        {
            throw new InvalidOperationException("O consumidor RabbitMQ já foi iniciado.");
        }

        conexao = await fabricaDeConexoes.CreateConnectionAsync(
            "verbose:consumidor-palavra-do-dia",
            tokenCancelamento);
        canal = await conexao.CreateChannelAsync(cancellationToken: tokenCancelamento);

        await TopologiaDeGeracaoDaPalavraDoDiaNoRabbitMq.DeclararAsync(
            canal,
            opcoes,
            tokenCancelamento);
        await canal.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: tokenCancelamento);

        var consumidor = new AsyncEventingBasicConsumer(canal);
        consumidor.ReceivedAsync += async (_, entrega) =>
        {
            try
            {
                var solicitacao = JsonSerializer.Deserialize<SolicitacaoDeGeracaoDaPalavraDoDia>(
                    entrega.Body.Span,
                    JsonSerializerOptions.Web)
                    ?? throw new JsonException("A mensagem recebida não contém uma solicitação válida.");

                await processarSolicitacaoAsync(solicitacao, tokenCancelamento);
                await canal.BasicAckAsync(
                    entrega.DeliveryTag,
                    multiple: false,
                    cancellationToken: tokenCancelamento);
            }
            catch (Exception excecao) when (excecao is JsonException or ArgumentException)
            {
                logger.LogError(
                    excecao,
                    "Mensagem inválida recebida. Ela será encaminhada para a fila de falhas.");
                await canal.BasicNackAsync(
                    entrega.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: tokenCancelamento);
            }
            catch (FalhaPermanenteDeEmailException excecao)
            {
                logger.LogError(
                    excecao,
                    "Falha permanente no envio de e-mail. A mensagem será encaminhada para a fila de falhas sem novas tentativas.");
                await canal.BasicNackAsync(
                    entrega.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: tokenCancelamento);
            }
            catch (Exception excecao) when (!tokenCancelamento.IsCancellationRequested)
            {
                var tentativaAtual = ObterTentativa(entrega.BasicProperties.Headers);

                if (tentativaAtual >= opcoes.MaximoDeNovasTentativas)
                {
                    logger.LogError(excecao, "Falha temporária excedeu {MaximoDeNovasTentativas} novas tentativas. A mensagem irá para a DLQ.", opcoes.MaximoDeNovasTentativas);
                    await canal.BasicNackAsync(entrega.DeliveryTag, false, false, tokenCancelamento);
                    return;
                }

                var proximaTentativa = tentativaAtual + 1;
                logger.LogWarning(excecao, "Falha temporária. A mensagem será tentada novamente pela vez {Tentativa}.", proximaTentativa);
                await canal.BasicPublishAsync(
                    opcoes.NomeDaExchangeDeTentativas,
                    opcoes.ChaveDeRoteamentoDeTentativas,
                    true,
                    new BasicProperties
                    {
                        ContentType = entrega.BasicProperties.ContentType,
                        DeliveryMode = DeliveryModes.Persistent,
                        Headers = new Dictionary<string, object?> { ["x-verbose-tentativa"] = proximaTentativa }
                    },
                    entrega.Body.ToArray(),
                    tokenCancelamento);
                await canal.BasicAckAsync(entrega.DeliveryTag, false, tokenCancelamento);
            }
        };

        identificadorDoConsumidor = await canal.BasicConsumeAsync(
            queue: opcoes.NomeDaFila,
            autoAck: false,
            consumer: consumidor,
            cancellationToken: tokenCancelamento);
    }

    public async ValueTask DisposeAsync()
    {
        if (foiDescartado)
        {
            return;
        }

        foiDescartado = true;

        if (canal is { IsOpen: true } && identificadorDoConsumidor is not null)
        {
            await canal.BasicCancelAsync(identificadorDoConsumidor);
        }

        if (canal is not null)
        {
            await canal.DisposeAsync();
        }

        if (conexao is not null)
        {
            await conexao.DisposeAsync();
        }
    }

    private static int ObterTentativa(IDictionary<string, object?>? cabecalhos)
    {
        if (cabecalhos?.TryGetValue("x-verbose-tentativa", out var valor) != true || valor is null)
        {
            return 0;
        }

        return Convert.ToInt32(valor, System.Globalization.CultureInfo.InvariantCulture);
    }
}
