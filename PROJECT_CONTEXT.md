# Contexto do Projeto Verbose

## Visão geral

Verbose é um serviço diário local de aprendizado de vocabulário em português brasileiro. A aplicação deverá selecionar uma palavra relativamente incomum, útil e contemporânea, produzir definição e exemplo, e enviar esse conteúdo por e-mail.

O escopo da primeira versão é local: agendamento diário, RabbitMQ, Ollama, persistência em JSON, geração de HTML em C# e envio via SMTP.

O marco atual é uma versão local de aprendizado, identificada pelo usuário como `0.1`. A versão `1.0` será considerada somente quando houver um deploy real na internet.

## Estado atual

A solução `Verbose.sln` é composta por `Verbose.Domain`, `Verbose.Application`, `Verbose.Infrastructure`, `Verbose.Worker` e projetos de testes. O Worker registra sua inicialização, pode publicar uma solicitação manual em desenvolvimento e mantém um consumidor RabbitMQ ativo até receber o cancelamento de `Ctrl+C`.

`Verbose.Domain` contém `PalavraDoDia` e `RegistroPalavraDoDia`. `PalavraDoDia` representa palavra, definição e exemplo já validados; `RegistroPalavraDoDia` associa esse conteúdo a uma data de referência e controla a transição de `PendenteEnvio` para `Enviada`. Ambos permanecem independentes de arquivo, RabbitMQ, Ollama, SMTP ou Worker. A validação atual exige campos não vazios, remove espaços nas extremidades, limita os tamanhos a 60, 400 e 500 caracteres e aceita na palavra somente letras e hífen.

`Verbose.Application` contém a porta de saída `IRepositorioDePalavrasDoDia`, com operações específicas para buscar um registro pela data, verificar se uma palavra já foi usada e salvar um registro. `Verbose.Infrastructure` contém `RepositorioDePalavrasEmJson`, implementação dessa porta responsável exclusivamente por converter registros de domínio para o formato de `data/used-words.json` e vice-versa. Arquivo ausente equivale a uma lista vazia; JSON inválido gera uma falha explícita. A gravação usa arquivo temporário no mesmo diretório e só então substitui o destino, reduzindo o risco de corromper o arquivo existente em uma falha durante a escrita.

O primeiro caso de uso é `RegistrarPalavraDoDia`. Ele verifica data e palavra antes de persistir um registro pendente e devolve um resultado explícito para data já utilizada ou palavra já utilizada. Essas situações são resultados esperados de negócio; falhas de arquivo continuam sendo exceções técnicas. `Verbose.Application.Testes` usa um repositório em memória exclusivamente nos testes para validar o caso de uso sem depender de JSON.

O Worker compõe as dependências: lê `Persistencia:DiretorioDados` de `appsettings.json` por meio de `OpcoesDePersistencia`, valida que o valor está preenchido, resolve caminhos relativos a partir da pasta de conteúdo do Worker e registra `RepositorioDePalavrasEmJson` como a implementação de `IRepositorioDePalavrasDoDia`. O valor padrão `../data` aponta para `data/` na raiz do repositório. Nenhum caso de uso é executado automaticamente na inicialização.

Para validação local, `ServicoDeExecucaoManual` pode ser ativado apenas em `Development` com a seção `ExecucaoManual` de `appsettings.Development.json`, arquivo ignorado pelo Git. O arquivo versionado `appsettings.Manual.example.json` demonstra o formato. Quando ativo, o serviço executa `RegistrarPalavraDoDia` uma única vez, registra o resultado no log e encerra o host. Essa ferramenta não substitui o fluxo futuro de RabbitMQ e Ollama.

A execução manual foi validada localmente com variáveis de ambiente temporárias: a primeira execução criou `data/used-words.json` com um registro `PendenteEnvio`; uma segunda execução com a mesma data retornou `DataJaUtilizada` sem acrescentar outro registro. A suíte atual possui seis testes automatizados, executados com `dotnet test Verbose.sln`.

`Verbose.Infrastructure.Testes` valida a persistência em diretórios temporários, sem criar ou alterar o arquivo de dados real. Os cenários atuais cobrem arquivo ausente, gravação e recuperação de um registro enviado, e JSON inválido. Os testes são executados com `dotnet test Verbose.sln`.

O projeto compila com `dotnet build Verbose.sln`, os testes passam com `dotnet test Verbose.sln -m:1` e o Worker inicia com `dotnet run --project Verbose.Worker`. Persistência JSON, RabbitMQ e Ollama foram validados de ponta a ponta até o registro JSON. A primeira resposta do Ollama foi estruturalmente válida, mas retornou `sobrecarregar`, considerada comum, com exemplo usando `sobrecarregado`; após a calibração, `idôneo` foi aceito como resultado editorial adequado.

O envio de e-mail está implementado e validado com Gmail SMTP. `NotificacaoService` coordena a notificação: busca o registro, não reenvia registros já `Enviada`, solicita ao `IEmailService` o envio e atualiza o JSON somente após o SMTP aceitar a mensagem. `EmailServiceSmtp` usa MailKit com STARTTLS. O template visual `palavra_do_dia_template.html` está incorporado na assembly de infraestrutura; o C# substitui `{{PALAVRA}}`, `{{DEFINICAO}}`, `{{EXEMPLO}}` e `{{data}}` com HTML escapado. O e-mail também possui corpo alternativo em texto puro.

O envio real por Gmail SMTP foi validado: um registro pendente para `2026-10-15` foi enviado e atualizado para `Enviada`. O agendador diário usa `Agendamento:Horario` e `Agendamento:FusoHorario`, com padrão `07:00` em `America/Sao_Paulo`. Quando o Worker inicia após o horário, ele publica imediatamente a solicitação da data atual; esse comportamento foi validado com uma execução temporária usando `Agendamento__Horario=00:00`, que registrou e enviou a palavra de `2026-10-11`. Também foi validada a espera até um horário futuro no mesmo dia: o agendador publicou a solicitação no horário configurado e calculou corretamente o próximo ciclo para o dia seguinte. Um registro pendente já existente também foi reenviado sem nova chamada ao Ollama.

O ambiente local necessário para a V1 foi preparado e validado: .NET, RabbitMQ, Erlang, driver NVIDIA e Ollama estão operacionais. O modelo local `qwen3:8b` foi testado com execução integral na GPU. Detalhes de sistema operacional, hardware, contas e instalações locais não são versionados neste repositório público.

## Decisões confirmadas

- Linguagem e plataforma: C# com .NET Worker Service.
- IA: Ollama local, usando inicialmente `qwen3:8b`.
- A aplicação chamará a API local do Ollama com saída estruturada por JSON Schema e `think: false`; a saída continuará sendo validada pela aplicação.
- O Ollama é acessado pela porta `IGeradorDePalavraDoDia` e pela implementação HTTP `GeradorDePalavraDoDiaNoOllama`; `appsettings.json` contém URL local, modelo e tempo limite sem segredos.
- Mensageria: RabbitMQ local.
- A topologia RabbitMQ usa a exchange direta durável `verbose.palavras`, a fila durável `verbose.gerar-palavra-do-dia` e a routing key `gerar-palavra-do-dia`. Mensagens inválidas são rejeitadas com NACK sem requeue e encaminhadas, por uma dead-letter exchange, à fila durável `verbose.gerar-palavra-do-dia.falhas`. Usuário e senha ficam exclusivamente em .NET User Secrets; `appsettings.json` contém somente parâmetros não sensíveis.
- Falhas temporárias no consumo são reenviadas à fila durável `verbose.gerar-palavra-do-dia.tentativas`, aguardam 10 segundos e voltam à fila principal. Após duas novas tentativas, a mensagem segue para a fila de falhas. ACK só ocorre após registro e notificação concluídos; uma resposta repetida da IA é considerada falha temporária para evitar perder a solicitação. Credenciais SMTP recusadas são classificadas como falha permanente e seguem diretamente para a DLQ.
- Persistência: arquivo `data/used-words.json`, gerenciado exclusivamente pela aplicação com `System.Text.Json`.
- Cada registro persistido deverá manter palavra, definição, exemplo, data de referência e status de envio.
- Estados previstos para o envio: `PendenteEnvio` e `Enviada`.
- E-mail: HTML gerado em C#; IA produz somente palavra, definição e exemplo.
- E-mail SMTP: Gmail SMTP (`smtp.gmail.com`, porta 587 e STARTTLS). O pacote MailKit é a única nova dependência para essa fronteira tecnológica. Remetente, destinatário e senha de aplicativo ficam em .NET User Secrets; servidor e porta, que não são segredos, ficam em `appsettings.json`.
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

O critério “relativamente incomum” não será mensurado automaticamente com precisão na V1, pois isso exigiria uma base lexical ou de frequência adicional. O prompt será calibrado e os resultados iniciais serão avaliados manualmente. JSON válido não é, por si só, garantia de qualidade editorial. Após a calibração, `idôneo` foi aceito como resultado adequado; `sobrecarregar` permanece como exemplo de saída comum que o prompt deve evitar.

## Evolução planejada após o marco local

Uma versão posterior, inicialmente chamada de `0.2`, deverá permitir que pessoas se cadastrem para receber a palavra diária por e-mail. A intenção é criar uma landing page simples, registrar os endereços de e-mail e enviar a palavra do dia para a lista de destinatários cadastrados.

MongoDB é a opção inicial para essa evolução, por ser uma tecnologia conhecida pelo usuário e adequada para persistir cadastros e seus estados. A decisão ainda não está implementada nem detalhada. A landing page, o MongoDB, fluxos de consentimento/descadastro, proteção de dados pessoais e deploy público ficam explicitamente fora do escopo atual e serão discutidos antes de qualquer implementação.

## Próxima etapa

Revisar a entrega completa de geração, mensageria, e-mail e agendamento antes de preparar o pull request para `development`.
