# BudgetManager

Projet personnel de gestion de budget.  
  
Le but de ce projet est de me permettre de monter en compétences sur la Clean Architecture et les principes du Domain-Driven Design.  
  
Certains choix techniques ne suivent pas strictement l’ensemble des recommandations associées à ces approches. J’ai privilégié un équilibre entre simplicité, lisibilité du code et respect des principes architecturaux, dans le contexte d’une application de taille limitée.

## État du projet

La solution contient actuellement les couches suivantes :

- Domain ;
- Application ;
- Infrastructure ;
- Web MVC / Razor Pages ;
- tests unitaires et d'intégration associés.

La couche Web utilise ASP.NET Core Identity pour l'authentification, la gestion du profil, la confirmation d'adresse e-mail, la récupération de mot de passe et la double authentification.

## Documentation

- [Architecture](docs/architecture.md)
- [Domaine](docs/domain.md)
- [Application](docs/application.md)
- [Infrastructure](docs/infrastructure.md)
- [Web](docs/web.md)
- [Tests](docs/testing.md)
- [Couverture des tests](docs/tests_coverage.md)
- [Décisions d’architecture](docs/decisions.md)
- [Conventions de développement](docs/conventions.md)
- [Outils et commandes](docs/tools.md)

## Prérequis

- SDK .NET 10 ;
- Docker pour l'exécution de `BudgetManager.Infrastructure.Tests` ;
- Node.js LTS et npm pour `BudgetManager.Web.JsTests`.

Sous Windows, Docker Desktop avec le backend WSL 2 est recommandé. Les instructions d'installation et de vérification sont détaillées dans [Tests](docs/testing.md).

Le poste de développement peut être préparé automatiquement avec `tools\configure_desktop.bat`. Les scripts et commandes disponibles sont décrits dans [Outils et commandes](docs/tools.md).

## Construire et tester

```bash
dotnet restore BudgetManager.sln
dotnet build BudgetManager.sln --configuration Release --no-restore
dotnet test --solution BudgetManager.sln --configuration Release --no-build
```

Les tests du domaine se trouvent dans `tests/BudgetManager.Domain.Tests`.  
Les tests de l'application se trouvent dans `tests/BudgetManager.Application.Tests`.  
Les tests de l'infrastructure se trouvent dans `tests/BudgetManager.Infrastructure.Tests` et nécessitent Docker.  
Les tests C# de la couche Web se trouvent dans `tests/BudgetManager.Web.Tests` et ne nécessitent pas Docker.  
Les tests JavaScript se trouvent dans `tests/BudgetManager.Web.JsTests` et utilisent Vitest avec jsdom.

## Intégration continue

Le workflow `.github/workflows/ci.yml` restaure les dépendances .NET et npm, compile la solution et exécute les tests .NET **et JavaScript** à chaque push sur les branches `main` ou `master`, ainsi que pour chaque pull request.  
  
Les résultats .NET générés dans `TestResults` et le rapport de couverture JavaScript généré par Vitest sont publiés comme artefacts GitHub Actions.


### Tests ciblés

`tools\test_solution.bat` permet d'exécuter uniquement les groupes utiles avec
`--domain`, `--application`, `--infrastructure`, `--web` et `--js`. Les
sélecteurs sont combinables. Par exemple :

```bat
tools\test_solution.bat --js
tools\test_solution.bat --web --js --no-open
```

Sans sélecteur, tous les tests sont exécutés.
