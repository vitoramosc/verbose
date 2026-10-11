using Microsoft.Extensions.Options;
using Verbose.Application.CasosDeUso;
using Verbose.Application.PortasDeSaida;
using Verbose.Application.Servicos;
using Verbose.Infrastructure.Email;
using Verbose.Infrastructure.Mensageria;
using Verbose.Infrastructure.Ollama;
using Verbose.Infrastructure.Persistencia;
using Verbose.Worker;
using Verbose.Worker.Configuracao;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<OpcoesDePersistencia>()
    .Bind(builder.Configuration.GetSection(OpcoesDePersistencia.NomeSecao))
    .Validate(
        opcoes => !string.IsNullOrWhiteSpace(opcoes.DiretorioDados),
        "A configuração Persistencia:DiretorioDados deve ser informada.")
    .ValidateOnStart();
builder.Services
    .AddOptions<OpcoesDoOllama>()
    .Bind(builder.Configuration.GetSection(OpcoesDoOllama.NomeSecao))
    .Validate(opcoes => Uri.TryCreate(opcoes.UrlBase, UriKind.Absolute, out _), "Ollama:UrlBase deve ser uma URL absoluta.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.Modelo), "Ollama:Modelo deve ser informado.")
    .Validate(opcoes => opcoes.TempoLimiteEmSegundos > 0, "Ollama:TempoLimiteEmSegundos deve ser positivo.")
    .ValidateOnStart();
builder.Services
    .AddOptions<OpcoesDeEmail>()
    .Bind(builder.Configuration.GetSection(OpcoesDeEmail.NomeSecao))
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.Servidor), "Email:Servidor deve ser informado.")
    .Validate(opcoes => opcoes.Porta is > 0 and <= 65535, "Email:Porta deve ser válida.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.Remetente), "Email:Remetente deve ser informado.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.SenhaDeAplicativo), "Email:SenhaDeAplicativo deve ser informada.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.Destinatario), "Email:Destinatario deve ser informado.")
    .ValidateOnStart();

builder.Services.Configure<OpcoesDeExecucaoManual>(
    builder.Configuration.GetSection(OpcoesDeExecucaoManual.NomeSecao));
builder.Services.Configure<OpcoesDePublicacaoManual>(
    builder.Configuration.GetSection(OpcoesDePublicacaoManual.NomeSecao));
builder.Services
    .AddOptions<OpcoesDeAgendamento>()
    .Bind(builder.Configuration.GetSection(OpcoesDeAgendamento.NomeSecao))
    .Validate(opcoes => opcoes.TentarObterHorario(out _), "Agendamento:Horario deve usar o formato HH:mm.")
    .Validate(
        opcoes => TimeZoneInfo.TryFindSystemTimeZoneById(opcoes.FusoHorario, out _),
        "Agendamento:FusoHorario deve ser um identificador de fuso horário válido.")
    .ValidateOnStart();
builder.Services
    .AddOptions<OpcoesDoRabbitMq>()
    .Bind(builder.Configuration.GetSection(OpcoesDoRabbitMq.NomeSecao))
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.Host), "RabbitMq:Host deve ser informado.")
    .Validate(opcoes => opcoes.Porta is > 0 and <= 65535, "RabbitMq:Porta deve ser válida.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.Usuario), "RabbitMq:Usuario deve ser informado.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.Senha), "RabbitMq:Senha deve ser informada.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.NomeDaExchange), "RabbitMq:NomeDaExchange deve ser informado.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.NomeDaFila), "RabbitMq:NomeDaFila deve ser informado.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.ChaveDeRoteamento), "RabbitMq:ChaveDeRoteamento deve ser informada.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.NomeDaExchangeDeFalhas), "RabbitMq:NomeDaExchangeDeFalhas deve ser informado.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.NomeDaFilaDeFalhas), "RabbitMq:NomeDaFilaDeFalhas deve ser informada.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.ChaveDeRoteamentoDeFalhas), "RabbitMq:ChaveDeRoteamentoDeFalhas deve ser informada.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.NomeDaExchangeDeTentativas), "RabbitMq:NomeDaExchangeDeTentativas deve ser informado.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.NomeDaFilaDeTentativas), "RabbitMq:NomeDaFilaDeTentativas deve ser informada.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.ChaveDeRoteamentoDeTentativas), "RabbitMq:ChaveDeRoteamentoDeTentativas deve ser informada.")
    .Validate(opcoes => opcoes.TempoDeEsperaDaTentativaEmSegundos > 0, "RabbitMq:TempoDeEsperaDaTentativaEmSegundos deve ser positivo.")
    .Validate(opcoes => opcoes.MaximoDeNovasTentativas >= 0, "RabbitMq:MaximoDeNovasTentativas não pode ser negativo.")
    .ValidateOnStart();

var diretorioDeConteudo = builder.Environment.ContentRootPath;

builder.Services.AddSingleton<IRepositorioDePalavrasDoDia>(provedorDeServicos =>
{
    var opcoes = provedorDeServicos
        .GetRequiredService<IOptions<OpcoesDePersistencia>>()
        .Value;
    var diretorioDados = Path.IsPathFullyQualified(opcoes.DiretorioDados)
        ? opcoes.DiretorioDados
        : Path.Combine(diretorioDeConteudo, opcoes.DiretorioDados);

    return new RepositorioDePalavrasEmJson(Path.GetFullPath(diretorioDados));
});

builder.Services.AddSingleton(provedorDeServicos =>
{
    var opcoes = provedorDeServicos
        .GetRequiredService<IOptions<OpcoesDoRabbitMq>>()
        .Value;

    return new PublicadorDeSolicitacaoNoRabbitMq(opcoes);
});
builder.Services.AddSingleton<IPublicadorDeSolicitacaoDePalavraDoDia>(provedorDeServicos =>
    provedorDeServicos.GetRequiredService<PublicadorDeSolicitacaoNoRabbitMq>());
builder.Services.AddSingleton(provedorDeServicos =>
{
    var opcoes = provedorDeServicos.GetRequiredService<IOptions<OpcoesDoOllama>>().Value;

    return new HttpClient
    {
        BaseAddress = new Uri(opcoes.UrlBase, UriKind.Absolute),
        Timeout = TimeSpan.FromSeconds(opcoes.TempoLimiteEmSegundos)
    };
});
builder.Services.AddSingleton<IGeradorDePalavraDoDia>(provedorDeServicos =>
{
    var clienteHttp = provedorDeServicos.GetRequiredService<HttpClient>();
    var opcoes = provedorDeServicos.GetRequiredService<IOptions<OpcoesDoOllama>>().Value;

    return new GeradorDePalavraDoDiaNoOllama(clienteHttp, opcoes);
});
builder.Services.AddSingleton<GeradorDeHtmlDaPalavraDoDia>();
builder.Services.AddSingleton<IEmailService>(provedorDeServicos =>
{
    var opcoes = provedorDeServicos.GetRequiredService<IOptions<OpcoesDeEmail>>().Value;
    var geradorDeHtml = provedorDeServicos.GetRequiredService<GeradorDeHtmlDaPalavraDoDia>();

    return new EmailServiceSmtp(opcoes, geradorDeHtml);
});
builder.Services.AddSingleton(provedorDeServicos =>
{
    var opcoes = provedorDeServicos
        .GetRequiredService<IOptions<OpcoesDoRabbitMq>>()
        .Value;

    var logger = provedorDeServicos
        .GetRequiredService<ILogger<ConsumidorDeSolicitacoesNoRabbitMq>>();

    return new ConsumidorDeSolicitacoesNoRabbitMq(opcoes, logger);
});

builder.Services.AddTransient<RegistrarPalavraDoDia>();
builder.Services.AddTransient<GerarERegistrarPalavraDoDia>();
builder.Services.AddTransient<NotificacaoService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<ServicoDePublicacaoManual>();
builder.Services.AddHostedService<ServicoDeAgendamentoDiario>();
builder.Services.AddHostedService<ServicoDeConsumoDeSolicitacoes>();
builder.Services.AddHostedService<ServicoDeExecucaoManual>();
builder.Services.AddHostedService<ServicoInicial>();

var host = builder.Build();
host.Run();
