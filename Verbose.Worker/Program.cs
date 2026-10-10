using Verbose.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ServicoInicial>();

var host = builder.Build();
host.Run();
