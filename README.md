# Verbose

Verbose é um serviço local diário de vocabulário em português brasileiro. Ele gera uma palavra relativamente incomum, útil e atual com IA local, envia o conteúdo por e-mail e oferece inscrição com confirmação de e-mail.

## Recursos atuais

- Worker .NET 10 com agendamento diário.
- RabbitMQ local, ACK após processamento, fila de tentativas e DLQ.
- Ollama local com revisão editorial da resposta.
- Persistência local em JSON, com escrita atômica.
- E-mail HTML por SMTP.
- Landing page local em `http://localhost:5078/`.
- Cadastro com double opt-in e cancelamento por link individual.
- Rate limit por IP para cadastro, confirmação e cancelamento.

## Pré-requisitos locais

- .NET SDK 10.
- RabbitMQ em execução.
- Ollama em execução com o modelo configurado.
- Credenciais SMTP configuradas somente nos User Secrets.

## Configuração local

O repositório não contém senhas, tokens nem dados de uso. Configure os valores locais necessários com User Secrets:

```bash
dotnet user-secrets set --project Verbose.Worker "RabbitMq:Usuario" "seu-usuario"
dotnet user-secrets set --project Verbose.Worker "RabbitMq:Senha" "sua-senha"
dotnet user-secrets set --project Verbose.Worker "Email:Remetente" "seu-email"
dotnet user-secrets set --project Verbose.Worker "Email:SenhaDeAplicativo" "sua-senha-de-aplicativo"
```

Os arquivos de dados locais ficam em `data/` e são ignorados pelo Git.

## Executar e validar

```bash
dotnet build Verbose.sln
dotnet test Verbose.sln -m:1
Agendamento__Horario=23:59 dotnet run --project Verbose.Worker
```

Com o Worker em execução, abra `http://localhost:5078/` para acessar a landing page.

## Estrutura

- `Verbose.Domain`: regras e conceitos de domínio.
- `Verbose.Application`: casos de uso e portas para tecnologias externas.
- `Verbose.Infrastructure`: JSON, SMTP, Ollama e RabbitMQ.
- `Verbose.Worker`: composição da aplicação, agendamento, endpoints e landing page.

Leia [AGENTS.md](AGENTS.md) para as diretrizes de desenvolvimento do repositório.
