# API pro psí spolky

Toto API poskytuje endpointy pro správu psích spolků, výstav a souvisejících funkcionalit.
#### Příklady requestů v souboru API_psi_spolky.http !
## Funkce

- Správa spolků (vytváření, úpravy, export)
- Registrace a správa psů
- Organizace výstav
- Auditní logy pro změny stanov
- Autorizace založená na rolích (Admin, Chairman, ReadOnly)

## Technologie

- .NET 9.0
- ASP.NET Core
- Entity Framework Core
- C# 13.0

## Autentizace

API používá autorizaci založenou na rolích s následujícími rolemi:

- Admin
- Chairman (Předseda)
- ReadOnly (Pouze pro čtení)
- Public (Veřejný přístup)

## Pro detailní dokumentaci API prosím využijte Swagger/OpenAPI dokumentaci na (adrese serveru)/swagger

## Lokální spuštění
- Naklonovat repozitář.
### Předpoklady
- .NET SDK 9.0
- MS SQL Server podle konfigurace connection stringu, je nutné vytvořit databázi s názvem "evidence", zbytek se udělá sám.
- Editor: Visual Studio 2022+ nebo JetBrains Rider 2024.2+

### Konfigurace
1. Zkopírujte soubor uživatelské konfigurace, pokud je potřeba:
    - appsettings.json → appsettings.Development.json (a upravte connection string, klíče apod.).
    - Podívat se do properties/launchSettings.json

### Spuštění ve Visual Studio
1. Otevřete řešení (.sln).
2. V Solution Explorer zvolte spouštěcí projekt (Set as Startup Project).
3. Zvolte profil spouštění:
    - IIS Express nebo Project (Kestrel).
4. Vyberte konfiguraci Debug a cílovou platformu Any CPU.
5. Stiskněte zelenou šipku spustit.
6. Aplikace poběží na URL z profilu (https://localhost:xxxx). Swagger: /swagger.

### Spuštění v JetBrains Rider
1. Otevřete složku řešení v Rideru.
2. Vpravo nahoře vyberte Run Configuration pro spouštění projektu (http nebo https).
3. Klikněte na zelenou šipku Run (Shift+F10) nebo Debug (Shift+F9).
4. Aplikace poběží na lokální adrese z konfigurace; Swagger: /swagger.

### Spuštění z příkazové řádky
1. Přejděte do složky spouštěcího projektu.
2. Pro vývoj:
    - dotnet restore
    - dotnet build
    - dotnet run
3. Otevřete prohlížeč na zobrazené adrese (např. https://localhost:xxxx) a navštivte /swagger pro endpointy.

### Časté problémy
- Port je obsazen: změňte port v launchSettings.json nebo Run Configuration.
- Certifikát HTTPS: nainstalujte dev certifikát: dotnet dev-certs https --trust.

