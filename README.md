# Nulis

A fast, lightweight, and distraction-free WYSIWYG markdown editor for Windows.

![Dark Theme](docs/dark.png)
![Light Theme](docs/light.png)

## ✨ Features

- 🎨 **Beautiful WYSIWYG Editor** - See your markdown rendered as you type
- 🌓 **Dark & Light Themes** - Automatically follows your Windows theme
- ⚡ **Lightning Fast** - Built with performance in mind
- 🎯 **Distraction-Free** - Clean, minimal interface
- 📝 **Full Markdown Support** - CommonMark + GitHub Flavored Markdown
- 🖼️ **Image Support** - Drag & drop or paste images
- ⌨️ **Keyboard Shortcuts** - Quick access to all features
- 💾 **Auto-Save Tracking** - Never lose your work
- 🎨 **Spell Check** - Built-in spell checking
- 🔍 **Command Palette** - Quick access to all features (Ctrl+Shift+P)

## 🚀 Getting Started

### Download & Install

#### Option 1: Download Pre-Built Release (Recommended)

1. **Download the latest release** from the [Releases page](https://github.com/armiaab/Nulis/releases)
2. **Choose your platform:**
 - `Nulis-x64.zip` - For 64-bit Windows (most common)
   - `Nulis-x86.zip` - For 32-bit Windows
   - `Nulis-ARM64.zip` - For Windows on ARM devices
3. **Extract the ZIP file** to a folder of your choice
4. **Run `Nulis.exe`** from the extracted folder

#### Option 2: Install via MSIX Package (Microsoft Store format)

1. Download the `.msix` or `.msixbundle` file from [Releases](https://github.com/armiaab/Nulis/releases)
2. Double-click to install
3. Launch Nulis from the Start Menu

### System Requirements

- **OS**: Windows 10 version 1809 (build 17763) or later
- **Runtime**: .NET 8.0 Runtime (automatically included in releases)
- **Additional**: WebView2 Runtime (automatically installed with Windows 11, or [download here](https://developer.microsoft.com/en-us/microsoft-edge/webview2/))

## 📖 How to Use

### Basic Operations

| Action | Shortcut | Description |
|--------|----------|-------------|
| **New File** | `Ctrl + N` | Create a new markdown file |
| **Open File** | `Ctrl + O` | Open an existing file |
| **Save** | `Ctrl + S` | Save current file |
| **Save As** | `Ctrl + Shift + S` | Save as a new file |
| **Rename** | `F2` | Rename the current file |
| **Command Palette** | `Ctrl + Shift + P` | Show all commands |

### Formatting

| Action | Shortcut | Markdown |
|--------|----------|----------|
| **Bold** | `Ctrl + B` | `**text**` |
| **Italic** | `Ctrl + I` | `*text*` |
| **Heading 1** | Type `#` + Space | `# Heading` |
| **Heading 2** | Type `##` + Space | `## Heading` |
| **Code Block** | Type ` ``` ` | ` ```code``` ` |
| **List** | Type `-` or `*` + Space | `- item` |
| **Numbered List** | Type `1.` + Space | `1. item` |
| **Quote** | Type `>` + Space | `> quote` |

### Working with Images

1. **Drag & Drop** - Drag an image file into the editor
2. **Command** - Use Command Palette (`Ctrl+Shift+P`) → "Pick Image"
3. **Markdown** - Type `![alt text](image.png)`

### Opening Files

- **Double-click** a `.md` file in File Explorer (after setting Nulis as default)
- **Drag & drop** a markdown file onto Nulis
- **Command line**: `Nulis.exe "path\to\file.md"`

## 🛠️ Building from Source

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v18 or later)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (optional, recommended)

### Clone & Build

```bash
# Clone the repository
git clone https://github.com/armiaab/Nulis.git
cd Nulis

# Build the project (this will also build the Milkdown editor)
dotnet build Nulis\Nulis.csproj -c Release

# Run the application
dotnet run --project Nulis\Nulis.csproj
```

### Development Build (with debugging)

```bash
# Switch to master branch for development
git checkout master

# Build and run
dotnet build Nulis\Nulis.csproj -c Debug
dotnet run --project Nulis\Nulis.csproj
```

### Production Build (optimized)

```bash
# Switch to production branch
git checkout production

# Build for all platforms (x64, x86, ARM64)
.\publish-production.ps1

# Or build for specific platform
dotnet publish Nulis\Nulis.csproj -c Release -p:Platform=x64
```

## 📦 Building Releases

For detailed instructions on creating production builds and releases, see:
- **Production builds**: Switch to the `production` branch
- **Publishing guide**: See `QUICK-PUBLISH.md` on the production branch
- **Automated script**: Use `publish-production.ps1` on the production branch

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Workflow

1. Fork the repository
2. Create a feature branch from `master`
3. Make your changes
4. Test thoroughly
5. Submit a Pull Request to `master`

**Note**: The `production` branch is for optimized releases only. Always develop on `master`.

## 📝 Project Structure

```
Nulis/
├── Nulis/      # Main WinUI 3 application
│   ├── Assets/   # Images and resources
│   │   └── milkdown/        # Built editor files
│   ├── Controls/            # Custom controls
│   ├── Services/            # Application services
│   ├── Editor/          # Milkdown editor source
│   │   ├── src/            # TypeScript source
│   │   └── package.json    # Node dependencies
│   ├── App.xaml.cs# Application entry point
│   ├── MainWindow.xaml.cs  # Main window logic
│   └── Nulis.csproj        # Project file
├── publish-production.ps1   # Production build script (production branch)
└── README.md            # This file
```

## 🔧 Technologies Used

- **WinUI 3** - Modern Windows UI framework
- **.NET 8** - Application runtime
- **WebView2** - Web rendering engine
- **Milkdown** - Markdown WYSIWYG editor
- **ProseMirror** - Text editing framework

## ❓ FAQ

### Why can't I open certain file types?
Nulis supports `.md`, `.markdown`, and `.txt` files only.

### How do I set Nulis as my default markdown editor?
Right-click a `.md` file → Open with → Choose another app → Select Nulis → Check "Always use this app"

### Does Nulis work offline?
Yes! Nulis is a fully offline application.

### Where are my files saved?
Files are saved wherever you choose. Nulis doesn't store files in a special location.

### Can I use Nulis on macOS or Linux?
Currently, Nulis is Windows-only due to WinUI 3. Cross-platform support may come in the future.

## 🐛 Known Issues & Limitations

- File must be saved before renaming
- Large files (>10MB) may load slowly
- Some advanced markdown features may not render perfectly

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- [Milkdown](https://milkdown.dev/) - Beautiful markdown editor component
- [ProseMirror](https://prosemirror.net/) - Powerful editing framework
- [WinUI 3](https://docs.microsoft.com/windows/apps/winui/) - Modern Windows UI

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/armiaab/Nulis/issues)
- **Discussions**: [GitHub Discussions](https://github.com/armiaab/Nulis/discussions)

---