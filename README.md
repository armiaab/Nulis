# Nulis 📝

**A modern, lightweight markdown editor for Windows**

Built with WinUI 3, .NET 8, and Milkdown for a beautiful, distraction-free writing experience.

![Version](https://img.shields.io/badge/version-0.1.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

## ✨ Features

- 🎨 **Clean Interface** - Minimalist, distraction-free design
- ⚡ **Real-time Preview** - WYSIWYG markdown editing powered by Milkdown
- 📝 **Full Markdown Support** - CommonMark + GitHub Flavored Markdown (GFM)
- 🎯 **File Associations** - Automatically handles `.md`, `.markdown`, and `.txt` files
- 🚀 **Fast & Responsive** - Native Windows performance with WinUI 3
- 💾 **Auto-save** - Never lose your work
- 🌙 **Dark Mode** - Easy on the eyes
- ⌨️ **Keyboard Shortcuts** - Efficient editing workflow

## 📥 Installation

### Option 1: Download MSIX Installer (Recommended)

1. **Download** the latest installer from [Releases](https://github.com/armiaab/Nulis/releases)
2. **Extract** the ZIP file
3. **Run** the installation:

   **Easy Install (PowerShell):**
   ```powershell
   # Navigate to extracted folder
   cd Nulis_[version]_x64_Test
   
   # Run installer script
   .\Install.ps1
   ```

   **Manual Install:**
   - Install certificate: Double-click `Nulis_[version]_x64.cer`
     - Select "Local Machine" → "Trusted Root Certification Authorities"
   - Install app: Double-click `Nulis_[version]_x64.msix`

4. **Launch** Nulis from Start Menu!

### Option 2: Developer Mode Install

For testers and developers:

1. Enable **Developer Mode** in Windows:
   - Settings → Privacy & Security → For developers → Turn on
2. Install the certificate (see above)
3. Double-click the MSIX to install

### System Requirements

- Windows 10 version 1809 (build 17763) or later
- Windows 11 (recommended)
- x64, x86, or ARM64 processor
- .NET 8 Desktop Runtime (auto-installed)
- WebView2 Runtime (pre-installed on Windows 11)

## 🎯 Usage

### Getting Started

1. Launch Nulis from Start Menu
2. Create a new file (`Ctrl+N`) or open existing (`Ctrl+O`)
3. Start writing markdown!

### Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| New file | `Ctrl+N` |
| Open file | `Ctrl+O` |
| Save | `Ctrl+S` |
| Save as | `Ctrl+Shift+S` |
| Command Palatte | `Ctrl+Shift+P` |
| Rename File | `F2` |
| Format Menu | `/` |

### File Associations

Nulis automatically registers for:
- `.md` - Markdown files
- `.markdown` - Markdown files
- `.txt` - Text files

**Set as default:**
Right-click a `.md` file → Open with → Choose Nulis → Check "Always use this app"

## 🛠️ Building from Source

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v18 or later)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) with:
  - Windows App SDK workload
  - Desktop development with C++

### Quick Build

```bash
# Clone the repository
git clone https://github.com/armiaab/Nulis.git
cd Nulis

# Build the MSIX installer
.\build-quick.ps1
```

The installer will be created in `Output/Nulis_[version]_x64_Test/`

### Development Build

```bash
# Clone and build
git clone https://github.com/armiaab/Nulis.git
cd Nulis

# Open in Visual Studio
start Nulis.sln

# Or build via command line
dotnet build Nulis\Nulis.csproj -c Debug

# Run
dotnet run --project Nulis\Nulis.csproj
```

### Production MSIX Build

```powershell
# Build x64 installer (recommended)
.\build-installer.ps1 -Platform x64 -Configuration Release

# Build for other platforms
.\build-installer.ps1 -Platform x86
.\build-installer.ps1 -Platform ARM64

# Build all platforms at once
.\build-all-platforms.ps1
```

## 📦 Building Releases

### Create MSIX Package

The `production` branch contains optimized builds with MSIX installer support:

```bash
# Switch to production branch
git checkout production

# Build installer
.\build-quick.ps1

# Output: Output/Nulis_[version]_x64_Test/Nulis_[version]_x64.msix
```

### Build Scripts

| Script | Purpose |
|--------|---------|
| `build-quick.ps1` | Quick x64 Release build |
| `build-installer.ps1` | Custom platform/config build |
| `build-all-platforms.ps1` | Build x64, x86, ARM64 |
| `build-auto.ps1` | Auto-finds MSBuild |

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Workflow

1. Fork the repository
2. Create a feature branch from `master`
   ```bash
   git checkout master
   git checkout -b feature/amazing-feature
   ```
3. Make your changes
4. Test thoroughly
5. Commit your changes
   ```bash
   git commit -m 'Add some amazing feature'
   ```
6. Push to the branch
   ```bash
   git push origin feature/amazing-feature
   ```
7. Open a Pull Request to `master`

**Note**: 
- `master` - Development branch

## 📝 Project Structure

```
Nulis/
├── Nulis/                   # Main WinUI 3 application
│   ├── Assets/              # Images and resources
│   │   └── milkdown/        # Built editor files
│   ├── Controls/            # Custom UI controls
│   ├── Services/            # Application services
│   ├── Editor/              # Milkdown editor source
│   │   ├── src/             # TypeScript source
│   │   └── package.json     # Node dependencies
│   ├── App.xaml.cs          # Application entry point
│   ├── MainWindow.xaml.cs   # Main window logic
│   └── Nulis.csproj         # Project file
├── Output/                  # Build output (MSIX packages)
├── build-installer.ps1      # Build script (production branch)
├── build-quick.ps1          # Quick build (production branch)
└── README.md                # This file
```

## 🔧 Technologies Used

- **[WinUI 3](https://docs.microsoft.com/windows/apps/winui/)** - Modern Windows UI framework
- **[.NET 8](https://dot.net/)** - Application runtime
- **[WebView2](https://developer.microsoft.com/microsoft-edge/webview2/)** - Web rendering engine
- **[Milkdown](https://milkdown.dev/)** - Markdown WYSIWYG editor
- **[ProseMirror](https://prosemirror.net/)** - Text editing framework
- **[Vite](https://vitejs.dev/)** - Frontend build tool

## 🗑️ Uninstall

1. Open **Settings** → **Apps** → **Installed apps**
2. Find **Nulis** in the list
3. Click the three dots (⋯) → **Uninstall**
4. Confirm

## 🐛 Troubleshooting

### "Package could not be registered"
- Enable Developer Mode: Settings → Privacy & Security → For developers
- Install certificate to "Trusted Root Certification Authorities"
- Verify correct platform (x64/x86/ARM64 matches your system)

### "App didn't start"
- Requires Windows 10 version 1809 or later
- Verify .NET 8 Desktop Runtime is installed
- Try uninstalling and reinstalling

### Certificate warnings
Development builds use a self-signed certificate. For production, use a proper code signing certificate or distribute through Microsoft Store.

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
Currently Windows-only due to WinUI 3. Cross-platform support may come in the future.

### How do I update Nulis?
Download and install the latest MSIX from Releases. It will update automatically.

## 🚀 Roadmap

- [ ] Math Support
- [ ] HTML tag support
- [ ] Microsoft Store distribution
- [ ] Custom themes and color schemes

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [Milkdown](https://milkdown.dev/) - Beautiful markdown editor component
- [ProseMirror](https://prosemirror.net/) - Powerful editing framework
- [WinUI 3](https://docs.microsoft.com/windows/apps/winui/) - Modern Windows UI
- [.NET](https://dot.net/) - Application framework