# Couverture des tests

Ce document décrit la couverture fonctionnelle actuelle de la solution.

Il ne constitue pas une mesure de couverture de code, mais une vue d'ensemble des comportements vérifiés par les tests.

---

# Couche Domain

Les tests du domaine couvrent les principaux comportements métier.

Ils vérifient notamment :

- les méthodes d'usine ;
- les entités ;
- les objets valeur ;
- les énumérations ;
- les permissions ;
- les méthodes métier ;
- les invariants des agrégats ;
- les règles d'autorisation ;
- les scénarios d'erreur.

L'objectif est de détecter toute régression affectant les règles métier du domaine.

---

# Couche Application

Les tests de l'application couvrent les principaux composants de la couche.

## Handlers

Les handlers de commandes et de requêtes sont testés.

Les tests vérifient notamment :

- le chargement des agrégats ;
- les appels aux repositories ;
- les appels aux Query Services ;
- les interactions avec les Contexts ;
- les mutations des agrégats ;
- la propagation du `CancellationToken` ;
- les scénarios de succès ;
- les principaux scénarios d'erreur.

---

## Validators

Les validators sont testés afin de vérifier :

- les validations syntaxiques ;
- les validations métier ;
- les règles dépendantes (`DependentRules`) ;
- les interactions avec les Contexts ;
- les scénarios valides.

---

## Behaviors

Les behaviors MediatR sont testés.

Les tests couvrent notamment :

- l'autorisation ;
- la validation ;
- l'ordre d'exécution des validators ;
- l'agrégation des erreurs de validation ;
- la propagation du `CancellationToken`.

---

## Contexts

Les Contexts sont testés afin de vérifier :

- le chargement des agrégats ;
- le cache scoped ;
- les méthodes de vérification ;
- les comportements en présence de ressources inexistantes ;
- les statuts métier (édition, verrouillage, déverrouillage...).

---

## Architecture

Les tests d'architecture vérifient notamment :

- la présence d'un handler pour chaque requête ;
- l'enregistrement des validators ;
- le respect des conventions d'architecture.

---


# Couche Infrastructure

Les tests de l'infrastructure couvrent les principaux comportements techniques et les interactions avec SQL Server.

## Modèle et migrations

Les tests vérifient notamment :

- l'application de la migration initiale ;
- les complex types `Iban` et `Bic` ;
- la précision de `UDecimal` ;
- les tokens de concurrence `RowVersion` ;
- les comportements de suppression des relations ;
- l'index filtré garantissant un seul propriétaire par budget ;
- les index uniques SQL Server ajoutés manuellement pour l'IBAN et le BIC.

## Persistence et concurrence

Les tests vérifient :

- l'audit à la création et à la modification ;
- le comportement en l'absence d'utilisateur courant ;
- la traduction de `DbUpdateException` en `UpdateException` ;
- la traduction de `DbUpdateConcurrencyException` en `ConcurrencyException` ;
- la détection des écritures concurrentes.

## Repositories

Les tests couvrent :

- la création, la modification et la suppression ;
- le chargement tracked ;
- les contrôles d'unicité ;
- la collation insensible à la casse et aux accents ;
- les contrôles d'utilisation ;
- le chargement tracked des repositories `Account`, `Bank`, `BudgetCategory`, `Budget` et `Transaction` ;
- le chargement des budgets associés lors du chargement tracked d'une catégorie ;
- le chargement complet de l'agrégat `Budget` ;
- la modification forcée de la racine du budget lorsqu'un enfant de l'agrégat change ;
- la suppression complète d'un agrégat `Budget` avec cascade sur ses accès et transactions, sans suppression des entités externes référencées ;
- la protection contre la suppression d'un utilisateur encore référencé par un `BudgetAccess`.

## Query Services

Les Query Services sont exécutés contre SQL Server afin de vérifier leur traduction réelle.

Les tests couvrent notamment :

- les projections ;
- les complex types ;
- les permissions bitwise ;
- les montants signés ;
- les agrégations de budget ;
- les recherches `LIKE` et l'échappement de leurs caractères spéciaux ;
- les filtres ;
- la pagination ;
- les tris ;
- les accès utilisateur et rôles.

## Initialisation et seeding

Les tests vérifient :

- l'ordre d'exécution des seeders ;
- la création des rôles ;
- la création de plusieurs utilisateurs configurés ;
- l'idempotence du seeding ;
- l'attribution d'un rôle à un utilisateur existant ;
- la désactivation globale ou individuelle du seeding ;
- la validation de la configuration.

---

# Couche Web

Les tests Web sécurisent les comportements C# de la couche de présentation :

- binding du protocole DataTables historique (`sEcho`, recherche, pagination et tris multiples) ;
- structure de réponse DataTables historique et mapping de critères sur les endpoints couverts ;
- extensions de présentation et cookie de culture ;
- utilisateur courant et claims HTTP ;
- modèles de formulaire et métadonnées de binding significatives ;
- localisation des erreurs métier ;
- TagHelper des champs obligatoires ;
- résultats et gardes des contrôleurs MVC ;
- conventions de validation et navigation des pages Identity.

Cette couverture vise les responsabilités de présentation. Elle ne duplique pas les règles métier de l'Application et ne prétend pas valider le rendu navigateur des vues Razor, CSS ou JavaScript.

# Limites

Les tests Web C# ne remplacent pas de futurs tests de bout en bout exécutés dans un navigateur.

Les tests Infrastructure couvrent la persistence et les interactions avec SQL Server, mais ne valident pas encore le comportement HTTP de l'application.

---

# Évolution

La couverture des tests évolue avec le projet.

Toute nouvelle fonctionnalité doit être accompagnée des tests nécessaires afin de limiter le risque de régression.

## Renforcement de la couverture de régression

La couverture inclut également des scénarios ciblés sur les angles morts identifiés lors des revues de code :

- chaque `ErrorCode` utilisé par un validator Application est référencé par au moins un test ;
- les préconditions métier de création et modification des transactions sont testées : existence et état des comptes, association des catégories, unicité du nom et compte de transfert ;
- les repositories tracked sont testés lorsque l'entité n'existe pas ;
- les clauses `excludingId` des contrôles d'unicité sont testées sur les principales entités ;
- un compte utilisé uniquement comme compte cible d'un virement est considéré comme utilisé ;
- le transfert de propriété d'un budget est vérifié contre SQL Server pour un membre existant et un nouvel utilisateur, avec garantie d'un propriétaire unique ;
- la persistance dédiée `TransferOwnershipAsync` est testée séparément de `UpdateAsync`, y compris pour un agrégat détaché, un ancien propriétaire incorrect et un échec de sauvegarde ;
- le rollback transactionnel d'un transfert de propriété est vérifié afin que l'ancien propriétaire reste propriétaire si la seconde étape de persistance échoue ;
- la mise à jour générique d'un budget est testée indépendamment du transfert de propriété afin d'éviter qu'une logique spécifique au changement de propriétaire contamine les autres mutations ;
- après transfert vers un membre existant, les permissions implicites du nouveau propriétaire et les permissions complètes conservées par l'ancien propriétaire sont vérifiées après rechargement depuis SQL Server ;
- les comportements `Restrict` et `Cascade` sont vérifiés avec des entités dépendantes non suivies afin de tester les contraintes réellement appliquées par SQL Server, et pas uniquement le `ChangeTracker` ;
- une recherche constituée uniquement d'espaces est vérifiée comme équivalente à l'absence de recherche ;
- la lecture complète d'un utilisateur sans rôle est explicitement couverte.

- les règles conditionnelles des validators sont testées sur les combinaisons croisées d'identifiants invalides afin de vérifier qu'aucune exception ne s'échappe de FluentValidation ;
- tous les validators de recherche rejettent explicitement les champs et directions de tri inconnus ;
- les branches propres à la modification d'une transaction (nom requis, montant, méthode, compte de transfert et appartenance au budget) sont couvertes individuellement ;
- les valeurs littérales des principaux `ErrorCodes` contractuels ainsi que les codes issus des validateurs IBAN/BIC sont verrouillés ;
- les gardes internes du domaine liées à la propriété d'un budget (`UserIsNotOwner`, promotion/démotion propriétaire) sont couvertes directement ;
- les collations des colonnes textuelles recherchées, y compris `Iban`, `Bic`, les utilisateurs et les rôles, sont vérifiées dans le modèle EF ;
- les index uniques sont exercés contre SQL Server avec des valeurs ne différant que par la casse ou les accents ;
- l'unicité SQL réelle des IBAN et BIC est vérifiée ;
- les quatre principales relations `Restrict` sont testées avec des dépendants non suivis afin de garantir que SQL Server refuse effectivement les suppressions ;
- les recherches SQL vérifient l'échappement littéral de `_`, `[` et `\`, la comparaison insensible à la casse et aux accents, ainsi que la sémantique multi-termes en `OR` ;
- les erreurs de création de rôle et d'attribution de rôle pendant le seeding sont vérifiées.

## Couverture de branches renforcée

La suite couvre également explicitement plusieurs branches auparavant couvertes
seulement de manière indirecte :

- états `AccountContext.IsOpenedAsync` / `IsClosedAsync` lorsque le compte est absent ;
- association et non-association explicites d'une catégorie à un budget ;
- correspondances indépendantes budget/catégorie dans `TransactionContext` ;
- chaque classe d'erreur des value objects `Iban` et `Bic`, avec vérification du code exact ;
- limites arithmétiques de `UDecimal`, y compris les dépassements de capacité `decimal` ;
- transfert de propriété stale/concurrent : un contexte obsolète ne peut pas écraser un transfert déjà persisté.

Ces scénarios ciblent des branches et transitions qui peuvent facilement être
perdues lors d'un refactoring malgré une couverture fonctionnelle globale élevée.


## Gestion des utilisateurs

Les nouveaux use cases de gestion des utilisateurs sont couverts à plusieurs niveaux.

La couche Application vérifie :

- la normalisation de l'adresse email et du numéro de téléphone ;
- les champs obligatoires et longueurs maximales du profil ;
- les numéros de téléphone optionnels, valides, invalides et trop longs ;
- la validation du nouveau mot de passe lors de l'activation par Identity et la remontée de plusieurs erreurs ;
- les rôles obligatoires, dupliqués sans tenir compte de la casse, inconnus fonctionnellement ou absents d'Identity ;
- l'existence de l'utilisateur pour les mises à jour et changements d'email ;
- l'interdiction de modifier le profil ou l'adresse e-mail d'un autre utilisateur ;
- l'interdiction de demander un changement d'adresse e-mail tant que l'adresse actuelle n'est pas confirmée ;
- l'interdiction de modifier ou supprimer la photo de profil d'un autre utilisateur ;
- la vérification de l'ancienne adresse avant génération d'un token de changement ;
- l'unicité de la nouvelle adresse en excluant l'utilisateur courant ;
- la validation du token et de l'adresse lors de la confirmation d'un changement d'e-mail ;
- la validation des préférences lors de la mise à jour du profil utilisateur ;
- l'interdiction de supprimer l'utilisateur courant ou un propriétaire de budget ;
- les rôles d'autorisation exigés par chaque commande ;
- le mapping exact des commandes vers `UserProfileData` et les appels à `IUserManager` ;
- la génération d'un mot de passe temporaire de 20 caractères lors de la création et le déclenchement du mail d'activation avec retour de sa date d'expiration ;
- la validation du token d'activation, de l'état non confirmé du compte et de la correspondance mot de passe / confirmation ;
- l'absence d'exception parasite lors de la validation d'une activation avec un identifiant vide ou inexistant : la politique de mot de passe n'est évaluée qu'après résolution effective de l'utilisateur ;
- la validation du nom de page d'activation, du sujet et d'une durée de token strictement positive ;
- l'envoi du mail d'activation avec une URL générée par `IActivationUrlGenerator`, une expiration calculée en UTC et une date affichée dans le fuseau configuré ;
- l'absence d'envoi et d'enregistrement des dates d'activation lorsque la génération de l'URL échoue ;
- l'absence d'enregistrement des dates d'activation lorsque l'envoi du mail échoue.

La couche Infrastructure vérifie avec SQL Server et le vrai ASP.NET Core Identity :

- l'existence des utilisateurs, emails et rôles ;
- la génération du lien d'activation avec conservation des valeurs de route et remplacement de tout `activationCode` fourni par l'appelant par un token Identity valide ;
- la détection d'un propriétaire de budget ;
- la création complète d'un utilisateur et l'affectation de plusieurs rôles ;
- le rollback de la création si le mot de passe ou l'affectation de rôle échoue ;
- la mise à jour du profil, du téléphone et des rôles ;
- la mise à jour autonome du profil sans modification de l'adresse e-mail ni des rôles ;
- la comparaison des rôles sans tenir compte de la casse ;
- le rollback d'une mise à jour si la gestion des rôles échoue après la sauvegarde du profil ;
- la génération d'un token de changement d'adresse sans modification immédiate de l'utilisateur ;
- la confirmation atomique du changement de `Email` et `UserName` ;
- le rollback de la confirmation si le token est invalide ou si la mise à jour du username échoue ;
- le rollback de la mise à jour du profil si sa persistance échoue après une modification du téléphone ;
- la suppression atomique de l'utilisateur et de ses accès non propriétaires ;
- le rollback de la suppression lorsqu'un accès propriétaire empêche la suppression Identity ;
- la politique réelle de mot de passe Identity ;
- la génération d'un mot de passe temporaire conforme à cette politique ;
- l'activation atomique du compte : confirmation de l'e-mail, remplacement du mot de passe temporaire et suppression des dates d'activation ;
- le rollback de l'activation si le token ou le nouveau mot de passe est invalide ;
- la séparation des providers `AccountActivation`, `EmailChange` et `PasswordReset` avec des durées respectives de 7 jours, 24 heures et 1 heure ;
- la validation de la configuration des durées de tokens et du fuseau horaire ;
- la conversion `Europe/Paris` en heure d'hiver et en heure d'été ;
- l'enregistrement des nouvelles abstractions Identity dans l'injection de dépendances.

## Bornes de longueur et caractères utilisateur

La suite couvre explicitement les bornes introduites par `MaximumLength` :

- chaque champ texte obligatoire accepte exactement sa longueur maximale ;
- `max + 1` reste rejeté avec le code d'erreur attendu ;
- les champs utilisateur `Email`, `FirstName`, `LastName` et `PhoneNumber` sont couverts aux bornes ;
- les emails contenant des caractères incompatibles avec le `UserName` Identity sont rejetés ;
- les caractères spéciaux explicitement autorisés (`.`, `_`, `-`, `+`) restent acceptés.

Les tests asynchrones utilisent `TestContext.Current.CancellationToken` pour rester cohérents avec le cycle de vie xUnit et permettre l'annulation correcte des tests.

