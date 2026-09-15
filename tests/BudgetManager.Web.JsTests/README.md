# BudgetManager.Web.JsTests

Tests unitaires du JavaScript applicatif de `BudgetManager.Web`.

## Prérequis

- Node.js
- npm

## Installation

```bat
cd tests\BudgetManager.Web.JsTests
npm ci
```

## Exécution

```bat
npm test
```

Mode interactif :

```bat
npm run test:watch
```

Avec couverture JavaScript :

```bat
npm run test:coverage
```

Les tests chargent les scripts de production depuis `src/BudgetManager.Web/wwwroot/js` au moyen de `import.meta.glob`, fourni par Vite. Ces fichiers se trouvent hors de la racine du projet de tests ; `coverage.allowExternal` est donc activé dans `vitest.config.mjs` pour que Vitest/V8 collecte leur couverture. Le motif `coverage.include` est résolu en chemin absolu normalisé depuis `vitest.config.mjs`, afin de correspondre sans ambiguïté aux fichiers exécutés. Les tests ne contiennent aucune copie du code de production.

## Intégration à la solution

Le dossier de tests est référencé dans `BudgetManager.sln` sous le dossier de
solution `tests`, afin que ses fichiers soient accessibles directement dans
Visual Studio.

`tools\test_solution.bat` exécute automatiquement ces tests avec couverture
après les tests .NET.

## GitHub Actions

Le workflow `.github/workflows/ci.yml` installe Node.js LTS, exécute `npm ci`
puis `npm run test:coverage`. Le rapport de couverture est publié comme artefact
`javascript-coverage`.


## Exécution depuis la racine du dépôt

Pour exécuter uniquement cette suite, avec sa couverture :

```bat
tools\test_solution.bat --js
```

Cela évite notamment d'exécuter `BudgetManager.Infrastructure.Tests`, qui est
nettement plus long que la suite JavaScript.

Par défaut, `tools\test_solution.bat --js` ouvre `coverage\index.html` à la fin d'une exécution réussie. Ajoutez `--no-open` pour empêcher cette ouverture automatique.


> **Configuration Vitest 5 :** les options de couverture sont placées dans `test.coverage` dans `vitest.config.mjs`. La configuration utilise le format ESM explicite (`.mjs`) et place les options de couverture dans `test.coverage`.


La couverture JavaScript est validée directement par Vitest avec des seuils minimaux de `0.01` pour les instructions, branches, fonctions et lignes. Le but n'est pas d'imposer encore un objectif de couverture, mais de faire échouer l'exécution si aucun code de production n'est réellement couvert.

## Périmètre couvert

La suite couvre désormais les 12 scripts applicatifs de `wwwroot/js`, y compris `sidebar.js`, `site.js`, `entity-manager.js` et les managers Bank, BudgetCategory, Account et User. Les tests chargent jQuery 3.7.1, la même version que celle déclarée dans `src/BudgetManager.Web/libman.json`. Les bibliothèques tierces qui ne font pas l'objet des tests (DataTables, Bootstrap Multiselect, modales Bootstrap) restent simulées de façon ciblée.

Les tests couvrent également les branches de comportement des managers et de la sidebar (erreurs, validations, confirmations, préférences système, raccourcis clavier), les changements de barres de défilement et le positionnement des tableaux à en-tête fixe. Les fonctions utilitaires globales de `site.js` (`textToHtml`, `validateForm` et `applyMultiSelectWidth`) sont exécutées depuis le source de production chargé en mode `?raw`, afin de tester le code exact sans le recopier dans les tests.
