# Contributing to TextEdit

Thanks for your interest in contributing!

## Getting Started

1. Fork the repository and create your branch from `main`
2. Ensure you have the .NET 8 SDK installed
3. Restore dependencies: `dotnet restore`
4. Build the solution: `dotnet build`
5. Run tests: `dotnet test`

## Development Guidelines

- Follow .NET naming conventions and keep classes focused (SRP)
- Avoid over-engineering (YAGNI, KISS)
- Prefer dependency injection and abstractions only where they add testability
- Keep UI components small and use `ShouldRender` where appropriate
- Write tests for new functionality (unit/integration as appropriate)

## Commit & PR Process

- Use clear, descriptive commit messages
- Reference issues in commits and PRs when applicable
- Ensure CI build is green and tests pass
- Update README or project docs if behavior changes

## Code Style

- C# 12, .NET 8
- Nullable reference types enabled
- One class per file
- XML comments for public APIs where helpful

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
