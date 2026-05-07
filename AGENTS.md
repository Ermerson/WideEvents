# AGENTS.md

## Project Overview

WideEvents is a high-performance structured logging and observability framework for .NET, focused on producing "Wide Events" and "Canonical Log Lines." Instead of generating fragmented logs across a request lifecycle, WideEvents captures a single rich, structured event with operational, technical, and business context. This simplifies debugging, querying, and observing production systems.

Key features include:
- Structured wide events
- OpenTelemetry integration
- Automatic trace/span correlation
- PII masking and sanitization
- Async exporter pipeline
- Source-generated schemas
- Minimal allocations
- Extensible exporters (e.g., OTLP, Kafka)

The framework integrates with ILogger, Serilog, and various observability pipelines, emphasizing performance and data governance for modern distributed systems.

## Build and Test Commands

This project uses .NET SDK for building and testing. Ensure you have the .NET SDK installed (version 6.0 or later recommended).

### Build Commands
- **Build the project**: `dotnet build`
- **Build in release mode**: `dotnet build --configuration Release`
- **Clean build artifacts**: `dotnet clean`

### Test Commands
- **Run all tests**: `dotnet test`
- **Run tests with coverage**: `dotnet test --collect:"XPlat Code Coverage"`
- **Run specific test project**: `dotnet test WideEvents.Tests/WideEvents.Tests.csproj`

### Additional Commands
- **Restore packages**: `dotnet restore`
- **Publish for deployment**: `dotnet publish --configuration Release`

## Code Style Guidelines

Follow standard .NET coding conventions and best practices. Use EditorConfig for consistent formatting.

### Key Guidelines
- Use PascalCase for class names, method names, and properties.
- Use camelCase for local variables and parameters.
- Use meaningful names; avoid abbreviations.
- Keep methods short and focused (Single Responsibility Principle).
- Use async/await for asynchronous operations.
- Prefer immutable types where possible.
- Document public APIs with XML comments.

### EditorConfig
An `.editorconfig` file is included in the repository. Ensure your IDE respects it for automatic formatting.

### Linting
Use Roslyn analyzers for code quality. Run `dotnet build` to check for warnings and errors.

## Testing Instructions

Tests are written using xUnit.net. Ensure all tests pass before submitting changes.

### Running Tests
1. Restore dependencies: `dotnet restore`
2. Build the solution: `dotnet build`
3. Run tests: `dotnet test`

### Writing Tests
- Place tests in corresponding test projects (e.g., `WideEvents.Core.Tests`).
- Use descriptive test names.
- Cover happy paths, edge cases, and error scenarios.
- Mock external dependencies using Moq or similar libraries.

### Test Coverage
Aim for high test coverage. Use tools like Coverlet for coverage reports.

## Security Considerations

WideEvents handles sensitive data, so security is paramount.

### PII Masking and Sanitization
- Implement sanitization hooks to mask PII (e.g., emails, credit card numbers).
- Ensure compliance with regulations like GDPR and PCI DSS.
- Centralize masking policies before exporting to external systems.

### Best Practices
- Avoid logging sensitive information in plain text.
- Use secure exporters (e.g., encrypted Kafka topics).
- Regularly audit dependencies for vulnerabilities using `dotnet list package --vulnerable`.
- Implement rate limiting and sampling to prevent data leaks.

### Reporting Security Issues
Report security vulnerabilities privately via email to [security@wideevents.dev] (placeholder; update as needed).

## Pull Request and Commit Message Guidelines

### Pull Requests (PRs)
- Create a PR for each feature, bug fix, or improvement.
- Provide a clear description of changes, including motivation and impact.
- Ensure all tests pass and code style checks succeed.
- Request reviews from at least one maintainer.
- Keep PRs small and focused; split large changes into multiple PRs.

### Commit Messages (Conventional Commits)
Use Conventional Commits format for clear, structured commit messages.

Format:
``` plain
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

#### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks (e.g., build scripts)

#### Examples
- `feat(core): add OpenTelemetry integration`
- `fix(aspnetcore): resolve middleware null reference`
- `docs: update README with installation steps`
- `test: add unit tests for event builder`

#### Rules
- Use imperative mood (e.g., "add" not "added").
- Keep the description concise (<50 characters).
- Use scope to indicate affected component (e.g., `core`, `aspnetcore`).
- For breaking changes, add `!` after type and include `BREAKING CHANGE:` in body.
- Reference issues with `#123`.
