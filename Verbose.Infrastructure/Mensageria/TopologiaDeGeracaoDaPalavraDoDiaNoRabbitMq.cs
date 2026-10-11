using RabbitMQ.Client;

namespace Verbose.Infrastructure.Mensageria;

internal static class TopologiaDeGeracaoDaPalavraDoDiaNoRabbitMq
{
    public static async Task DeclararAsync(
        IChannel canal,
        OpcoesDoRabbitMq opcoes,
        CancellationToken tokenCancelamento)
    {
        await canal.ExchangeDeclareAsync(exchange: opcoes.NomeDaExchangeDeTentativas, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, cancellationToken: tokenCancelamento);
        await canal.QueueDeclareAsync(
            opcoes.NomeDaFilaDeTentativas, true, false, false,
            new Dictionary<string, object?>
            {
                ["x-message-ttl"] = opcoes.TempoDeEsperaDaTentativaEmSegundos * 1000,
                ["x-dead-letter-exchange"] = opcoes.NomeDaExchange,
                ["x-dead-letter-routing-key"] = opcoes.ChaveDeRoteamento
            }, cancellationToken: tokenCancelamento);
        await canal.QueueBindAsync(queue: opcoes.NomeDaFilaDeTentativas, exchange: opcoes.NomeDaExchangeDeTentativas, routingKey: opcoes.ChaveDeRoteamentoDeTentativas, arguments: null, cancellationToken: tokenCancelamento);

        await canal.ExchangeDeclareAsync(
            exchange: opcoes.NomeDaExchangeDeFalhas,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: tokenCancelamento);
        await canal.QueueDeclareAsync(
            queue: opcoes.NomeDaFilaDeFalhas,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: tokenCancelamento);
        await canal.QueueBindAsync(
            queue: opcoes.NomeDaFilaDeFalhas,
            exchange: opcoes.NomeDaExchangeDeFalhas,
            routingKey: opcoes.ChaveDeRoteamentoDeFalhas,
            arguments: null,
            cancellationToken: tokenCancelamento);

        await canal.ExchangeDeclareAsync(
            exchange: opcoes.NomeDaExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: tokenCancelamento);
        await canal.QueueDeclareAsync(
            queue: opcoes.NomeDaFila,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = opcoes.NomeDaExchangeDeFalhas,
                ["x-dead-letter-routing-key"] = opcoes.ChaveDeRoteamentoDeFalhas
            },
            cancellationToken: tokenCancelamento);
        await canal.QueueBindAsync(
            queue: opcoes.NomeDaFila,
            exchange: opcoes.NomeDaExchange,
            routingKey: opcoes.ChaveDeRoteamento,
            arguments: null,
            cancellationToken: tokenCancelamento);
    }
}
