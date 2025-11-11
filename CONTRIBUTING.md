# Contributing to Nulis

Thank you for your interest in contributing to Nulis! This document provides guidelines and instructions for contributing.

## ?? Getting Started

### Prerequisites

Before you begin, ensure you have the following installed:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) v18 or later
- [Git](https://git-scm.com/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/) (recommended)

### Setting Up Your Development Environment

1. **Fork the repository** on GitHub

2. **Clone your fork**:
   ```bash
   git clone https://github.com/YOUR-USERNAME/Nulis.git
   cd Nulis
   ```

3. **Add upstream remote**:
   ```bash
   git remote add upstream https://github.com/armiaab/Nulis.git
   ```

4. **Install dependencies**:
   ```bash
   # Install .NET dependencies (automatic during build)
 dotnet restore Nulis\Nulis.csproj
   
   # Install Node.js dependencies for the editor
   cd Nulis\Editor
   npm install
   cd ..\..
   ```

5. **Build the project**:
   ```bash
   dotnet build Nulis\Nulis.csproj -c Debug
   ```

6. **Run the application**:
   ```bash
   dotnet run --project Nulis\Nulis.csproj
   ```

## ?? Branch Strategy

- **`master`** - Main development branch (develop here)
- **`production`** - Production-optimized builds only (don't develop here)

### Workflow

1. Always create feature branches from `master`
2. Never commit directly to `master` or `production`
3. Production branch is for optimized releases only

## ?? Making Changes

### 1. Create a Feature Branch

```bash
git checkout master
git pull upstream master
git checkout -b feature/your-feature-name
```

Use descriptive branch names:
- `feature/add-export-to-pdf`
- `fix/file-save-bug`
- `docs/update-readme`
- `refactor/simplify-command-palette`

### 2. Make Your Changes

#### Code Style Guidelines

**C# Code**:
- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and concise

**TypeScript/JavaScript (Editor)**:
- Use ES6+ features
- Follow existing code style
- Use TypeScript types when possible

**XAML**:
- Use proper indentation (2 spaces)
- Group related properties
- Use binding where appropriate

#### Logging

- Use the provided `LoggerService` for logging
- Log important operations (file I/O, errors, warnings)
- Use appropriate log levels:
  - `LogError` - Errors that need attention
  - `LogWarning` - Unexpected but handled situations
  - `LogInformation` - Important operations
  - `LogDebug` - Detailed debugging info
  - `LogTrace` - Very detailed diagnostics

Example:
```csharp
var logger = LoggerService.GetLogger<MainWindow>();
logger.LogInformation("File loaded successfully: {FileName}", fileName);
```

### 3. Testing Your Changes

Before submitting:

1. **Build without errors**:
 ```bash
   dotnet build Nulis\Nulis.csproj -c Debug
   ```

2. **Test the application**:
   - Run the app and test your changes
   - Test on both light and dark themes
 - Test with different file sizes
   - Test edge cases

3. **Test keyboard shortcuts** (if applicable)

4. **Check for memory leaks** (if making significant changes)

### 4. Commit Your Changes

Use clear, descriptive commit messages:

```bash
git add .
git commit -m "Add PDF export feature

- Implement PDF generation using X library
- Add export button to command palette
- Include tests for edge cases
- Update documentation"
```

**Good commit messages**:
- `Add support for table of contents`
- `Fix crash when opening large files`
- `Refactor file loading logic for better performance`
- `Update README with installation instructions`

**Bad commit messages**:
- `fix bug`
- `update`
- `changes`
- `wip`

### 5. Push and Create Pull Request

```bash
git push origin feature/your-feature-name
```

Then create a Pull Request on GitHub:
1. Go to your fork on GitHub
2. Click "Pull Request"
3. Select your feature branch
4. Fill out the PR template with:
   - Clear description of changes
   - Screenshots (if UI changes)
   - Related issue numbers

## ?? Reporting Bugs

### Before Reporting

1. **Search existing issues** to avoid duplicates
2. **Test on the latest version**
3. **Try to reproduce** the bug consistently

### Creating a Bug Report

Include:
- **Clear title**: "Crash when opening files with emoji in filename"
- **Steps to reproduce**: Numbered list of exact steps
- **Expected behavior**: What should happen
- **Actual behavior**: What actually happens
- **Environment**:
  - Windows version
  - .NET version
  - Nulis version
- **Screenshots/Logs**: If applicable
- **Sample file**: If the bug relates to a specific file

## ?? Suggesting Features

### Feature Requests Should Include:

- **Use case**: Why is this feature needed?
- **Description**: What should the feature do?
- **Mockups**: UI mockups if applicable
- **Alternatives**: Other solutions you've considered

## ??? Project Architecture

### Key Components

```
Nulis/
??? App.xaml.cs       # Application lifecycle
??? MainWindow.xaml.cs    # Main window and editor logic
??? Controls/      # Custom controls
?   ??? CustomCommandPalette.cs
??? Services/       # Services
?   ??? LoggerService.cs
??? Editor/       # Milkdown editor (TypeScript)
    ??? src/
 ?   ??? main.ts       # Editor initialization
    ??? package.json
```

### Data Flow

1. User interacts with UI (WinUI 3)
2. MainWindow handles events
3. Commands sent to WebView2/Milkdown
4. Editor sends messages back via `WebMessageReceived`
5. Files saved/loaded through Windows Storage APIs

## ?? Resources

### Documentation

- [WinUI 3 Documentation](https://docs.microsoft.com/windows/apps/winui/)
- [WebView2 Documentation](https://docs.microsoft.com/microsoft-edge/webview2/)
- [Milkdown Documentation](https://milkdown.dev/)
- [ProseMirror Guide](https://prosemirror.net/docs/guide/)

### Useful Links

- [.NET 8 Documentation](https://docs.microsoft.com/dotnet/)
- [C# Programming Guide](https://docs.microsoft.com/dotnet/csharp/)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)

## ? Pull Request Checklist

Before submitting your PR, ensure:

- [ ] Code builds without errors
- [ ] Changes tested on both light and dark themes
- [ ] No new warnings introduced
- [ ] Logging added for important operations
- [ ] Code follows existing style
- [ ] Commit messages are clear and descriptive
- [ ] PR description explains what and why
- [ ] Screenshots included (if UI changes)
- [ ] Documentation updated (if needed)

## ?? Code Review Process

1. **Automated checks** run on all PRs
2. **Maintainer review** - May request changes
3. **Discussion** - Feel free to discuss suggestions
4. **Approval & Merge** - Once approved, maintainers will merge

## ?? Good First Issues

Look for issues labeled:
- `good first issue` - Good for newcomers
- `help wanted` - Maintainers need help
- `documentation` - Improve docs

## ?? Communication

- **GitHub Issues** - Bug reports and features
- **GitHub Discussions** - General questions and ideas
- **Pull Request comments** - Code-specific discussions

## ?? License

By contributing, you agree that your contributions will be licensed under the MIT License.

## ?? Thank You!

Your contributions make Nulis better for everyone. Thank you for taking the time to contribute!

---

**Questions?** Feel free to ask in [GitHub Discussions](https://github.com/armiaab/Nulis/discussions)
