# Recipe Module

The **SlideGenerator.Recipe** module persists, queries, and packages user-defined slide-generation recipes.

## Responsibility
- CRUD operations on stored recipes (SQLite).
- Exporting a recipe and its referenced files as a portable `.recipe` zip package.
- Importing a `.recipe` package, validating its manifest, and restoring referenced files into the workspace.

## Key Abstractions
- **`IRecipeRepository`**: CRUD over `RecipeEntry` rows stored in the local SQLite database.
- **`IRecipeFileManifestExtractor`**: Determines which file paths a recipe references so the exporter can bundle them. The default `NullRecipeFileManifestExtractor` returns an empty manifest; richer extractors can be plugged in via DI.
- **`ZipImportRules`** (in `Domain/Rules/`): Validation rules applied to incoming `.recipe` zip packages before they are accepted.

## Storage
- **Format**: SQLite, accessed through `SqliteConnectionFactory` (from `SlideGenerator.Utilities`).
- **Location**: Under `%LOCALAPPDATA%/SlideGenerator/` per `NameAndPaths`.

## IPC Surface
The Recipe module is exposed to the frontend through the Ipc handler `RecipeHandler` with the following methods:
`recipe.list`, `recipe.query`, `recipe.add`, `recipe.update`, `recipe.delete`, `recipe.export`, `recipe.import`.

The Generator does not consume `RecipeEntry` directly — it works against the canonical `RecipeSummary` produced by `SlideGenerator.Summarization`.
