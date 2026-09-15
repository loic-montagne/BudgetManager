# Web

## Objectif

Le projet `BudgetManager.Web` constitue la couche de présentation et le composition root de l'application.

Il utilise ASP.NET Core MVC et Razor Pages.

Il référence :

- `BudgetManager.Application` pour les cas d'utilisation et abstractions ;
- `BudgetManager.Infrastructure` pour l'enregistrement des implémentations techniques.

La couche Web ne doit pas contenir de règles métier.

---

# Authentification

L'authentification repose sur ASP.NET Core Identity.

Une fallback policy exige un utilisateur authentifié pour les endpoints qui ne déclarent pas explicitement une autre politique.

Les pages qui doivent être accessibles avant connexion utilisent `[AllowAnonymous]`.

Le pipeline HTTP exécute :

```text
UseRouting
    ↓
UseAuthentication
    ↓
UseAuthorization
```

Le cookie Identity redirige vers :

```text
/Identity/Account/Login
```

en cas d'accès non authentifié.

---

# Interface Identity scaffoldée

Les pages Identity nécessaires à BudgetManager sont scaffoldées afin de pouvoir utiliser le layout et le style de l'application tout en conservant les mécanismes ASP.NET Core Identity.

Les fonctionnalités actuellement présentes couvrent notamment :

- connexion ;
- activation du compte et confirmation de l'adresse e-mail ;
- renvoi de l'e-mail d'activation ;
- mot de passe oublié ;
- réinitialisation du mot de passe ;
- connexion avec double authentification ;
- connexion avec code de récupération ;
- profil utilisateur ;
- changement d'adresse e-mail ;
- changement de mot de passe ;
- activation et gestion d'un authenticator ;
- génération et affichage des codes de récupération ;
- désactivation et réinitialisation de la double authentification.

L'inscription libre n'est pas exposée : la création des utilisateurs est gérée par les cas d'utilisation d'administration de l'Application.

---

# Layout authentifié et anonyme

Les pages Identity utilisent le layout principal de BudgetManager.

Le layout adapte son rendu à l'état d'authentification :

- utilisateur authentifié : header, sidebar et menu applicatif ;
- utilisateur anonyme : contenu seul, sans navigation métier.

Cette séparation permet aux pages `Login`, `ForgotPassword`, `ResetPassword`, `LoginWith2fa`, etc. de conserver le thème visuel de BudgetManager sans afficher l'interface applicative réservée aux utilisateurs connectés.

Le thème clair/sombre est partagé entre l'écran de connexion et l'application grâce à la même préférence enregistrée côté navigateur.

---

# Profil et sécurité du compte

Les pages situées sous :

```text
/Identity/Account/Manage
```

permettent à l'utilisateur connecté de gérer son profil et les éléments de sécurité pris en charge par Identity.

Les règles métier d'administration des utilisateurs restent dans l'Application. Les mécanismes techniques de mot de passe et de double authentification restent confiés à ASP.NET Core Identity.

---

# User Secrets

`BudgetManager.Web` porte le `UserSecretsId` car il constitue le projet de démarrage et le composition root.

Les secrets de développement utilisés par Infrastructure sont donc associés à ce projet, notamment :

- mots de passe des utilisateurs seedés ;
- credentials SMTP.

Le `UserSecretsId` est une configuration de projet et peut être versionné. Les valeurs des secrets ne doivent jamais être ajoutées au dépôt.

---

# Génération des URLs publiques et reverse proxy

Les liens absolus envoyés par les e-mails Identity (activation du compte, réinitialisation du mot de passe et confirmation de changement d'adresse e-mail) sont générés par `IApplicationUrlBuilder`.

Cette abstraction appartient à la couche Web et évite de construire les URLs à partir du `Host` de la requête HTTP.

En environnement `Development`, `DevApplicationUrlBuilder` conserve le comportement local historique et génère les URLs à partir de la requête courante.

Dans les autres environnements, `ApplicationUrlBuilder` utilise `Application:PublicUrl`, validée au démarrage comme URI absolue HTTPS. La génération est également robuste si l'application est exposée sous un path base.

En production derrière un reverse proxy (par exemple ISPConfig sur Debian), le pipeline utilise les forwarded headers `X-Forwarded-For` et `X-Forwarded-Proto`. Seuls les proxies locaux de confiance sont déclarés dans `ForwardedHeadersOptions`, afin que l'adresse cliente utilisée notamment par le rate limiting soit correctement restaurée sans faire confiance à des en-têtes arbitraires.


---

# Activation du compte

L'inscription libre n'est pas disponible. Un compte est d'abord créé par l'administration avec un mot de passe temporaire généré côté Application.

Le lien envoyé par l'e-mail d'activation ouvre `/Identity/Account/ActivateAccount`. L'utilisateur ressaisit l'adresse e-mail enregistrée initialement par l'administrateur et choisit son propre mot de passe.

La page transmet ensuite `ActivateUserCommand` à l'Application. Un compte dont l'adresse e-mail n'est pas confirmée ne peut pas se connecter, car Identity est configuré avec `RequireConfirmedAccount` et `RequireConfirmedEmail`.

Le renvoi public de l'e-mail d'activation ne concerne que les comptes encore en attente. Le changement d'adresse e-mail d'un utilisateur déjà actif suit un flux distinct.

---

# Changement d'adresse e-mail

Le changement d'adresse e-mail est séparé en deux cas d'utilisation.

`ChangeEmailUserCommand` est réservé à l'utilisateur authentifié. Il vérifie que l'utilisateur cible est l'utilisateur courant, que l'ancienne adresse correspond bien à l'adresse actuelle et que la nouvelle adresse est valide et disponible. Il génère ensuite le token Identity utilisé pour la confirmation.

`ConfirmEmailChangeUserCommand` est utilisé par la page de confirmation accessible via le lien envoyé par e-mail. Il valide l'identifiant utilisateur, la nouvelle adresse et le token Identity, puis confirme le changement de manière atomique côté Infrastructure afin de conserver l'invariant entre l'adresse e-mail et le nom d'utilisateur.


---

# Tests de la couche Web

Le projet `BudgetManager.Web.Tests` vérifie les comportements C# spécifiques à la présentation : contrôleurs MVC, adaptation DataTables, extensions, localisation, utilisateur courant, TagHelpers et éléments testables des PageModels Identity.

Les règles métier restent testées dans Domain/Application et les mécanismes techniques Identity/persistence dans Infrastructure. Les vues Razor, feuilles de style et scripts JavaScript ne sont pas considérés comme couverts par les tests xUnit Web ; un éventuel niveau navigateur/end-to-end sera ajouté uniquement pour des scénarios qui le justifient.
