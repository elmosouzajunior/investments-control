# Omega Invest

Aplicativo multi-tenant para empresas controlarem planos de investimento, instituições financeiras, investimentos, operações de aporte/retirada e medições mensais.

## Stack

- Backend: ASP.NET Core / .NET 9 neste ambiente local. O projeto está pronto para migrar para .NET 10 quando o SDK 10 estiver instalado.
- Frontend: Angular 21.
- Banco: SQL Server / Azure SQL.
- Autenticação: ASP.NET Identity + JWT.

## Usuário inicial

- E-mail: `master@omegainvest.com`
- Senha: `Master@123`

## Rodar localmente

Backend:

```powershell
sqllocaldb start MSSQLLocalDB
dotnet run --project backend\src\Omega.Invest.API\Omega.Invest.API.csproj --urls http://localhost:5241
```

Frontend:

```powershell
cd frontend
npm install
npx ng serve --host 0.0.0.0 --port 4200
```

URLs:

- Frontend: `http://localhost:4200`
- API: `http://localhost:5241`
- Swagger: `http://localhost:5241/swagger`
