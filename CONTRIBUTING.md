# Contributing

## Prerequisites

.NET SDK 10.0 or later. That's it.

## Setup

```bash
git clone https://github.com/santos-404/DtoGenerator
cd DtoGenerator
dotnet restore
dotnet build
dotnet test
```

Tests run the generator in-process against synthetic source snippets, so there are no external services to spin up.

## Project structure

Two source projects, one test project:

- `src/DtoGenerator.Attributes` — the attributes consumers put on their classes. Never published on its own.
- `src/DtoGenerator` — the generator itself. This is the NuGet package.
- `tests/DtoGenerator.Tests` — xUnit tests.

If you add a new attribute: add the class to `DtoGenerator.Attributes`, wire up the generator logic in `DtoGenerator/DtoSourceGenerator.cs`, add tests.

## Versioning

Versions follow [semantic versioning](https://semver.org): `MAJOR.MINOR.PATCH`.

- **patch** — bug fix, no API change (`v1.0.0 → v1.0.1`)
- **minor** — new feature, backwards compatible (`v1.0.1 → v1.1.0`)
- **major** — breaking change (`v1.1.0 → v2.0.0`)

Version numbers come from git tags via [MinVer](https://github.com/adamralph/minver). You never edit a version number in a file — you just tag.

## Making a release

Releases must be made from `main` with a clean working tree.

```bash
make release              # patch bump  →  v1.0.0 becomes v1.0.1
make release BUMP=minor   # minor bump  →  v1.0.1 becomes v1.1.0
make release BUMP=major   # major bump  →  v1.1.0 becomes v2.0.0
```

This creates a git tag and pushes it. GitHub Actions takes it from there: builds, runs tests, packs, creates a GitHub Release, and (for v1.0.0 and above) publishes to NuGet. Nothing is published if tests fail.

## Branch strategy

Until v1.0.0, everything goes directly to `main`. After v1.0.0, feature work goes on a separate branch and merges to `main` via pull request before releasing.
