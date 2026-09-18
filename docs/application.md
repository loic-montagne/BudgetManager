# Application

## Objectif

Le projet `BudgetManager.Application` contient les cas d'utilisation.

Il orchestre les interactions entre :

- le domaine ;
- la persistence ;
- les services externes.

Les invariants propres à un agrégat restent dans le Domain.

L’Application orchestre les règles impliquant plusieurs agrégats ou nécessitant un accès à la persistence, sans dupliquer les comportements métier internes aux entités.

---

# Organisation

Les fonctionnalités sont organisées par domaine métier.

Exemple :

```
Budget
├── Create
├── Update
├── Delete
├── GetById
├── Search
├── ReorderCategories
└── Common
```

Un dossier de cas d’utilisation contient généralement :

- une commande ou une requête ;
- son handler ;
- un validator lorsqu’une validation est nécessaire ;
- éventuellement les DTO propres au cas d’utilisation.

---

# CQRS

Les écritures utilisent des commandes.

Les lectures utilisent des requêtes.

Les lectures ne chargent jamais les agrégats.

Elles utilisent directement des Query Services retournant des DTO.

---

# Validation

La validation est réalisée avec FluentValidation.

Les validators vérifient :

- les données saisies ;
- les préconditions ;
- certaines règles nécessitant une lecture de la persistence.

Les validators ne remplacent jamais les règles du domaine.

---

# Contexts

Les Contexts constituent la couche d'accès aux agrégats pendant un cas d'utilisation.

Ils permettent :

- de charger les agrégats ;
- de partager une même instance pendant tout le scope ;
- de centraliser certaines vérifications simples.

Ils évitent les chargements multiples du même agrégat.

---

# Behaviors

Deux behaviors MediatR sont utilisés.

## AuthorizationBehavior

Vérifie que l'utilisateur est authentifié.

## ValidationBehavior

Exécute les validators avant le handler.

Les validators sont exécutés séquentiellement afin d'éviter les accès concurrents au même DbContext.

---

# Repositories

Les handlers utilisent les repositories uniquement pour les écritures.

Les repositories retournent toujours des agrégats complets et suivis par Entity Framework.

---

# Query Services

Les lectures utilisent des Query Services.

Ils retournent des DTO projetés.

Ils ne retournent jamais d'entités du domaine.

---

# Gestion des erreurs

Les validators retournent des erreurs de validation.

Les handlers peuvent laisser remonter les exceptions métier.

L'interface est responsable de convertir ces erreurs en réponses adaptées au protocole utilisé.
---

# Cycle de vie des utilisateurs

La création d'un utilisateur administré ne demande pas de mot de passe à l'administrateur.

`CreateUserCommandHandler` génère un mot de passe temporaire de 20 caractères via `IPasswordGenerator`, puis le transmet à `IUserManager.CreateAsync(...)`. Le mot de passe temporaire n'est jamais exposé dans le contrat de création. Une fois l'utilisateur créé, le handler déclenche `SendUserActivationEmailCommand` et retourne l'identifiant du compte ainsi que la date d'expiration du lien d'activation.

L'activation du compte est séparée en deux cas d'utilisation :

- `SendUserActivationEmailCommand` reçoit le nom de la page d'activation, les valeurs de route, la durée de validité et le sujet du mail ; son handler fait générer le token et l'URL par `IActivationUrlGenerator`, envoie le lien d'activation puis enregistre en UTC la date d'envoi et la date d'expiration du lien ;
- `ActivateUserCommand` valide le compte, le token d'activation et le nouveau mot de passe choisi par l'utilisateur, puis délègue l'activation à `IUserManager`.

Le mail affiche la date d'expiration convertie dans le fuseau horaire de l'instance via `IDateTimeLocalizer`. Les valeurs persistées restent en UTC.

Lors de l'activation, Infrastructure confirme l'adresse e-mail, supprime le mot de passe temporaire, enregistre le mot de passe choisi par l'utilisateur et efface `ActivationEmailSentOn` / `ActivationEmailExpiresOn` dans une même transaction.

Le changement d'adresse e-mail et la réinitialisation d'un mot de passe restent des flux distincts de l'activation du compte.

---

# E-mails

L'Application définit les contrats et les modèles nécessaires à l'envoi d'e-mails sans dépendre d'une technologie SMTP particulière.

Les interfaces techniques sont regroupées dans :

```text
Abstractions/Email
├── IEmailSender
├── IEmailTemplateRenderer
└── ITemplatedEmailSender
```

Les modèles de message sont regroupés dans :

```text
Email
├── EmailAddress
├── EmailAttachment
├── EmailInlineImage
├── EmailMessageBase
├── EmailMessage
├── TemplatedEmailMessage
├── RenderedEmailTemplate
├── EmailTemplates
└── Templates
```

## Messages directs

`EmailMessage` représente un message prêt à être envoyé.

Il supporte :

- les destinataires `To`, `Cc` et `Bcc` ;
- un corps texte ;
- un corps HTML ;
- les pièces jointes ;
- les images intégrées au contenu HTML.

Les streams utilisés pour les pièces jointes et les images intégrées restent la propriété de l'appelant. Ils doivent rester ouverts jusqu'à la fin de `SendAsync` et sont libérés par l'appelant.

## Messages basés sur un template

`TemplatedEmailMessage` représente un message dont le corps doit être rendu à partir d'un template.

Le rendu est délégué à `IEmailTemplateRenderer`, puis le message final est envoyé par `IEmailSender`.

`ITemplatedEmailSender` encapsule cette séquence :

```text
TemplatedEmailMessage
        ↓
IEmailTemplateRenderer
        ↓
EmailMessage
        ↓
IEmailSender
```

Les noms de templates connus sont centralisés dans `EmailTemplates` afin d'éviter les chaînes littérales dispersées dans le code.

L'Application ne connaît ni MailKit, ni MimeKit, ni le protocole SMTP.


# Ordre des catégories d’un budget

Le cas d’utilisation `Budget/ReorderCategories` reçoit l’identifiant du budget et la collection ordonnée des identifiants de catégories. Le Domain valide et applique l’ordre ; l’Application vérifie au préalable l’existence, l’éditabilité du budget et l’association des catégories.
