using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
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

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<OpcoesDePersistencia>()
    .Bind(builder.Configuration.GetSection(OpcoesDePersistencia.NomeSecao))
    .Validate(
        opcoes => !string.IsNullOrWhiteSpace(opcoes.DiretorioDados),
        "A configuração Persistencia:DiretorioDados deve ser informada.")
    .ValidateOnStart();

builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opcoes.AddPolicy("inscricoes", contexto =>
    {
        var chaveDaParticao = contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

        return RateLimitPartition.GetFixedWindowLimiter(
            chaveDaParticao,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    opcoes.AddPolicy("confirmacoes", contexto =>
    {
        var chaveDaParticao = contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

        return RateLimitPartition.GetFixedWindowLimiter(
            chaveDaParticao,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});
builder.Services
    .AddOptions<OpcoesDeInscricao>()
    .Bind(builder.Configuration.GetSection(OpcoesDeInscricao.NomeSecao))
    .Validate(
        opcoes => Uri.TryCreate(opcoes.UrlBaseDeConfirmacao, UriKind.Absolute, out var url) &&
            (string.Equals(url.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)),
        "Inscricao:UrlBaseDeConfirmacao deve ser uma URL HTTP ou HTTPS absoluta.")
    .Validate(
        opcoes => opcoes.HorasDeValidadeDaConfirmacao is > 0 and <= 168,
        "Inscricao:HorasDeValidadeDaConfirmacao deve estar entre 1 e 168.")
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
    .ValidateOnStart();

builder.Services.Configure<OpcoesDeExecucaoManual>(
    builder.Configuration.GetSection(OpcoesDeExecucaoManual.NomeSecao));
builder.Services.Configure<OpcoesDePublicacaoManual>(
    builder.Configuration.GetSection(OpcoesDePublicacaoManual.NomeSecao));
builder.Services
    .AddOptions<OpcoesDeAgendamento>()
    .Bind(builder.Configuration.GetSection(OpcoesDeAgendamento.NomeSecao))
    .Validate(
        opcoes => opcoes.ExecutarImediatamente || opcoes.TentarObterHorario(out _),
        "Agendamento:Horario deve usar o formato HH:mm ou o valor agora em Development.")
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
builder.Services.AddSingleton<IRepositorioDeDestinatarios>(provedorDeServicos =>
{
    var opcoes = provedorDeServicos
        .GetRequiredService<IOptions<OpcoesDePersistencia>>()
        .Value;
    var diretorioDados = Path.IsPathFullyQualified(opcoes.DiretorioDados)
        ? opcoes.DiretorioDados
        : Path.Combine(diretorioDeConteudo, opcoes.DiretorioDados);

    return new RepositorioDeDestinatariosEmJson(Path.GetFullPath(diretorioDados));
});
builder.Services.AddSingleton<IRepositorioDeEnviosDaPalavraDoDia>(provedorDeServicos =>
{
    var opcoes = provedorDeServicos
        .GetRequiredService<IOptions<OpcoesDePersistencia>>()
        .Value;
    var diretorioDados = Path.IsPathFullyQualified(opcoes.DiretorioDados)
        ? opcoes.DiretorioDados
        : Path.Combine(diretorioDeConteudo, opcoes.DiretorioDados);

    return new RepositorioDeEnviosDaPalavraDoDiaEmJson(Path.GetFullPath(diretorioDados));
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
builder.Services.AddSingleton<IRevisorEditorialDePalavraDoDia>(provedorDeServicos =>
{
    var clienteHttp = provedorDeServicos.GetRequiredService<HttpClient>();
    var opcoes = provedorDeServicos.GetRequiredService<IOptions<OpcoesDoOllama>>().Value;
    var logger = provedorDeServicos
        .GetRequiredService<ILogger<RevisorEditorialDePalavraDoDiaNoOllama>>();

    return new RevisorEditorialDePalavraDoDiaNoOllama(clienteHttp, opcoes, logger);
});
builder.Services.AddSingleton<GeradorDeHtmlDaPalavraDoDia>();
builder.Services.AddSingleton<GeradorDeHtmlDeConfirmacaoDeInscricao>();
builder.Services.AddSingleton<IEmailService>(provedorDeServicos =>
{
    var opcoes = provedorDeServicos.GetRequiredService<IOptions<OpcoesDeEmail>>().Value;
    var geradorDeHtml = provedorDeServicos.GetRequiredService<GeradorDeHtmlDaPalavraDoDia>();
    var geradorDeHtmlDeConfirmacao = provedorDeServicos
        .GetRequiredService<GeradorDeHtmlDeConfirmacaoDeInscricao>();

    return new EmailServiceSmtp(opcoes, geradorDeHtml, geradorDeHtmlDeConfirmacao);
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
builder.Services.AddTransient<CadastrarDestinatario>(provedorDeServicos =>
{
    var opcoes = provedorDeServicos.GetRequiredService<IOptions<OpcoesDeInscricao>>().Value;

    return new CadastrarDestinatario(
        provedorDeServicos.GetRequiredService<IRepositorioDeDestinatarios>(),
        provedorDeServicos.GetRequiredService<IEmailService>(),
        provedorDeServicos.GetRequiredService<TimeProvider>(),
        new Uri(opcoes.UrlBaseDeConfirmacao, UriKind.Absolute),
        TimeSpan.FromHours(opcoes.HorasDeValidadeDaConfirmacao));
});
builder.Services.AddTransient<ConfirmarInscricao>();
builder.Services.AddTransient<CancelarInscricao>();
builder.Services.AddTransient<ValidadorEditorialDaPalavraDoDia>();
builder.Services.AddTransient<GerarERegistrarPalavraDoDia>();
builder.Services.AddTransient<NotificacaoService>(provedorDeServicos =>
{
    var opcoes = provedorDeServicos.GetRequiredService<IOptions<OpcoesDeInscricao>>().Value;

    return new NotificacaoService(
        provedorDeServicos.GetRequiredService<IRepositorioDePalavrasDoDia>(),
        provedorDeServicos.GetRequiredService<IRepositorioDeDestinatarios>(),
        provedorDeServicos.GetRequiredService<IRepositorioDeEnviosDaPalavraDoDia>(),
        provedorDeServicos.GetRequiredService<IEmailService>(),
        new Uri(opcoes.UrlBaseDeConfirmacao, UriKind.Absolute));
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<ServicoDePublicacaoManual>();
builder.Services.AddHostedService<ServicoDeAgendamentoDiario>();
builder.Services.AddHostedService<ServicoDeConsumoDeSolicitacoes>();
builder.Services.AddHostedService<ServicoDeExecucaoManual>();
builder.Services.AddHostedService<ServicoInicial>();

var aplicacao = builder.Build();
aplicacao.UseDefaultFiles();
aplicacao.UseStaticFiles();
aplicacao.UseRateLimiter();
aplicacao.MapearEndpointsDeInscricao();
aplicacao.Run();
