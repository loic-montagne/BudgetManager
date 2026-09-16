# Tests

## Objectif

Les tests ont pour objectif de détecter rapidement les régressions fonctionnelles tout en conservant une suite de tests lisible, maintenable et suffisamment indépendante des détails d'implémentation.

L'objectif n'est pas d'obtenir artificiellement 100 % de couverture de code, mais de couvrir l'ensemble des comportements métier significatifs.

---

# Organisation

Les tests sont répartis selon les différentes couches de l'application.

```
tests
├── BudgetManager.Domain.Tests
├── BudgetManager.Application.Tests
├── BudgetManager.Infrastructure.Tests
├── BudgetManager.Web.Tests
└── BudgetManager.Web.JsTests
```

Chaque projet de tests est responsable d'une seule couche.

`BudgetManager.Web.Tests` couvre la couche de présentation ASP.NET Core avec des tests unitaires ciblés sur les comportements Web.

## Plateforme de tests

La solution utilise Microsoft.Testing.Platform comme runner de tests .NET.

Les projets de tests utilisent xUnit v3. La couverture de code est collectée avec `coverlet.MTP` et peut être générée au format Cobertura. Les résultats de tests au format TRX sont produits par l'intégration xUnit pour Microsoft.Testing.Platform.

La sélection de Microsoft.Testing.Platform comme runner est définie dans le fichier `global.json` à la racine de la solution.

---

# Philosophie

Les tests doivent :

- vérifier un comportement ;
- rester simples à comprendre ;
- être indépendants les uns des autres ;
- documenter le comportement attendu.

Chaque test représente une seule intention.

La duplication est préférée à une factorisation excessive lorsqu'elle améliore la lisibilité.

---

# Organisation des tests

Les tests suivent systématiquement le modèle :

- Arrange
- Act
- Assert

Exemple :

```csharp
// Arrange

...

// Act

...

// Assert

...
```

Les méthodes de test suivent la convention :

```
Méthode_Condition_RésultatAttendu
```

Exemple :

```
TransferOwnership_WhenCurrentUserIsNotOwner_Throws
```

---

# Tests du domaine

Les tests du domaine vérifient notamment :

- les invariants métier ;
- les règles de gestion ;
- les méthodes métier ;
- les objets valeur ;
- les exceptions métier.

Ils ne dépendent d'aucune infrastructure.

---

# Tests de l'application

Les tests de l'application couvrent notamment :

- les handlers ;
- les validators ;
- les behaviors ;
- les contexts ;
- les conventions d'architecture ;
- le cycle de vie des utilisateurs, notamment la génération du mot de passe temporaire, l'activation et l'envoi de l'e-mail d'activation.

Pour la création d'un utilisateur, le mot de passe n'est plus fourni ni validé par `CreateUserCommand` : il est généré par `IPasswordGenerator`. Les tests de création vérifient donc que le handler demande un mot de passe temporaire de 20 caractères, le transmet à `IUserManager`, déclenche `SendUserActivationEmailCommand` et retourne l'identifiant du compte avec la date d'expiration du lien. La politique de mot de passe choisie par l'utilisateur est testée lors de l'activation du compte, là où cette règle métier est réellement appliquée.

Ils vérifient principalement :

- les interactions entre composants ;
- les appels aux repositories et Query Services ;
- la propagation du `CancellationToken` ;
- les comportements attendus des cas d'utilisation.

---

# Tests de l'infrastructure

Les tests de l'infrastructure combinent :

- des tests unitaires pour les composants purement techniques ;
- des tests d'intégration contre une instance réelle de SQL Server lancée avec Testcontainers.

Les tests d'intégration vérifient notamment :

- l'application des migrations ;
- les opérations Identity transactionnelles, notamment l'activation d'un compte et le remplacement du mot de passe temporaire ;
- le rollback complet de l'activation si le token ou le nouveau mot de passe est invalide ;
- la conservation des informations d'activation tant que le compte n'est pas activé ;
- la configuration des providers de tokens et de leurs durées de validité ;
- la génération du mot de passe temporaire conformément à la politique Identity ;
- la conversion des dates vers le fuseau horaire configuré, y compris les différences heure d'hiver / heure d'été ;
- le mapping EF Core ;
- les contraintes et index SQL Server ;
- la concurrence optimiste via `RowVersion` ;
- les repositories ;
- les Query Services et leur traduction SQL ;
- la pagination, les tris et les recherches ;
- les complex types `Iban` et `Bic` ;
- l'audit ;
- l'initialisation et le seeding Identity.

Chaque test d'intégration utilise une base de données isolée.

## Prérequis Docker

L'exécution de `BudgetManager.Infrastructure.Tests` nécessite un moteur Docker opérationnel.

Sous Windows, l'environnement recommandé est Docker Desktop avec le backend WSL 2.

### Installation sous Windows

Vérifier que WSL est disponible :

```powershell
wsl --version
```

Si WSL n'est pas installé, ouvrir PowerShell en tant qu'administrateur et exécuter :

```powershell
wsl --install
```

Redémarrer Windows si l'installation le demande.

Si WSL est déjà installé, il peut être mis à jour et configuré pour utiliser WSL 2 par défaut :

```powershell
wsl --update
wsl --set-default-version 2
```

Installer ensuite Docker Desktop for Windows en conservant le backend WSL 2, puis démarrer Docker Desktop.

### Vérification

Vérifier que le client et le serveur Docker sont accessibles :

```powershell
docker version
```

Puis vérifier qu'un conteneur peut réellement être exécuté :

```powershell
docker run --rm hello-world
```

Les tests Infrastructure peuvent alors être lancés depuis la racine de la solution :

```powershell
dotnet test tests/BudgetManager.Infrastructure.Tests
```

Ils peuvent également être exécutés depuis Test Explorer dans Visual Studio. Docker Desktop doit simplement être démarré en arrière-plan.

Il n'est pas nécessaire d'installer SQL Server localement : Testcontainers démarre automatiquement le conteneur SQL Server utilisé par les tests.

Le premier lancement peut être plus long, car Docker doit télécharger l'image SQL Server si elle n'est pas encore disponible localement.

Les projets `BudgetManager.Domain.Tests` et `BudgetManager.Application.Tests` ne nécessitent pas Docker.

---

# Doubles de test

Les dépendances sont simulées avec NSubstitute lorsqu'un test unitaire ne nécessite pas l'implémentation réelle.

Les tests Domain et Application ne dépendent pas d'une base de données.

Les tests Infrastructure utilisent au contraire SQL Server lorsque le comportement à vérifier dépend réellement du moteur relationnel ou de la traduction EF Core.

---

# CancellationToken

Tous les tests utilisent :

```csharp
TestContext.Current.CancellationToken
```

afin de reproduire le comportement réel des cas d'utilisation.

---

# Lisibilité

Les tests privilégient la lisibilité.

Les variables possèdent des noms explicites.

Les appels de méthodes sont volontairement écrits sur plusieurs lignes lorsque cela améliore la compréhension.

---

# Régressions

Lorsqu'un bug est corrigé, un test reproduisant ce scénario doit être ajouté avant la correction lorsque cela est possible.

Les tests doivent être placés au niveau qui porte réellement la règle :

- Application pour l'orchestration, la validation et les interactions entre abstractions ;
- Infrastructure pour les comportements Identity, EF Core, SQL Server, configuration et transactions réelles ;
- Domain pour les invariants métier indépendants de toute infrastructure.

Un test devenu lié à un comportement supprimé doit être supprimé ou adapté plutôt que conservé artificiellement pour maintenir un nombre de tests ou un taux de couverture.

Les validators qui combinent validation d’identifiant et chargement d’un utilisateur doivent aussi être testés avec un identifiant vide ou inexistant afin de garantir qu’une erreur de validation est retournée sans exception secondaire.

La suite de tests constitue une protection contre les régressions fonctionnelles futures.
---

# Tests Web

Le projet `BudgetManager.Web.Tests` couvre les comportements C# propres à la couche de présentation sans dupliquer les règles métier déjà vérifiées dans Domain, Application et Infrastructure.

La suite couvre notamment :

- le protocole DataTables moderne accepté par `DataTablesModelBinder` ;
- les extensions Web et la gestion du cookie de culture ;
- l'adaptation de l'utilisateur courant à partir des claims HTTP ;
- les modèles Web et contrats de binding significatifs ;
- la localisation des erreurs métier ;
- le `RequiredLabelTagHelper` ;
- les contrôleurs MVC, leurs résultats, la propagation des paramètres vers MediatR et le contrat JSON DataTables ;
- les conventions et comportements simples des Razor PageModels Identity.

Les fichiers Razor `.cshtml` et CSS ne sont pas testés directement par xUnit. Le JavaScript applicatif dispose désormais de tests unitaires dédiés dans `BudgetManager.Web.JsTests`. Les scénarios nécessitant un navigateur ou le pipeline HTTP complet relèvent de futurs tests end-to-end/intégration dédiés si leur valeur justifie leur coût.

## Tests JavaScript

Les tests unitaires du JavaScript applicatif se trouvent dans
`tests/BudgetManager.Web.JsTests`.

Ils utilisent Vitest avec jsdom. Les scripts de production sont chargés comme modules par Vite depuis `src/BudgetManager.Web/wwwroot/js`, sans copie du code applicatif. Ce chargement permet à Vitest/V8 d'instrumenter réellement les fichiers de production et de mesurer leur couverture.

Installation des dépendances :

```bat
cd tests\BudgetManager.Web.JsTests
npm ci
```

Exécution :

```bat
npm test
```

Exécution avec couverture JavaScript :

```bat
npm run test:coverage
```

La première suite couvre `phone-number.js`, `password-requirements.js`,
`password-visibility.js`, `user-profile-menu.js` et `manage-profile.js`.
Les scripts plus fortement couplés à jQuery/DataTables (`sidebar.js`,
`entity-manager.js` et les managers d'entités) feront l'objet des passes
suivantes.

### Exécution globale

`tools\test_solution.bat` restaure également les dépendances npm avec `npm ci`,
exécute `npm run test:coverage` et produit le rapport HTML JavaScript dans :

```text
tests\BudgetManager.Web.JsTests\coverage\index.html
```

Le rapport HTML consolidé généré par ReportGenerator reste celui de la couverture .NET ; la couverture JavaScript est produite séparément par Vitest/V8. `test_solution.bat` vérifie également que le résumé Vitest contient bien des lignes instrumentées afin d'éviter qu'une exécution à 0 % due à un problème d'instrumentation soit considérée comme valide.
