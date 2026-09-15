# Infrastructure

## Objectif

Le projet `BudgetManager.Infrastructure` contient l'ensemble des dépendances techniques.

Il implémente notamment :

- Entity Framework Core ;
- les repositories ;
- les Query Services ;
- les migrations ;
- les accès aux services externes.

---

# Persistence

Entity Framework Core est utilisé pour :

- le mapping des entités ;
- le suivi des agrégats ;
- la concurrence optimiste.

Les configurations sont réalisées via `IEntityTypeConfiguration<T>`.

---

# Repositories

Les repositories implémentent les interfaces définies dans `BudgetManager.Application`.

Ils chargent systématiquement les agrégats complets nécessaires aux commandes.

---

# Query Services

Les Query Services utilisent des projections LINQ.

Ils retournent directement les DTO attendus par l'application.

Ils utilisent systématiquement `AsNoTracking()`.

---

# Concurrence

La concurrence est gérée grâce à une colonne `RowVersion`.

Les conflits sont détectés lors de `SaveChanges()`.

L'application traduit ensuite ces conflits en erreur fonctionnelle.

---

# Initialisation de la base de données

L'infrastructure fournit le mécanisme permettant d'initialiser la base de données au démarrage de l'application.

L'initialisation applique les migrations EF Core en attente, puis exécute les seeders enregistrés dans le conteneur d'injection de dépendances.

Les seeders implémentent `IDataSeeder` et sont exécutés dans l'ordre défini par leur propriété `Order`.

## Rôles

`RoleSeeder` crée les rôles applicatifs nécessaires au fonctionnement de l'application.

Il est exécuté avant le seeding des utilisateurs afin de garantir que les rôles existent avant leur attribution.

Le seeder est idempotent : les rôles déjà présents ne sont pas recréés.

## Utilisateurs

`UsersSeeder` permet de créer un ou plusieurs utilisateurs initiaux à partir de la configuration de l'application.

Le seeding des utilisateurs peut être activé ou désactivé globalement. Chaque utilisateur configuré peut également être activé ou désactivé
individuellement.

Les utilisateurs sont identifiés dans la configuration par une clé logique indépendante de leur adresse email.

Exemple :

```json
{
  "Seed": {
    "Users": {
      "Enabled": true,
      "Users": {
        "BootstrapAdmin": {
          "Enabled": true,
          "Role": "Administrator",
          "Email": "admin@example.com",
          "FirstName": "Admin",
          "LastName": "Bootstrap"
        },
        "TestUser": {
          "Enabled": false,
          "Role": "User",
          "Email": "user@example.com",
          "FirstName": "Test",
          "LastName": "User"
        }
      }
    }
  }
}
```

Lorsqu'un utilisateur activé n'existe pas encore, il est créé via ASP.NET Core Identity puis le rôle configuré lui est attribué.

Lorsqu'il existe déjà, il n'est pas recréé. Le seeder garantit que le rôle configuré lui est attribué, mais ne synchronise pas ses autres informations.

Le seeding est donc principalement destiné à l'amorçage d'un environnement. Une fois créé, un utilisateur seedé est géré comme n'importe quel autre utilisateur de l'application et peut notamment être modifié ou supprimé.

## Mots de passe

Les mots de passe des utilisateurs seedés ne doivent jamais être stockés dans les fichiers de configuration versionnés.

En développement, ils peuvent être fournis à l'aide de User Secrets.

Les User Secrets sont rattachés au projet de démarrage `BudgetManager.Web`, qui constitue le composition root de l'application et fournit la configuration à la couche Infrastructure.

Depuis la racine de la solution, initialiser User Secrets une seule fois sur le projet Web :

```powershell
dotnet user-secrets init --project .\src\BudgetManager.Web\BudgetManager.Web.csproj
```

Ajouter ou remplacer le mot de passe d'un utilisateur seedé :

```powershell
dotnet user-secrets set "Seed:Users:Users:BootstrapAdmin:Password" "<password>" --project .\src\BudgetManager.Web\BudgetManager.Web.csproj
```

Pour une autre clé logique d'utilisateur, remplacer `BootstrapAdmin` par la clé utilisée dans la configuration :

```powershell
dotnet user-secrets set "Seed:Users:Users:<UserCode>:Password" "<password>" --project .\src\BudgetManager.Web\BudgetManager.Web.csproj
```

Vérifier les secrets actuellement configurés :

```powershell
dotnet user-secrets list --project .\src\BudgetManager.Web\BudgetManager.Web.csproj
```

Les séparateurs `:` de la clé de configuration ne doivent pas être échappés dans PowerShell lorsqu'ils se trouvent dans une chaîne entre guillemets.

En production, le mot de passe doit être fourni par une source de configuration sécurisée, par exemple une variable d'environnement ou un gestionnaire de secrets.

Avec une variable d'environnement :

```text
Seed__Users__Users__BootstrapAdmin__Password=<password>
```

ASP.NET Core convertit les doubles underscores (`__`) en séparateurs de configuration (`:`).

Une fois le compte de bootstrap créé et s'il n'est plus nécessaire de maintenir ce seeding, celui-ci peut être désactivé dans la configuration.

## Ordre d'exécution

L'ordre actuel d'initialisation est :

```text
Migrations EF Core
        ↓
RoleSeeder
        ↓
UsersSeeder
```

Cet ordre garantit que le schéma de base de données est à jour et que les rôles nécessaires existent avant la création des utilisateurs.


---

# Injection de dépendances

L'infrastructure expose une unique méthode d'enregistrement :

```csharp
services.AddInfrastructure(...)
```

Aucun autre projet ne connaît les implémentations concrètes.
---

# E-mails

L'infrastructure fournit l'implémentation technique du système d'envoi d'e-mails.

Elle utilise :

- MailKit pour la connexion et l'envoi SMTP ;
- MimeKit pour la construction des messages MIME ;
- des ressources embarquées pour les templates HTML et texte ;
- l'injection de dépendances pour exposer les implémentations aux autres couches.

L'organisation actuelle est :

```text
Email
├── Smtp
│   ├── SmtpEmailSender
│   └── SmtpOptions
├── Templates
│   ├── AccountActivation.fr-FR.html / .txt
│   ├── AccountActivation.en-US.html / .txt
│   ├── EmailChangeConfirmation.fr-FR.html / .txt
│   ├── EmailChangeConfirmation.en-US.html / .txt
│   ├── PasswordReset.fr-FR.html / .txt
│   └── PasswordReset.en-US.html / .txt
├── EmailTemplateRenderer
├── HtmlEncoder
└── TemplatedEmailSender
```

## SMTP

`SmtpEmailSender` implémente `BudgetManager.Application.Abstractions.Email.IEmailSender`.

Il prend en charge :

- les destinataires `To`, `Cc` et `Bcc` ;
- les corps texte et HTML ;
- les pièces jointes ;
- les images inline avec Content-ID ;
- les connexions SMTP sécurisées via `SecureSocketOptions` ;
- l'authentification SMTP facultative ;
- un timeout configurable ;
- la propagation du `CancellationToken` ;
- la journalisation des succès et erreurs sans journaliser les credentials SMTP.

La déconnexion SMTP est effectuée dans un bloc de nettoyage indépendant du token de la requête afin qu'une annulation ne masque pas une erreur d'envoi.

## Configuration SMTP

La section attendue est `Smtp`.

Exemple de configuration non sensible :

```json
{
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "SecureOptions": "StartTls",
    "Timeout": 10000,
    "FromAddress": "noreply@budgetmanager.net",
    "FromName": "Budget Manager"
  }
}
```

L'authentification est facultative. `UserName` et `Password` doivent être soit tous les deux renseignés, soit tous les deux absents.

Lorsque des credentials sont nécessaires, ils ne doivent pas être versionnés. En développement, ils sont stockés dans les User Secrets du projet `BudgetManager.Web` :

```powershell
dotnet user-secrets set "Smtp:UserName" "<username>" --project .\src\BudgetManager.Web\BudgetManager.Web.csproj
dotnet user-secrets set "Smtp:Password" "<password>" --project .\src\BudgetManager.Web\BudgetManager.Web.csproj
```

Vérification :

```powershell
dotnet user-secrets list --project .\src\BudgetManager.Web\BudgetManager.Web.csproj
```

Les options SMTP sont validées au démarrage. L'application refuse donc de démarrer si le serveur, le port, le mode de sécurité, le timeout, l'expéditeur ou la paire utilisateur/mot de passe sont incohérents.

## Templates

`EmailTemplateRenderer` charge les templates embarqués dans `BudgetManager.Infrastructure.dll`.

Pour un même nom de template, les deux représentations suivantes peuvent exister :

```text
<TemplateName>.html
<TemplateName>.txt
```

Au moins l'une des deux doit être présente.

Les placeholders utilisent la syntaxe :

```text
{{PropertyName}}
```

Les valeurs insérées dans un template HTML sont encodées avant insertion. Les valeurs insérées dans un template texte sont conservées telles quelles.

Les templates actuellement présents sont :

- `AccountActivation` ;
- `EmailChangeConfirmation` ;
- `PasswordReset`.

## Identity

Les pages Identity personnalisées utilisent directement `ITemplatedEmailSender` pour les e-mails applicatifs. Elles réutilisent ainsi la même infrastructure SMTP que le reste de l'application sans passer par `Microsoft.AspNetCore.Identity.UI.Services.IEmailSender`.

Les flux actuellement raccordés à cette infrastructure utilisent les templates localisés de Budget Manager pour l'activation d'un compte, la réinitialisation du mot de passe et la confirmation d'un changement d'adresse e-mail. Les sujets sont eux aussi localisés côté Web.

## Injection de dépendances

Les services d'e-mail sont enregistrés par `AddInfrastructure(...)` :

```text
IEmailTemplateRenderer
    → EmailTemplateRenderer

IEmailSender
    → SmtpEmailSender

ITemplatedEmailSender
    → TemplatedEmailSender
```

---

# Tokens Identity

Budget Manager utilise trois providers de tokens Identity distincts afin que chaque flux dispose de sa propre durée de validité :

```text
AccountActivation → confirmation de l'adresse lors de l'activation du compte
EmailChange       → confirmation d'un changement d'adresse e-mail
PasswordReset     → réinitialisation d'un mot de passe oublié
```

Les durées sont configurées dans la section `IdentityTokens` :

```json
{
  "IdentityTokens": {
    "AccountActivationLifetime": "7.00:00:00",
    "EmailChangeLifetime": "1.00:00:00",
    "PasswordResetLifetime": "01:00:00"
  }
}
```

La configuration courante correspond respectivement à 7 jours, 24 heures et 1 heure.

Les trois valeurs doivent être strictement positives. Les options sont validées au démarrage.

Le provider de confirmation d'adresse est volontairement réservé à l'activation du compte. Le changement d'adresse e-mail et le mot de passe oublié utilisent leurs providers dédiés et ne réduisent donc pas la durée de validité du lien d'activation.

`ActivationUrlGenerator` implémente `IActivationUrlGenerator` pour le flux d'activation : il demande à ASP.NET Core Identity un token de confirmation d'adresse, l'encode en Base64 URL-safe, l'ajoute aux valeurs de route sous la clé `activationCode`, puis délègue la construction de l'URL absolue à `IApplicationUrlBuilder`. Une valeur `activationCode` éventuellement fournie par l'appelant est remplacée par le token généré.

---

# Dates et fuseau horaire

Les dates métier et techniques persistées par l'application restent en UTC. `TimeProvider.GetUtcNow()` demeure la source de temps pour les écritures.

Le fuseau horaire utilisé pour la présentation est configuré dans la section `Localization` :

```json
{
  "Localization": {
    "TimeZone": "Europe/Paris"
  }
}
```

L'identifiant est résolu avec `TimeZoneInfo.FindSystemTimeZoneById(...)` et validé au démarrage.

`IDateTimeLocalizer` / `DateTimeLocalizer` centralise la conversion d'un `DateTimeOffset` UTC vers le fuseau de l'instance. La conversion n'est effectuée qu'au moment de l'affichage ou de la construction d'un contenu destiné à l'utilisateur ; la valeur persistée n'est pas transformée.

Cette conversion s'appuie sur les règles du fuseau et gère automatiquement les changements heure d'été / heure d'hiver.

