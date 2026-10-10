using Microsoft.Extensions.Options;
using Verbose.Application.CasosDeUso;
using Verbose.Application.PortasDeSaida;
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

builder.Services.Configure<OpcoesDeExecucaoManual>(
    builder.Configuration.GetSection(OpcoesDeExecucaoManual.NomeSecao));

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

builder.Services.AddTransient<RegistrarPalavraDoDia>();
builder.Services.AddHostedService<ServicoDeExecucaoManual>();
builder.Services.AddHostedService<ServicoInicial>();

var host = builder.Build();
host.Run();
