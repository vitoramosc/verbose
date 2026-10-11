# Diretrizes do repositório

## Objetivo

Verbose é um serviço local diário de vocabulário em português brasileiro. Ele seleciona uma palavra relativamente incomum, útil e atual, gera definição e exemplo com IA local e envia o resultado por e-mail.

## Tecnologias e restrições

- C# e .NET Worker Service.
- RabbitMQ e Ollama executados localmente.
- Modelo atual para avaliação: `qwen3:8b` no Ollama.
- Persistência local em JSON com `System.Text.Json`; o arquivo previsto é `data/used-words.json`.
- E-mail HTML montado em C# e enviado por SMTP.
- Arquitetura de monólito organizado com conceitos de Arquitetura Hexagonal.
- Não usar Docker, banco de dados SQL, Redis, microserviços, nuvem para executar a aplicação ou APIs de IA externas/pagas na V1.
- Não adicionar bibliotecas, serviços ou tecnologias sem uma necessidade concreta e explicada.

## Convenções de desenvolvimento

- Escrever código, nomes de domínio, logs e documentação em português, salvo nomes técnicos inevitáveis de APIs ou bibliotecas externas.
- Aplicar Clean Code e SOLID com pragmatismo. Não criar interfaces, padrões ou camadas vazias apenas por convenção.
- Isolar tecnologias externas na infraestrutura; regras de negócio não devem depender de RabbitMQ, Ollama, SMTP ou arquivos.
- Criar abstrações apenas em fronteiras tecnológicas ou quando houver benefício concreto de manutenção e substituição.
- Manter dependências sem ciclos.

## Processo de trabalho

- Trabalhar em etapas pequenas e verticais, introduzindo um conceito principal por etapa.
- Antes de implementar, explicar objetivo, decisão técnica, arquivos envolvidos, como executar e como validar.
- Preferir algo observável e depurável em cada etapa: logs, breakpoints, comandos locais, inspeção de arquivos ou testes proporcionais.
- Não avançar para a próxima etapa sem confirmação explícita do usuário e análise da validação da etapa atual.
- Explicar conceitos novos gradualmente, no momento em que forem necessários.
- Não criar a solução, projetos, classes ou funcionalidades fora do escopo aprovado da etapa atual.

## Segurança e configuração

- Nunca registrar senhas, tokens, chaves privadas ou credenciais no Git, documentação ou logs.
- Manter credenciais SMTP e configurações sensíveis fora do código-fonte.
- Validar toda saída da IA antes de persistir, enviar por e-mail ou inserir em HTML.
- Escapar valores dinâmicos no HTML do e-mail.
- Tratar explicitamente erros de arquivos, Ollama, RabbitMQ e SMTP, sem registrar segredos.
- Não instalar ferramentas, alterar configurações relevantes do sistema ou executar operações destrutivas sem autorização explícita e explicação prévia.

## Git e documentação

- Criar branches somente com os prefixos `feature/` (funcionalidade) ou `fix/` (correção), em minúsculas e usando hífens na descrição.
- Usar `feat:` para commits de funcionalidade e `fix:` para commits de correção.
- Usar `development` como integração da V1: branches de funcionalidade ou correção partem dela e retornam a ela por pull request quando uma entrega coerente estiver concluída e validada.
- Manter `main` funcional e reservada à integração da V1 completa e validada.
- Não criar commits automaticamente. Ao concluir uma etapa, sugerir uma mensagem de commit e aguardar decisão do usuário.
- Verificar o estado do Git antes de sugerir comandos potencialmente destrutivos.
- Não usar `git add .`; adicionar arquivos explicitamente e revisar o conteúdo preparado antes de cada commit.
- Ler `AGENTS.md` e `PROJECT_CONTEXT.md` antes de começar uma nova etapa, quando o contexto local existir.
- Manter `PROJECT_CONTEXT.md` somente como documentação local, fora do Git. Atualizá-lo quando o estado real, decisões relevantes ou a próxima etapa mudarem.
- Atualizar este arquivo somente quando as regras permanentes de trabalho mudarem.
