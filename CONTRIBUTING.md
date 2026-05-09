# Contributing

## Prerequisites

.NET SDK 10.0 or later. That's it.

## Setup

```bash
git clone https://github.com/santos-404/GenDto
cd GenDto
dotnet restore
make build
make test
```

Tests run the generator in-process against synthetic source snippets, so there are no external services to spin up.

## Development commands

| Command | What it does |
|---|---|
| `make build` | Compile the solution |
| `make test` | Run the full test suite |
| `make pack` | Build a `.nupkg` into `./artifacts` for local testing |
| `make release` | Cut a release (see [Making a release](#making-a-release)) |

### Testing the package locally

If you want to smoke-test the generator in a real project before opening a PR:

```bash
make pack
# then in your test project:
dotnet add package GenDto --source /path/to/GenDto/artifacts
```

## Project structure

Two source projects, one test project:

- `src/GenDto.Attributes` — the attributes consumers put on their classes. Never published on its own.
- `src/GenDto` — the generator itself. This is the NuGet package.
- `tests/GenDto.Tests` — xUnit tests.

If you add a new attribute: add the class to `GenDto.Attributes`, wire up the generator logic in `GenDto/DtoSourceGenerator.cs`, add tests.

## Submitting a change

For anything beyond a trivial fix, open an issue first so we can agree on the approach before you write code.

1. Fork the repo and create a branch from `main`
2. Make your changes and add tests if relevant
3. Run `make test` and make sure everything passes
4. Open a pull request against `main` with a clear description of what changed and why

Keep pull requests focused — one thing per PR. If you're fixing a bug, don't also refactor surrounding code.

## Code conventions

- No comments unless the WHY is non-obvious (the what is clear from the code)
- No error handling for things that can't happen — trust internal invariants
- No abstractions beyond what the current task needs

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
