# Architecture

## Présentation

BudgetManager est une application de gestion de budget développée selon les principes de la Clean Architecture.

L'objectif est de maintenir une séparation claire entre :

- le domaine métier ;
- les cas d'utilisation ;
- l'infrastructure technique ;
- la présentation.

Chaque couche possède une responsabilité unique et ne dépend que des couches situées en dessous.

```
                 Web
              ┌───┴───┐
              ▼       ▼
        Application  Infrastructure
              │       │
              └───┬───┘
                  ▼
                Domain


Web ───────────────► Application
Web ───────────────► Infrastructure
Infrastructure ────► Application
Infrastructure ────► Domain
Application ───────► Domain
```

Le domaine est totalement indépendant des technologies utilisées.

---

# Organisation de la solution

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

---

# Domain

Le projet Domain contient exclusivement les règles métier.

Il regroupe :

- les agrégats ;
- les entités ;
- les objets valeur ;
- les énumérations ;
- les exceptions métier.

Le Domain :

- ne dépend d'aucune technologie ;
- ne connaît ni Entity Framework, ni MediatR, ni ASP.NET Core ;
- garantit les invariants métier.

Toutes les règles métier doivent être appliquées par le Domain.

---

# Application

Le projet Application contient les cas d'utilisation.

Chaque cas d'utilisation est organisé par fonctionnalité.

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

# Validation

La validation est répartie entre deux niveaux.

## FluentValidation

Les validators vérifient :

- le format des données ;
- les règles nécessitant l'accès à la persistence ;
- les préconditions des cas d'utilisation.

Ils ne remplacent jamais les règles métier.

## Domaine

Le domaine reste responsable de :

- tous les invariants ;
- toutes les règles métier ;
- toutes les autorisations métier.

Même si un validator oublie une règle, le domaine reste cohérent.

---

# Contexts

Les validators et les handlers utilisent des Contexts.

Les Contexts encapsulent :

- le chargement des agrégats ;
- le cache scoped ;
- certaines requêtes métier simples.

Ils évitent :

- les chargements multiples du même agrégat ;
- la duplication du code d'accès aux repositories.

Chaque Context charge des entités suivies (tracked) par Entity Framework.

Pendant toute la durée d'un cas d'utilisation, une même entité n'est chargée qu'une seule fois.

---

# Repositories

Les repositories sont utilisés uniquement pour les commandes.

Ils manipulent les agrégats du domaine.

Les méthodes `GetTrackedByIdAsync()` retournent toujours un agrégat complet.

---

# Query Services

Les lectures utilisent des Query Services.

Ils retournent directement des DTO projetés.

Ils ne chargent jamais d'agrégats.

Cette séparation permet :

- d'optimiser les lectures ;
- d'éviter le tracking inutile ;
- de conserver un domaine indépendant.

---

# Behaviors MediatR

Deux behaviors sont utilisés.

## AuthorizationBehavior

Vérifie que l'utilisateur courant est authentifié.

## ValidationBehavior

Exécute tous les validators avant l'exécution du handler.

Les validators sont exécutés séquentiellement afin d'éviter les accès concurrents au même DbContext.

---

# Gestion des erreurs

Le domaine lève des exceptions lorsqu’un invariant ou une règle métier est violé.

L’Application utilise également des exceptions applicatives pour représenter notamment :

- une ressource introuvable ;
- une requête invalide ;
- un utilisateur non authentifié ;
- un accès interdit.

La couche de présentation transforme ces erreurs en réponses adaptées au protocole utilisé.

---

# Concurrence

La concurrence est gérée au niveau de la persistence grâce au mécanisme de concurrence optimiste (RowVersion).

Les validators ne garantissent jamais l'absence de conflit concurrent.

Le domaine vérifie systématiquement les invariants lors des mutations.

---

# Services externes et e-mails

Les dépendances vers des services externes suivent le principe d'inversion des dépendances.

Pour l'e-mail :

```text
Application
    IEmailSender
    IEmailTemplateRenderer
    ITemplatedEmailSender
          ▲
          │ implémentations
          │
Infrastructure
    SmtpEmailSender
    EmailTemplateRenderer
    TemplatedEmailSender
          ▲
          │ utilisation directe
          │
Web / pages Identity personnalisées
    ITemplatedEmailSender
```

Les modèles d'e-mail appartiennent à l'Application et restent indépendants de SMTP, MailKit et MimeKit.

Les pages Identity personnalisées n'utilisent pas l'interface `Microsoft.AspNetCore.Identity.UI.Services.IEmailSender` comme abstraction d'envoi. Elles s'appuient directement sur `ITemplatedEmailSender`, ce qui permet d'utiliser les templates localisés et les sujets définis par Budget Manager.

---

# Tests

Les tests sont répartis par couche.

## Domain

Les tests du Domain couvrent :

- les règles métier ;
- les invariants ;
- les objets valeur.

## Application

Les tests Application couvrent :

- les handlers ;
- les validators ;
- les behaviors ;
- les contexts ;
- les conventions d'architecture.

L'objectif est de détecter rapidement toute régression fonctionnelle.

---

# Principes de conception

Les principaux principes suivis dans le projet sont :

- Clean Architecture
- Domain-Driven Design
- CQRS
- Repository Pattern
- Dependency Injection
- Validation Behavior
- Optimistic Concurrency
- Aggregate Root