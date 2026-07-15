# Cabeleila API

API em .NET 8 (ASP.NET Core Web API, Controllers) + PostgreSQL (SQL puro via Npgsql), JWT.

## Projetos

- `src/Cabeleila.Api` - Web API (Controllers, JwtSettings, Program.cs)
- `src/Cabeleila.Dto` - DTOs (entidades + requests/responses)
- `src/Cabeleila.Dal` - acesso a dados (SQL puro, sem ORM)
- `src/Cabeleila.Security` - hashing de senha (PBKDF2 nativo do .NET)
- `tools/Cabeleila.SeedStaff` - CLI para criar/atualizar o usuario staff (Leila)
- `tools/Cabeleila.MigrationRunner` - CLI generico para rodar um arquivo `.sql` qualquer no Postgres (ex.: `database/schema.sql`)

## Configuracao

1. Ajuste a connection string em `src/Cabeleila.Api/appsettings.json` (`ConnectionStrings:Cabeleila`) para o seu Postgres local (banco `cabeleila`, ja criado com `database/schema.sql`).
2. Crie o primeiro usuario staff (Leila) rodando:

```
dotnet run --project tools/Cabeleila.SeedStaff -- "Server=localhost;Port=5432;User Id=postgres;Password=SUASENHA;Database=cabeleila;" "Leila" leila@cabeleila.com.br "SuaSenha123" OWNER
```

Pode rodar de novo a qualquer momento para resetar a senha (e um upsert por email).

## Rodar a API

```
dotnet run --project src/Cabeleila.Api
```

Swagger em `http://localhost:5199/swagger` (ou a porta exibida no console).

## Autenticacao

- `POST /api/auth/customer/register` e `/api/auth/customer/login` - cliente final.
- `POST /api/auth/staff/login` - Leila/atendentes (sem endpoint publico de cadastro; use o SeedStaff).
- Demais rotas exigem `Authorization: Bearer {token}`. Role `Customer` para `/api/appointments/*`, role `OWNER`/`ATTENDANT` para `/api/staff/*` e escrita em `/api/services`.

## Endpoints principais

| Rota | Quem | O que faz |
|---|---|---|
| `GET /api/services` | publico | catalogo de servicos (tela de agendamento) |
| `GET /api/appointments/availability?date=&durationMinutes=` | cliente | grade de horarios livres (30 em 30 min) dentro do expediente, sem conflito de agenda |
| `GET /api/appointments/suggest-date?date=` | cliente | avisa se ja existe agendamento na mesma semana |
| `POST /api/appointments` | cliente | cria agendamento (1+ servicos); usa `keepOriginalDate` para confirmar apesar da sugestao |
| `GET /api/appointments?from=&to=` | cliente | historico |
| `PUT /api/appointments/{id}/reschedule` / `/cancel` | cliente | so ate 2 dias antes do agendado; revalida horario comercial e conflito |
| `PUT /api/appointments/{id}/services` | cliente | troca os servicos do agendamento (mesma regra dos 2 dias) |
| `GET /api/staff/appointments?date=&status=` | staff | listagem operacional |
| `PUT /api/staff/appointments/{id}/confirm` | staff | confirma o agendamento ao cliente |
| `PUT /api/staff/appointments/{id}/reschedule` / `/cancel` / `/services` | staff | sem restricao de 2 dias (alteracao por telefone); mesma checagem de horario/conflito |
| `PUT /api/staff/appointments/items/{itemId}/status` | staff | status por servico (fecha o agendamento sozinho quando todos concluem) |
| `GET /api/staff/dashboard/weekly-performance?referenceDate=` | staff | agendamentos e faturamento da semana |
| `GET`/`PUT /api/staff/business-hours` | staff | define o horario de atendimento por dia da semana (base da grade de disponibilidade) |

## Observacoes de seguranca

- `JwtSettings` (chave de assinatura do token) fica como classe compilada em `src/Cabeleila.Api/JwtSettings.cs`, conforme padrao pedido. Por estar no binario ela e extraivel via decompilacao; para producao com dados sensiveis o ideal seria mover para variavel de ambiente / secret manager.
- A connection string do Postgres fica em `appsettings.json` (fora do codigo-fonte), para nao vazar credenciais no Git.
