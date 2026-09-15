# Décisions d'architecture

Ce document recense les principaux choix de conception du projet ainsi que leur justification.

---

## Les validators peuvent accéder à la persistence

Les validators utilisent des Contexts afin de vérifier certaines préconditions.

Exemples :

- existence d'une entité ;
- unicité d'un nom ;
- association entre deux agrégats.

Les règles métier restent systématiquement validées par le domaine.

---

## Absence de UnitOfWork

Le projet n'utilise pas de UnitOfWork explicite.

Entity Framework Core joue déjà ce rôle.

Ajouter une abstraction supplémentaire n'apporterait pas de bénéfice significatif.

---

## Utilisation des Contexts

Les Contexts évitent de charger plusieurs fois le même agrégat pendant un cas d'utilisation.

Ils encapsulent également certaines vérifications simples.

---

## Exécution séquentielle des validators

Les validators FluentValidation sont exécutés séquentiellement.

Cela évite plusieurs accès concurrents au même DbContext.

---

## Séparation Repository / Query Service

Les écritures utilisent des repositories.

Les lectures utilisent des Query Services.

Cette séparation permet :

- d'optimiser les requêtes ;
- d'éviter le tracking inutile ;
- de limiter les dépendances du domaine.

---

## Le domaine reste la source de vérité

Les validators améliorent l'expérience utilisateur.

Ils ne garantissent jamais les invariants métier.

Le domaine effectue systématiquement ses propres contrôles avant toute mutation.

---

## Égalité des entités

L'égalité des entités repose uniquement sur leur identifiant.

Ce choix simplifie les comparaisons tout en restant cohérent avec le modèle métier.

---

## Concurrence optimiste

Les conflits d'écriture sont gérés grâce à une colonne `RowVersion`.

La persistence détecte les conflits.

L'application les transforme ensuite en erreur fonctionnelle.
---

## Service d'e-mail indépendant d'Identity

L'envoi d'e-mails n'est pas construit autour de l'interface `IEmailSender` d'ASP.NET Core Identity.

L'Application définit ses propres abstractions :

- `IEmailSender` pour envoyer un message déjà construit ;
- `IEmailTemplateRenderer` pour rendre les templates ;
- `ITemplatedEmailSender` pour combiner rendu et envoi.

Infrastructure fournit l'implémentation SMTP avec MailKit/MimeKit.

Les pages Identity personnalisées utilisent directement `ITemplatedEmailSender` pour les e-mails applicatifs (activation de compte, réinitialisation du mot de passe et changement d’adresse e-mail). Il n’existe plus d’adaptateur `IdentityEmailSender`.

Ce choix permet :

- de réutiliser l'infrastructure d'e-mail en dehors d'Identity ;
- de conserver les dépendances MailKit/MimeKit dans Infrastructure ;
- de supporter texte, HTML, pièces jointes et images inline ;
- de faire évoluer les templates sans coupler l'Application au protocole SMTP.

## Templates HTML et texte embarqués

Les templates d'e-mail sont stockés comme ressources embarquées dans Infrastructure.

Cette solution évite de dépendre de chemins physiques qui pourraient différer entre le développement et la publication.

Les données dynamiques sont encodées dans les templates HTML afin d'éviter l'injection accidentelle de HTML. Les templates texte ne subissent pas cet encodage.


## Séparation des durées de vie des tokens Identity

L'activation du compte, le changement d'adresse e-mail et la réinitialisation du mot de passe utilisent trois providers de tokens distincts.

Cette séparation évite qu'une durée courte adaptée au mot de passe oublié réduise implicitement la durée du lien d'activation. Les durées sont configurables et validées au démarrage de l'application.

## Stockage UTC et conversion à la présentation

Les dates sont conservées en UTC dans la persistence. Le fuseau horaire de l'instance est une configuration de présentation et la conversion est centralisée derrière `IDateTimeLocalizer`.

Ce choix évite d'introduire des heures locales dans les données persistées et laisse `TimeZoneInfo` appliquer les règles de changement d'heure du fuseau configuré.
