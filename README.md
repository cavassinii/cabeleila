# Cabeleila

Sistema de agendamento online para o salao de beleza da Leila: clientes agendam servicos pela
internet, e o salao ganha um painel operacional e gerencial para acompanhar tudo.

## Tecnologias utilizadas

**Back-end (`api/`)**
- C# / .NET 8 (ASP.NET Core Web API, padrao MVC via Controllers)
- PostgreSQL, acessado com SQL puro via Npgsql (sem ORM)
- Autenticacao JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`), com a configuracao do
  token (`JwtSettings`) como classe compilada em vez de `appsettings.json`
- Hash de senha com PBKDF2 nativo do `System.Security.Cryptography` (sem biblioteca externa)
- Swagger / Swashbuckle para documentacao e teste manual dos endpoints

**Front-end (`web/`)**
- HTML5 + CSS3 + JavaScript puro (ES Modules), sem framework e sem build step
- Estrutura MVC manual (`models` / `views` / `controllers` / `services`)
- Servidor estatico minimo em Node.js (`web/server.js`, so com modulos nativos, sem dependencias)

**Banco de dados**
- PostgreSQL, schema completo em um unico arquivo: `database/schema.sql`

## Estrutura do repositorio

```
Cabeleila/
├── database/
│   ├── schema.sql              # script unico de criacao de todas as tabelas
│   └── seed.sql                 # dados iniciais (usuario da Leila + cliente de teste)
├── api/                         # API .NET - ver api/README.md para detalhes (projetos, endpoints)
│   ├── src/
│   │   ├── Cabeleila.Api        # Web API (Controllers, Program.cs, JwtSettings)
│   │   ├── Cabeleila.Dto        # entidades + requests/responses
│   │   ├── Cabeleila.Dal        # acesso a dados (SQL puro)
│   │   └── Cabeleila.Security   # hash de senha
│   └── tools/
│       ├── Cabeleila.SeedStaff       # cria/atualiza o usuario staff (Leila)
│       └── Cabeleila.MigrationRunner # roda um arquivo .sql qualquer no Postgres via .NET
├── web/                         # front-end estatico (HTML/CSS/JS)
└── docs/
    ├── capturas-de-tela/         # prints das telas do sistema
    └── video/                    # video do sistema em funcionamento
```

## Como rodar o projeto

Pre-requisitos: **PostgreSQL**, **.NET 8 SDK** e **Node.js** instalados.

### 1. Banco de dados

Crie um banco chamado `cabeleila`:

```
createdb -U postgres cabeleila
```

Depois rode o script de schema. Se tiver o `psql` instalado:

```
psql -U postgres -d cabeleila -f database/schema.sql
```

Se preferir nao instalar o `psql`, use o `Cabeleila.MigrationRunner` (so precisa do .NET SDK):

```
dotnet run --project api/tools/Cabeleila.MigrationRunner -- "Server=localhost;Port=5432;User Id=postgres;Password=SUASENHA;Database=cabeleila;" database/schema.sql
```

### 2. Configurar a API

Ajuste a connection string em `api/src/Cabeleila.Api/appsettings.json` (chave
`ConnectionStrings:Cabeleila`) para o usuario/senha do seu Postgres local.

### 3. Criar o usuario da Leila

Nao existe cadastro publico para a equipe (por seguranca). Crie o primeiro usuario com:

```
dotnet run --project api/tools/Cabeleila.SeedStaff -- "Server=localhost;Port=5432;User Id=postgres;Password=SUASENHA;Database=cabeleila;" "Leila" leila@cabeleila.com.br "SuaSenha123" OWNER
```

Pode rodar de novo a qualquer momento (e um upsert por e-mail) para resetar a senha.

### 4. Rodar a API

```
dotnet run --project api/src/Cabeleila.Api
```

Fica disponivel em `http://localhost:5199` (Swagger em `http://localhost:5199/swagger`).

### 5. Rodar o front-end

Em outro terminal:

```
node web/server.js
```

Fica disponivel em `http://localhost:8080`. O CORS da API ja libera essa origem (e
`http://localhost:5500`, caso prefira usar a extensao Live Server do VS Code para servir a
pasta `web/` em vez do `server.js`).

### 6. Acessar

- Cliente: `http://localhost:8080/login.html`
- Equipe (Leila): `http://localhost:8080/staff/login.html`

## Credenciais de teste

| Perfil | E-mail | Senha |
|---|---|---|
| Cliente | `cliente.teste@example.com` | `123456` |
| Equipe (Leila, OWNER) | `leila@cabeleila.com.br` | `Leila@123456` |

## Funcionalidades implementadas

**Fundamental (pedido pela Leila para o cliente)**
- Cadastro/login do cliente
- Agendamento de um ou mais servicos, com grade de horarios disponiveis (30 em 30 min)
- Alteracao do agendamento (data/hora e servicos) pelo proprio cliente, permitida ate 2 dias
  antes do agendado - depois disso, so por telefone com o salao
- Historico de agendamentos por periodo, com detalhe de cada um
- Sugestao automatica de mesma data quando ja existe agendamento do cliente na mesma semana

**Plus (diferencial operacional/gerencial da Leila)**
- Painel da equipe com login separado
- Listagem dos agendamentos recebidos (filtro por data/status)
- Confirmacao do agendamento ao cliente
- Alteracao de data/hora e servicos por telefone, sem a restricao dos 2 dias
- Gerenciamento do status de cada servico do agendamento (fecha o agendamento sozinho quando
  todos os servicos sao concluidos)
- Desempenho semanal (agendamentos, concluidos, cancelados, faturamento)

**Controle de agenda (evolucao pedida depois, alem do escopo original do PDF)**
- Horario de funcionamento configuravel por dia da semana pela propria Leila
- Duracao dos servicos sempre em multiplos de 30 minutos, para nao deixar buracos na agenda
- Bloqueio de conflito de horario pela duracao real do atendimento (nao so pelo horario de
  inicio), tanto na criacao quanto na alteracao de um agendamento - vale tanto para o cliente
  quanto para a Leila, ja que so ela atende no salao (piloto mono-profissional)

## Documentacao adicional

- [api/README.md](api/README.md) - projetos da API, autenticacao e referencia de endpoints
- [docs/capturas-de-tela](docs/capturas-de-tela) - prints das telas do sistema
- [docs/video](docs/video) - vídeo do sistema em funcionamento

## Observacoes e decisoes de projeto

- **Regra dos 2 dias**: verificada sempre contra a data/hora *original* do agendamento (nao a
  nova data pedida), para nao dar brecha de burlar a regra reagendando em cascata.
- **Cancelamento**: o PDF fala apenas em "alteracao"; adicionei cancelamento por ser o
  complemento natural de um sistema de agendamento, nao por ser um requisito literal.
- **Mono-tenant / mono-profissional**: o schema assume um unico salao e uma unica profissional
  (a Leila) atendendo, decisao confirmada com o cliente para este piloto. Adaptar para varios
  profissionais exigiria uma tabela de profissionais e agenda por profissional.
- **Seguranca do JWT**: `JwtSettings` fica como classe compilada (conforme pedido), o que
  significa que a chave de assinatura e extraivel via decompilacao do binario. Para producao
  com dados sensiveis, o ideal seria mover para variavel de ambiente ou um secret manager.
- **Testes automatizados**: nao foram implementados testes unitarios/integracao nesta entrega.
  A aplicacao foi validada manualmente (API via curl/Swagger e front-end via browser), incluindo
  os fluxos de conflito de agenda, regra dos 2 dias e edicao de servicos.
- **Sessoes separadas**: cliente e equipe usam chaves de sessao independentes no navegador, para
  logar como um perfil nao derrubar a sessao do outro perfil aberta em outra aba.
