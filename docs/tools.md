# Outils et commandes

## Objectif

Le répertoire `tools` regroupe les scripts et commandes utilisés pour préparer le poste de développement, maintenir la solution, exécuter les tests et générer des archives de revue.

Les scripts sont conçus pour être lancés depuis la racine de la solution.

```text
tools
├── clean_solution.bat
├── commands.txt
├── compress_solution.bat
├── configure_desktop.bat
├── restore_solution.bat
└── test_solution.bat
```

Les commandes utiles et durables sont documentées ci-dessous afin que la documentation soit la source de référence.

---

# Préparation du poste de développement

## `configure_desktop.bat`

Le script `tools/configure_desktop.bat` prépare un poste Windows pour développer et tester BudgetManager.

Il vérifie et installe si nécessaire :

- Git ;
- le SDK .NET 10 ;
- l'outil global `dotnet-ef` compatible .NET 10 ;
- LibMan (`Microsoft.Web.LibraryManager.Cli`) ;
- ReportGenerator (`dotnet-reportgenerator-globaltool`) ;
- Node.js LTS et npm ;
- WSL 2 ;
- Docker Desktop.

WinGet est utilisé pour les installations automatiques.

Visual Studio est détecté et signalé, mais n'est pas installé automatiquement.

Le script configure également le `PATH` utilisateur pour les outils détectés ou installés.

Le SDK .NET fournit directement les commandes usuelles de la solution, notamment `dotnet restore`, `dotnet build`, `dotnet test`, `dotnet run`, `dotnet clean`, `dotnet publish`, `dotnet watch` et `dotnet format`. Aucun outil global supplémentaire n'est nécessaire pour ces commandes.

Lorsqu'une élévation de privilèges est nécessaire, le script se relance en mode administrateur. La console élevée reste ouverte à la fin afin de permettre la lecture du résultat.

Le script est idempotent : il peut être relancé sans réinstaller les composants déjà présents.

Utilisation :

```bat
tools\configure_desktop.bat
```

Après installation ou modification du `PATH`, les consoles déjà ouvertes et Visual Studio doivent être redémarrés.

Si WSL vient d'être activé, un redémarrage de Windows peut être nécessaire avant de pouvoir utiliser Docker.

---

# Restauration des dépendances

## `restore_solution.bat`

Le script `tools/restore_solution.bat` restaure l'ensemble des dépendances nécessaires à la solution.

Il :

1. restaure les packages NuGet de `BudgetManager.sln` ;
2. restaure les bibliothèques clientes déclarées dans `src/BudgetManager.Web/libman.json`.

Utilisation :

```bat
tools\restore_solution.bat
```

Le script nécessite :

- le SDK .NET ;
- l'outil global LibMan (`Microsoft.Web.LibraryManager.Cli`).

Ces outils sont installés ou vérifiés par `tools/configure_desktop.bat`.

Les bibliothèques clientes sont restaurées dans :

```text
src\BudgetManager.Web\wwwroot\lib
```

Ce script peut notamment être utilisé après un clonage du dépôt ou après un nettoyage des dépendances locales.

---

# Nettoyage de la solution

## `clean_solution.bat`

Le script `tools/clean_solution.bat` supprime récursivement les répertoires générés ou temporaires suivants :

```text
bin
obj
packages
.vs
TestResults
tests_results
```

Utilisation :

```bat
tools\clean_solution.bat
```

Il est utile avant une archive, après un changement important de dépendances ou lorsqu'un artefact de compilation local provoque un comportement incohérent.

---

# Archivage de la solution

## `compress_solution.bat`

Le script `tools/compress_solution.bat` crée l'archive :

```text
tmp\BudgetManager.zip
```

L'archive contient les fichiers de la solution qui ne sont pas ignorés par les règles `.gitignore`.

Les répertoires ignorés tels que `.vs`, `bin`, `obj` ou `tmp` ne sont pas parcourus lors de la copie.

Le script fonctionne également avant l'initialisation du dépôt Git.

Utilisation sans exclusion :

```bat
tools\compress_solution.bat
```

Il est possible d'exclure un ou plusieurs projets en passant leur nom en argument :

```bat
tools\compress_solution.bat BudgetManager.Web
```

ou :

```bat
tools\compress_solution.bat BudgetManager.Application BudgetManager.Web
```

Pour chaque projet exclu :

- `src\<Projet>` n'est pas copié ;
- `tests\<Projet>.Tests` n'est pas copié s'il existe ;
- le projet et son éventuel projet de tests sont retirés de la copie de `BudgetManager.sln` contenue dans l'archive.

Le fichier `BudgetManager.sln` original n'est jamais modifié.

Ce script est notamment utilisé pour produire une archive destinée à une revue de code.

---

# Tests et couverture

## `test_solution.bat`

Le script `tools/test_solution.bat` exécute la chaîne complète de tests et de couverture.

Il :

1. restaure les dépendances NuGet et LibMan via `restore_solution.bat` ;
2. restaure les dépendances de `BudgetManager.Web.JsTests` avec `npm ci` ;
3. compile et exécute tous les tests .NET de la solution ;
4. exécute les tests JavaScript avec Vitest, collecte leur couverture V8 et vérifie que des lignes de production ont réellement été instrumentées ;
5. génère les résultats .NET au format TRX et la couverture Cobertura ;
6. installe `ReportGenerator` s'il n'est pas disponible ;
7. génère le rapport HTML consolidé de couverture .NET ;
8. ouvre le rapport .NET dans le navigateur.

Utilisation :

```bat
tools\test_solution.bat
```

Pour générer le rapport sans l'ouvrir automatiquement :

```bat
tools\test_solution.bat --no-open
```

Les résultats sont générés dans :

```text
tests_results
├── test_runs
└── coverage_report
```

Le rapport HTML principal de couverture .NET est :

```text
tests_results\coverage_report\index.html
```

Le rapport de couverture JavaScript généré par Vitest est :

```text
tests\BudgetManager.Web.JsTests\coverage\index.html
```

Les deux couvertures restent séparées : ReportGenerator consolide les rapports .NET, tandis que Vitest/V8 produit le rapport JavaScript. Le script contrôle `coverage-summary.json` et échoue si aucune ligne JavaScript de production n'a été instrumentée.

Les tests Infrastructure utilisent Testcontainers et nécessitent donc un moteur Docker opérationnel.

Pour exécuter les tests directement, sans passer par le script `tools/test_solution.bat`, avec génération des résultats TRX et de la couverture Cobertura :

```powershell
dotnet test --solution BudgetManager.sln --results-directory .\tmp -- --report-xunit-trx --coverlet --coverlet-output-format cobertura --coverlet-include "[BudgetManager.*]*"
```

Cette commande utilise Microsoft.Testing.Platform et génère, pour chaque projet de tests, un fichier TRX et un rapport de couverture Cobertura dans `tmp`.

---

# Entity Framework Core et migrations

Les commandes suivantes concernent le contexte SQL Server utilisé par l'Infrastructure :

```text
BudgetManager.Infrastructure.Persistence.ApplicationDbContext
```

Elles sont à lancer depuis la racine de la solution.

## Ajouter une migration

```powershell
dotnet ef migrations add <MigrationName> `
    --context BudgetManager.Infrastructure.Persistence.ApplicationDbContext `
    --project .\src\BudgetManager.Infrastructure\BudgetManager.Infrastructure.csproj `
    --startup-project .\src\BudgetManager.Infrastructure\BudgetManager.Infrastructure.csproj `
    --output-dir Persistence\Migrations
```
```powershell
dotnet ef migrations add <MigrationName> --context BudgetManager.Infrastructure.Persistence.ApplicationDbContext --project .\src\BudgetManager.Infrastructure\BudgetManager.Infrastructure.csproj --startup-project .\src\BudgetManager.Infrastructure\BudgetManager.Infrastructure.csproj --output-dir Persistence\Migrations
```

## Supprimer la dernière migration

```powershell
dotnet ef migrations remove `
    --context BudgetManager.Infrastructure.Persistence.ApplicationDbContext `
    --project .\src\BudgetManager.Infrastructure\BudgetManager.Infrastructure.csproj `
    --startup-project .\src\BudgetManager.Infrastructure\BudgetManager.Infrastructure.csproj
```
```powershell
dotnet ef migrations remove --context BudgetManager.Infrastructure.Persistence.ApplicationDbContext --project .\src\BudgetManager.Infrastructure\BudgetManager.Infrastructure.csproj --startup-project .\src\BudgetManager.Infrastructure\BudgetManager.Infrastructure.csproj
```

## Mettre à jour la base de développement

```powershell
dotnet ef database update `
    --context BudgetManager.Infrastructure.Persistence.ApplicationDbContext `
    --project .\src\BudgetManager.Infrastructure\BudgetManager.Infrastructure.csproj `
    --startup-project .\src\BudgetManager.Infrastructure\BudgetManager.Infrastructure.csproj
```
```powershell
dotnet ef database update --context BudgetManager.Infrastructure.Persistence.ApplicationDbContext --project .\src\BudgetManager.Infrastructure\BudgetManager.Infrastructure.csproj --startup-project .\src\BudgetManager.Infrastructure\BudgetManager.Infrastructure.csproj
```

`database update` est destiné au poste de développement. Le mécanisme de déploiement des migrations en production devra être défini avec la couche de présentation et le processus de déploiement.

## Index particuliers de la première migration

Avec EF Core 10, les index uniques portant sur les valeurs des complex types `Iban` et `Bic` sont ajoutés manuellement dans la migration initiale.

La première migration doit contenir :

```csharp
migrationBuilder.CreateIndex(
    name: "IX_Accounts_Iban",
    table: "Accounts",
    column: "Iban",
    unique: true);

migrationBuilder.CreateIndex(
    name: "IX_Banks_Bic",
    table: "Banks",
    column: "Bic",
    unique: true);
```

Cette particularité doit être conservée tant que le modèle EF Core ne représente pas directement ces index.

---

# Seeding des utilisateurs

La structure de configuration du seeding est la suivante :

```json
{
  "Seed": {
    "Users": {
      "Enabled": true,
      "Users": {
        "<UserCode>": {
          "Enabled": true,
          "Role": "Administrator",
          "Email": "admin@budgetmanager.local",
          "LastName": "Administrator",
          "FirstName": "Bootstrap"
        }
      }
    }
  }
}
```

La configuration détaillée et le comportement du seeding sont décrits dans [Infrastructure](infrastructure.md).

## Mot de passe en développement

Le mot de passe ne doit pas être ajouté dans un fichier de configuration versionné.

Les User Secrets sont associés à `BudgetManager.Web`, qui est le projet de démarrage et le composition root de l'application.

Depuis la racine de la solution, initialiser User Secrets une seule fois :

```powershell
dotnet user-secrets init --project .\src\BudgetManager.Web\BudgetManager.Web.csproj
```

Ajouter ou remplacer le mot de passe d'un utilisateur seedé :

```powershell
dotnet user-secrets set "Seed:Users:Users:<UserCode>:Password" "<password>" --project .\src\BudgetManager.Web\BudgetManager.Web.csproj
```

Par exemple pour `BootstrapAdmin` :

```powershell
dotnet user-secrets set "Seed:Users:Users:BootstrapAdmin:Password" "<password>" --project .\src\BudgetManager.Web\BudgetManager.Web.csproj
```

Afficher les secrets configurés :

```powershell
dotnet user-secrets list --project .\src\BudgetManager.Web\BudgetManager.Web.csproj
```

Il n'est pas nécessaire d'échapper les `:` de la clé dans PowerShell.

## Mot de passe en production

En production, le secret doit provenir du mécanisme de secrets de l'environnement de déploiement.

Exemple de variable d'environnement Docker :

```yaml
environment:
  Seed__Users__Enabled: "true"
  Seed__Users__Users__BootstrapAdmin__Password: "${BOOTSTRAP_USER_PASSWORD}"
```

`BOOTSTRAP_USER_PASSWORD` doit lui-même être fourni par le gestionnaire de secrets de l'environnement.
