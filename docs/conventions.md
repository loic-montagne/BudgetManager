# Conventions de développement

Ce document décrit les conventions utilisées dans le projet.

L'objectif est de conserver un code homogène, lisible et facile à maintenir.

---

# Principes généraux

Les principes suivants guident le développement :

- privilégier la simplicité ;
- écrire un code explicite ;
- limiter les effets de bord ;
- privilégier les règles métier dans le domaine ;
- éviter les optimisations prématurées.

---

# Organisation des projets

La solution est organisée selon la Clean Architecture.

```
src
├── BudgetManager.Domain
├── BudgetManager.Application
├── BudgetManager.Infrastructure
└── BudgetManager.Web

tests
├── BudgetManager.Domain.Tests
├── BudgetManager.Application.Tests
├── BudgetManager.Infrastructure.Tests
└── BudgetManager.Web.IntegrationTests
```

Chaque projet possède une responsabilité unique.

---

# Organisation des fonctionnalités

Les cas d'utilisation sont organisés par fonctionnalité.

Exemple :

```
Budget
├── Create
├── Update
├── Delete
├── GetById
├── Search
└── Common
```

Un dossier de cas d’utilisation contient généralement :

- une commande ou une requête ;
- son handler ;
- un validator lorsqu’une validation est nécessaire ;
- éventuellement les DTO propres au cas d’utilisation.

---

# Organisation des classes

L'ordre des membres est le suivant :

1. constructeur primaire
1. constantes
1. champs privés
1. propriétés publiques
1. constructeur explicite
1. méthodes privées
1. méthodes protégées
1. méthodes publiques


---

# Constructeurs

Les constructeurs primaires sont privilégiés lorsqu'ils améliorent la lisibilité.

Exemple :

```csharp
public sealed class BudgetContext(
    IBudgetRepository repository,
    IEntityCacheContext cache)
```

---

# Nommage

Les noms doivent être explicites.

Privilégier :

```text
currentUserId
transferAccountId
budgetCategory
```

Éviter :

```text
id
item
obj
tmp
```

Les noms courts comme `id` ou `item` sont acceptables lorsque leur signification est évidente dans un contexte très limité. Dans les autres cas, un nom métier explicite est privilégié. Les abréviations sont évitées lorsqu'elles nuisent à la compréhension.

---

# Méthodes

Une méthode doit représenter une seule intention.

Une méthode courte est généralement préférable à une méthode complexe.

Une méthode privée n'est extraite que lorsqu'elle améliore réellement la lisibilité.

---

# Exceptions

Les exceptions représentent :

- une violation des règles métier ;
- une erreur technique inattendue.

Elles ne sont jamais utilisées pour piloter un comportement normal.

---

# Domaine

Le domaine est la seule source de vérité.

Les règles métier sont toujours implémentées dans le domaine.

Les validators ne remplacent jamais les invariants métier.

---

# Application

Les handlers orchestrent les cas d'utilisation.

Ils :

- chargent les agrégats ;
- invoquent le domaine ;
- demandent la persistance.

Ils ne contiennent pas de logique métier.

---

# Validators

Les validators vérifient :

- les données d'entrée ;
- les préconditions ;
- les vérifications nécessitant un accès à la persistence.

Les validators ne modifient jamais l'état du système.

Les règles dépendantes utilisent `DependentRules` lorsque cela permet d'éviter des vérifications inutiles.

---

# Contexts

Les Contexts sont utilisés pour :

- charger les agrégats ;
- partager les instances pendant le scope courant ;
- centraliser certaines vérifications simples.

Ils remplacent les chargements multiples d'un même agrégat.

Les méthodes booléennes ne lèvent jamais d'exception lorsqu'une ressource est absente.

---

# Repositories

Les repositories sont utilisés uniquement par les commandes.

Les lectures utilisent des Query Services.

Les méthodes `GetTrackedByIdAsync()` retournent toujours un agrégat complet.

---

# Query Services

Les Query Services :

- retournent des DTO ;
- utilisent `AsNoTracking()` ;
- n'exposent jamais les entités du domaine.

---

# Entity Framework Core

Les configurations sont réalisées via `IEntityTypeConfiguration<T>`.

Les entités ne contiennent aucun attribut Entity Framework.

Le mapping est entièrement externalisé.

---

# Nullabilité

Les références nullables sont activées.

Le mot-clé `required` est privilégié lorsque cela améliore la sécurité.

Les opérateurs `!` doivent rester exceptionnels.

---

# Commentaires

Le code doit être suffisamment explicite pour se comprendre sans commentaire.

Les commentaires XML sont réservés :

- aux interfaces publiques ;
- aux comportements non évidents ;
- aux règles métier particulières.

Les commentaires expliquant *comment* fonctionne le code sont évités.

Les commentaires expliquant *pourquoi* un choix a été fait sont encouragés.

---

# Tests

Les tests suivent systématiquement le modèle :

- Arrange
- Act
- Assert

Chaque test vérifie une seule intention.

Les noms des tests suivent le format :

```
Méthode_Condition_RésultatAttendu
```

Exemple :

```
TransferOwnership_WhenCurrentUserIsNotOwner_Throws
```

---

# CancellationToken

Les tests utilisent systématiquement :

```csharp
TestContext.Current.CancellationToken
```

Les handlers, repositories et Query Services propagent toujours le `CancellationToken`.

---

# Mocks

Les dépendances sont simulées avec NSubstitute.

Les tests ne dépendent jamais d'une base de données réelle.

---

# Formatage

Le code est volontairement aéré.

Les appels de méthodes sont écrits sur plusieurs lignes lorsque cela améliore la lisibilité.

Exemple :

```csharp
await repository.UpdateAsync(
    budget,
    cancellationToken);
```

La lisibilité est toujours privilégiée par rapport au nombre de lignes.

---

# Philosophie

Le projet privilégie :

- la lisibilité ;
- la simplicité ;
- des responsabilités clairement séparées ;
- un domaine riche ;
- une architecture facilement testable.

Lorsqu'un choix est possible entre une solution plus courte et une solution plus explicite, la solution la plus explicite est privilégiée.

Une convention n'est adoptée que si elle améliore durablement la lisibilité, la cohérence ou la maintenabilité du code.
---

# Ressources et streams

Lorsqu'un modèle Application transporte un `Stream`, la propriété de la ressource reste explicitement définie par le contrat public.

Pour les pièces jointes et les images inline des e-mails :

- le service d'e-mail ne dispose pas les streams fournis ;
- l'appelant conserve leur propriété ;
- les streams doivent rester ouverts jusqu'à la fin de `SendAsync` ;
- l'appelant est responsable de leur libération.

Cette règle est documentée dans les commentaires XML des abstractions concernées.
