# Domaine

## Objectif

Le projet `BudgetManager.Domain` contient exclusivement les règles métier.

Il ne dépend d'aucune technologie et ne connaît ni Entity Framework Core, ni MediatR, ni ASP.NET Core.

Son rôle est de garantir les invariants du métier.

---

# Principes

Le domaine suit plusieurs principes issus du Domain-Driven Design.

- Entités
- Objets valeur
- Agrégats
- Racines d'agrégat
- Exceptions métier

Toutes les modifications passent obligatoirement par les méthodes métier.

Aucune propriété métier ne peut être modifiée directement depuis l'extérieur.

---

# Agrégats

Les principales racines d’agrégat manipulées par l’Application sont notamment :

- Budget ;
- Bank ;
- Account ;
- BudgetCategory.

`Budget` constitue l’agrégat central pour la gestion des accès, des catégories associées et des transactions.

Il est responsable notamment de :

- la gestion des accès ;
- des catégories ;
- des transactions ;
- du verrouillage.

Toutes les modifications de ces éléments passent par le Budget.

---

# Autorisations

Les autorisations métier sont également contrôlées par le domaine.

Même si l'application valide les permissions avant d'exécuter un cas d'utilisation, le domaine effectue toujours ses propres vérifications.

Cela garantit qu'aucun appel incorrect ne puisse contourner les règles métier.

---

# Validation

Le domaine valide les préconditions nécessaires à la création d’un état valide :

- identifiants non vides ;
- chaînes obligatoires ;
- formats des objets valeur ;
- valeurs numériques autorisées ;
- valeurs d’énumération reconnues.

L’Application valide également les données d’entrée afin de retourner des erreurs détaillées avant l’exécution du cas d’utilisation.

Les validations applicatives ne remplacent jamais les invariants garantis par le domaine.

---

# Exceptions

Les exceptions du domaine représentent une violation des règles métier.

Elles ne doivent jamais être utilisées pour représenter des erreurs techniques.

L'interface est responsable de convertir ces exceptions en réponses adaptées au protocole utilisé.

---

# Objets valeur

Les objets valeur sont immuables.

Ils garantissent eux-mêmes leur validité.

Exemples :

- IBAN
- BIC
- UDecimal