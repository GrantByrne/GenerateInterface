# Claude Development Rules

## Development Workflow
When completing any item on the todo list, follow this strict workflow:

1. **Make the change** - Implement the required functionality
2. **Commit the change** - Create a git commit with a descriptive message
3. **Build in release mode** - Run `dotnet build --configuration Release` from the src/ directory
4. **Fix any build issues** - Address all errors and warnings (zero tolerance for warnings)
5. **Commit fixes if needed** - If build fixes were required, commit them separately

## Code Quality Standards
- **Zero build warnings** - All warnings must be resolved before completing a task
- **Release builds must pass** - Always build in Release configuration to catch optimization issues
- **Clean commits** - Each logical change should be in its own commit
- **Descriptive commit messages** - Follow conventional commit format when possible
- **File-scoped namespaces** - All C# files should use file-scoped namespaces (namespace MyNamespace;) instead of block-scoped namespaces
- **Remove auto-generated files** - When creating new projects, always remove auto-generated Class1.cs files
- **No GitHub release assets** - GitHub workflows should not publish release assets, only create releases and publish to NuGet

## Project Structure
- All source code in `src/` directory
- Solution file located at `src/GenerateInterface.sln`
- Each project in its own subdirectory under `src/`
- MIT License at root level
- README.md at root level

## Build Commands
- Build: `dotnet build --configuration Release` (from src/ directory)
- Test: `dotnet test --configuration Release` (from src/ directory)
- Pack: `dotnet pack --configuration Release` (from src/ directory)