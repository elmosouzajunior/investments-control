# Deploy Azure

Base recomendado, semelhante ao Omega Fleet Management:

- `Azure SQL Database` para o banco de dados.
- `Azure App Service` para a API ASP.NET Core.
- `Azure Static Web Apps` para o frontend Angular.

## Variáveis da API

Configure no App Service:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer=OmegaInvestAPI`
- `Jwt__Audience=OmegaInvestAngular`
- `Jwt__DurationInMinutes=480`
- `Cors__AllowedOrigins__0=https://<sua-static-web-app>.azurestaticapps.net`
- `BootstrapAdmin__Email`
- `BootstrapAdmin__Password`
- `BootstrapAdmin__Name`

## Observações

- Use uma chave JWT forte em produção.
- Troque a senha do usuário Master após o primeiro login.
- A versão local foi criada em `.NET 9` porque o SDK `.NET 10` não está instalado nesta máquina.
