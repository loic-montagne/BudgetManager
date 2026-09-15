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
└── BudgetManager.Infrastructure.Tests
```

Chaque projet de tests est responsable d'une seule couche.

`BudgetManager.Web.IntegrationTests` sera ajouté avec la couche Web.

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

Aucun projet de tests Web n'est encore ajouté à la solution. Les tests actuels restent limités aux couches Domain, Application et Infrastructure.

Les comportements Web spécifiques (pages Identity, génération des URLs publiques, rate limiting et gestion des forwarded headers) sont documentés dans `docs/web.md` et feront l'objet de tests d'intégration dédiés lorsqu'un projet `BudgetManager.Web.IntegrationTests` sera introduit.
