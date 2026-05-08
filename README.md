# Investments Control

Aplicação full-stack para controle de investimentos por empresa, com gestão master, cadastros operacionais, lançamentos financeiros, dashboard executivo e análises gráficas.

O projeto está organizado como um monorepo contendo frontend Angular e backend ASP.NET Core.

## Visão Geral

O sistema permite que um usuário master cadastre empresas e usuários administradores. Cada empresa acessa sua própria área para configurar planos de investimento, tipos/categorias, instituições financeiras, investimentos e lançamentos.

Além dos cadastros, a aplicação apresenta indicadores de saldo real, saldo previsto, rendimento real, rendimento previsto, composição da carteira por categoria e gráficos mensais de evolução.

## Stack

### Frontend

- Angular 21
- SCSS
- Bootstrap Icons
- ApexCharts com `ng-apexcharts`
- Estrutura inspirada no padrão visual do projeto Fleet Management

### Backend

- ASP.NET Core / .NET 9
- Entity Framework Core
- SQL Server LocalDB em desenvolvimento
- ASP.NET Identity
- JWT
- Swagger

### Banco Local

- SQL Server LocalDB
- Banco padrão: `OmegaInvestDb`

## Estrutura Do Repositório

```text
.
├── backend
│   ├── Omega.Invest.sln
│   └── src
│       ├── Omega.Invest.API
│       ├── Omega.Invest.Application
│       ├── Omega.Invest.Domain
│       └── Omega.Invest.Infrastructure
├── frontend
│   ├── package.json
│   └── src
│       └── app
│           ├── components
│           ├── pages
│           ├── shared
│           ├── app.html
│           ├── app.scss
│           └── app.ts
└── README.md
```

## Funcionalidades Implementadas

### Área Master

- Login com layout baseado no Fleet Management.
- Sidebar com layout e ícones modernizados.
- Cadastro de empresas.
- Cadastro de usuários.
- Listas otimizadas, com menos espaços vazios.
- Cadastro e edição em modais separados.
- Botões e tabelas com estilo mais delicado e consistente.

### Área Da Empresa

- Dashboard executivo.
- Cadastro de planos de investimento.
- Cadastro de categorias/tipos de investimento.
- Cadastro de instituições financeiras.
- Cadastro de investimentos.
- Lançamentos de aportes.
- Lançamentos de retiradas.
- Lançamentos de rendimentos/saldos mensais.
- Listas filtradas por plano.
- Ordenação dos lançamentos do mais novo para o mais antigo.
- Datas formatadas em `dd/mm/aaaa`.

### Dashboard

- Filtro de plano.
- Filtro de data de referência, iniciado com a data do dia.
- Filtro de prazo da projeção, padrão de 30 anos.
- KPIs:
  - Saldo Real
  - Saldo Previsto
  - Rendimento Real
  - Rendimento Previsto
- Comparativo Real x Previsto com barras horizontais premium.
- Gráfico de rosca com composição da carteira por categoria.
- Projeção em faixa separada:
  - Real projetado
  - Previsto projetado
  - Diferença projetada
- A projeção usa como início a primeira data registrada no plano, considerando data inicial do investimento, aporte, retirada ou medição.
- A data de referência afeta o saldo atual, indicadores e composição da carteira.

### Tela De Gráficos

- Nova aba `Gráficos`.
- Filtro por plano.
- Filtro por investimento, permitindo visualizar:
  - Todos os investimentos do plano.
  - Um investimento isolado.
- Gráfico de barras verticais dos últimos 12 meses:
  - Valor Previsto
  - Valor Realizado
- Gráfico mensal de percentual de rendimento:
  - Previsto
  - Realizado
- Comparação ano a ano:
  - Melhor mês
  - Pior mês
  - Total de meses
  - Retorno mensal por ano
  - Retorno anual
  - Retorno acumulado

## Regras De Cálculo Relevantes

### Rendimento Mensal Realizado

O percentual mensal não é acumulado. Ele compara apenas o mês selecionado:

```text
% mensal = (saldo final do mês / (saldo do mês anterior + aportes do mês - retiradas do mês) - 1) * 100
```

No primeiro mês, o valor inicial do investimento entra como base.

### Rendimento Mensal Previsto

O previsto mensal compara o saldo previsto do mês contra a base prevista do mês anterior, considerando os fluxos de caixa já existentes até a data de referência.

### Projeção

O prazo da projeção começa na primeira data registrada do plano, não na data filtrada do dashboard.

```text
data final da projeção = primeira data registrada + prazo da projeção
```

A data filtrada continua sendo usada para apurar o saldo atual e projetar o saldo real restante até o fim do prazo.

## Usuário Inicial

O backend cria um usuário master inicial conforme `appsettings.json`.

```text
E-mail: master@omegainvest.com
Senha: Master@123
```

## Como Rodar Localmente

### 1. Subir o banco LocalDB

```powershell
sqllocaldb start MSSQLLocalDB
```

### 2. Rodar o backend

Na raiz do repositório:

```powershell
dotnet run --project backend\src\Omega.Invest.API\Omega.Invest.API.csproj --urls http://localhost:5241
```

URLs do backend:

```text
API: http://localhost:5241
Swagger: http://localhost:5241/swagger
```

### 3. Rodar o frontend

```powershell
cd frontend
npm install
npx ng serve --host 0.0.0.0 --port 4200
```

URL do frontend:

```text
http://localhost:4200
```

## Comandos Úteis

### Build Do Frontend

```powershell
cd frontend
npm run build
```

### Build Do Backend

Na raiz do repositório:

```powershell
dotnet build backend\Omega.Invest.sln
```

### Status Do Git

```powershell
git status
```

### Push Para O GitHub

```powershell
git push origin main
```

## Configurações Importantes

### API usada pelo frontend

O frontend carrega a URL da API em tempo de execução por:

```text
frontend/public/app-config.json
```

Em desenvolvimento, o valor padrão aponta para:

```json
{
  "apiUrl": "http://localhost:5241/api"
}
```

No deploy PRD, o GitHub Actions substitui esse arquivo no build para apontar para a API publicada no Azure.

### Connection string local

Configurada em:

```text
backend/src/Omega.Invest.API/appsettings.json
```

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=OmegaInvestDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

### CORS local

```json
"AllowedOrigins": [ "http://localhost:4200" ]
```

## Validações Recentes

Últimas validações feitas durante o desenvolvimento:

```powershell
npm run build
dotnet build backend\Omega.Invest.sln
```

Ambas passaram com sucesso antes do último push.

## Observações Para Produção

Antes de publicar em PRD, revisar:

- Connection string de produção.
- Chave JWT de produção.
- Origem permitida no CORS.
- Estratégia de migração/criação do banco.
- Variáveis de ambiente ou secrets para configurações sensíveis.
- URL da API consumida pelo frontend.
- Build de produção do Angular.

## Repositório

```text
https://github.com/elmosouzajunior/investments-control.git
```
