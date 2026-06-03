# PRD Local

Este roteiro cria uma versao local de producao do Omega Invest e traz os dados do Azure SQL de PRD para um banco SQL Server no PC.

## 1. Banco local

Recomendado nesta maquina:

- SQL Server Express: `.\SQLEXPRESS`
- SQL Server Developer tambem e uma boa opcao.
- SQL Server LocalDB pode funcionar para uso individual, mas nesta maquina a instancia padrao nao esta saudavel.

Banco padrao usado nos scripts:

```text
OmegaInvestPrdLocal
```

## 2. Exportar dados de PRD

Opcao A: Azure Portal

1. Acesse o Azure SQL Database de PRD.
2. Use a acao **Export**.
3. Gere um arquivo `.bacpac` em uma Storage Account.
4. Baixe o arquivo para a maquina local, por exemplo:

```text
artifacts\omega-invest-prd.bacpac
```

Opcao B: SqlPackage local

```powershell
.\scripts\export-prd-azure-sql-bacpac.ps1 `
  -SourceConnectionString "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" `
  -OutputPath .\artifacts\omega-invest-prd.bacpac
```

Opcao C: SqlPackage local com senha digitada no prompt

```powershell
.\scripts\export-azure-sql-direct-bacpac.ps1 `
  -ServerName "<server>.database.windows.net" `
  -DatabaseName "<database>" `
  -UserName "<sql-user>" `
  -OutputPath .\artifacts\omega-invest-prd.bacpac
```

## 3. Importar no PC

Com SQL Server Express:

```powershell
.\scripts\import-prd-bacpac-local.ps1 `
  -BacpacPath .\artifacts\omega-invest-prd.bacpac `
  -TargetServerName ".\SQLEXPRESS" `
  -TargetDatabaseName OmegaInvestPrdLocal
```

Com SQL Server Developer/local default:

```powershell
.\scripts\import-prd-bacpac-local.ps1 `
  -BacpacPath .\artifacts\omega-invest-prd.bacpac `
  -TargetServerName "localhost" `
  -TargetDatabaseName OmegaInvestPrdLocal
```

## 4. Rodar API em ProductionLocal

```powershell
.\scripts\start-prd-local-api.ps1
```

A chave JWT local e definida por variavel de ambiente em tempo de execucao. Para manter tokens validos entre reinicios, defina antes de iniciar:

```powershell
$env:OMEGA_INVEST_PRD_LOCAL_JWT_KEY = "<uma-chave-local-forte>"
```

Se a variavel nao existir, os scripts geram uma chave temporaria para aquela execucao.

A API sobe em:

```text
http://localhost:5241
```

O ambiente `ProductionLocal` usa:

```text
backend\src\Omega.Invest.API\appsettings.ProductionLocal.json
```

## 5. Rodar frontend

```powershell
cd frontend
npm run start -- --host 0.0.0.0 --port 4200
```

Frontend:

```text
http://localhost:4200
```

## Rodar tudo com um comando

Para subir API, frontend e abrir o navegador:

```powershell
.\scripts\start-prd-local-app.ps1
```

Ou use o atalho/script:

```text
Omega Invest PRD Local.cmd
```

Para parar os processos iniciados pelo script:

```powershell
.\scripts\stop-prd-local-app.ps1
```

Ou use:

```text
Omega Invest Parar.cmd
```

Logs e PIDs ficam em:

```text
artifacts\prd-local
```

## 6. Publicar artefatos locais

Para gerar build local de producao:

```powershell
.\scripts\publish-prd-local.ps1
```

Saida da API:

```text
artifacts\api-prd-local
```

## 7. Checklist antes de desligar/reduzir Azure

- Fazer uma exportacao final do Azure SQL.
- Evitar novos lancamentos na nuvem durante a janela de migracao.
- Importar o `.bacpac` no banco local.
- Validar login master e usuarios de empresa.
- Conferir empresas, planos, investimentos, aportes, retiradas e medicoes mensais.
- Conferir dashboard e graficos contra a PRD antiga.
- Guardar o `.bacpac` final como backup offline.
