# Contexto do Projeto Verbose

## Visão geral

Verbose é um serviço diário local de aprendizado de vocabulário em português brasileiro. A aplicação deverá selecionar uma palavra relativamente incomum, útil e contemporânea, produzir definição e exemplo, e enviar esse conteúdo por e-mail.

O escopo da primeira versão é local: agendamento diário, RabbitMQ, Ollama, persistência em JSON, geração de HTML em C# e envio via SMTP.

## Estado atual

A solução `Verbose.sln`, o projeto `Verbose.Worker` e a biblioteca `Verbose.Domain` foram criados. O Worker possui somente `ServicoInicial`, um serviço hospedado que registra sua inicialização e permanece ativo até receber o cancelamento de `Ctrl+C`.

`Verbose.Domain` contém `PalavraDoDia` e `RegistroPalavraDoDia`. `PalavraDoDia` representa palavra, definição e exemplo já validados; `RegistroPalavraDoDia` associa esse conteúdo a uma data de referência e controla a transição de `PendenteEnvio` para `Enviada`. Ambos permanecem independentes de arquivo, RabbitMQ, Ollama, SMTP ou Worker. A validação atual exige campos não vazios, remove espaços nas extremidades e limita os tamanhos a 60, 400 e 500 caracteres, respectivamente.

`Verbose.Application` contém a porta de saída `IRepositorioDePalavrasDoDia`, com operações específicas para buscar um registro pela data, verificar se uma palavra já foi usada e salvar um registro. `Verbose.Infrastructure` contém `RepositorioDePalavrasEmJson`, implementação dessa porta responsável exclusivamente por converter registros de domínio para o formato de `data/used-words.json` e vice-versa. Arquivo ausente equivale a uma lista vazia; JSON inválido gera uma falha explícita. A gravação usa arquivo temporário no mesmo diretório e só então substitui o destino, reduzindo o risco de corromper o arquivo existente em uma falha durante a escrita.

O primeiro caso de uso é `RegistrarPalavraDoDia`. Ele verifica data e palavra antes de persistir um registro pendente e devolve um resultado explícito para data já utilizada ou palavra já utilizada. Essas situações são resultados esperados de negócio; falhas de arquivo continuam sendo exceções técnicas. `Verbose.Application.Testes` usa um repositório em memória exclusivamente nos testes para validar o caso de uso sem depender de JSON.

O Worker compõe as dependências: lê `Persistencia:DiretorioDados` de `appsettings.json` por meio de `OpcoesDePersistencia`, valida que o valor está preenchido, resolve caminhos relativos a partir da pasta de conteúdo do Worker e registra `RepositorioDePalavrasEmJson` como a implementação de `IRepositorioDePalavrasDoDia`. O valor padrão `../data` aponta para `data/` na raiz do repositório. Nenhum caso de uso é executado automaticamente na inicialização.

Para validação local, `ServicoDeExecucaoManual` pode ser ativado apenas em `Development` com a seção `ExecucaoManual` de `appsettings.Development.json`, arquivo ignorado pelo Git. O arquivo versionado `appsettings.Manual.example.json` demonstra o formato. Quando ativo, o serviço executa `RegistrarPalavraDoDia` uma única vez, registra o resultado no log e encerra o host. Essa ferramenta não substitui o fluxo futuro de RabbitMQ e Ollama.

A execução manual foi validada localmente com variáveis de ambiente temporárias: a primeira execução criou `data/used-words.json` com um registro `PendenteEnvio`; uma segunda execução com a mesma data retornou `DataJaUtilizada` sem acrescentar outro registro. A suíte atual possui seis testes automatizados, executados com `dotnet test Verbose.sln`.

`Verbose.Infrastructure.Testes` valida a persistência em diretórios temporários, sem criar ou alterar o arquivo de dados real. Os cenários atuais cobrem arquivo ausente, gravação e recuperação de um registro enviado, e JSON inválido. Os testes são executados com `dotnet test Verbose.sln`.

O projeto compila com `dotnet build Verbose.sln` e inicia com `dotnet run --project Verbose.Worker`. Não há RabbitMQ, Ollama, persistência JSON, e-mail ou configuração de segredos implementados ainda.

O ambiente local necessário para a V1 foi preparado e validado: .NET, RabbitMQ, Erlang, driver NVIDIA e Ollama estão operacionais. O modelo local `qwen3:8b` foi testado com execução integral na GPU. Detalhes de sistema operacional, hardware, contas e instalações locais não são versionados neste repositório público.

## Decisões confirmadas

- Linguagem e plataforma: C# com .NET Worker Service.
- IA: Ollama local, usando inicialmente `qwen3:8b`.
- A aplicação chamará a API local do Ollama com saída estruturada por JSON Schema e `think: false`; a saída continuará sendo validada pela aplicação.
- Mensageria: RabbitMQ local.
- Persistência: arquivo `data/used-words.json`, gerenciado exclusivamente pela aplicação com `System.Text.Json`.
- Cada registro persistido deverá manter palavra, definição, exemplo, data de referência e status de envio.
- Estados previstos para o envio: `PendenteEnvio` e `Enviada`.
- E-mail: HTML gerado em C#; IA produz somente palavra, definição e exemplo.
- A V1 será um monólito organizado, sem Docker, SQL, Redis, microserviços, nuvem ou APIs externas de IA.
- Segredos de desenvolvimento ficarão em .NET User Secrets; configurações de execução futuras usarão variáveis de ambiente ou arquivos locais ignorados pelo Git.
- Dados gerados em `data/`, incluindo `used-words.json`, não serão versionados.
- Fluxo Git: branches usam `feature/` ou `fix/`; commits usam `feat:` ou `fix:`. As entregas coerentes da V1 são integradas em `development`; `main` recebe somente a V1 completa e validada.

## Arquitetura aprovada para implementação gradual

A divisão planejada possui quatro projetos:

- `Verbose.Domain`: conceitos e regras de negócio independentes de tecnologia.
- `Verbose.Application`: casos de uso e portas necessárias para tecnologias externas.
- `Verbose.Infrastructure`: implementações para JSON, Ollama, RabbitMQ e SMTP.
- `Verbose.Worker`: ponto de entrada, composição de dependências e serviços hospedados.

As interfaces serão criadas somente para fronteiras concretas. As fronteiras inicialmente previstas são repositório de palavras, geração pelo Ollama e notificação.

O Worker conterá dois papéis distintos:

- agendador, que publica uma solicitação diária;
- consumidor, que recebe a solicitação e inicia o caso de uso.

Para a V1, haverá uma única instância do Worker e um consumidor processando uma mensagem por vez. A mensagem terá uma data de referência, usada para tornar a execução diária idempotente: publicações duplicadas para a mesma data não devem gerar ou enviar uma nova palavra.

## Consistência e limitações conhecidas

O fluxo planejado é:

1. Verificar se já existe registro para a data de referência.
2. Gerar e validar o conteúdo, se ainda não houver registro.
3. Persistir como `PendenteEnvio` de forma segura.
4. Enviar o e-mail.
5. Atualizar o registro para `Enviada`.
6. Confirmar a mensagem no RabbitMQ somente após a conclusão definida para a etapa.

Sem banco de dados e sem transação com SMTP, uma falha após o SMTP aceitar o e-mail e antes da atualização para `Enviada` pode causar reenvio. A V1 aceitará esse risco residual, preferindo potencial duplicidade a perder uma palavra.

O critério “relativamente incomum” não será mensurado automaticamente com precisão na V1, pois isso exigiria uma base lexical ou de frequência adicional. O prompt será calibrado e os resultados iniciais serão avaliados manualmente. JSON válido não é, por si só, garantia de qualidade editorial.

## Próxima etapa

Concluir a entrega de persistência com revisão, commit e integração em `development`. A próxima funcionalidade planejada é introduzir RabbitMQ gradualmente, começando pela conexão local e pelos conceitos de fila, mensagem e publicação.
