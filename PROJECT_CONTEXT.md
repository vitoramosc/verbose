# Contexto do Projeto Verbose

## Visão geral

Verbose é um serviço diário local de aprendizado de vocabulário em português brasileiro. A aplicação deverá selecionar uma palavra relativamente incomum, útil e contemporânea, produzir definição e exemplo, e enviar esse conteúdo por e-mail.

O escopo da primeira versão é local: agendamento diário, RabbitMQ, Ollama, persistência em JSON, geração de HTML em C# e envio via SMTP.

## Estado atual

Não existe solução .NET, projeto, código de aplicação ou configuração de e-mail no repositório. O repositório continha apenas `README.md` antes da criação desta documentação.

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
- Fluxo Git: branches usam `feature/` ou `fix/`; commits usam `feat:` ou `fix:`; `main` recebe somente etapas concluídas e validadas.

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

Criar a solução .NET mínima e um único Worker que inicia e registra um log no terminal. Esta etapa não incluirá RabbitMQ, JSON, Ollama ou e-mail.
