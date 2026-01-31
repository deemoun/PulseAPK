# PulseAPK

<p align="center">
  🌍 <strong>Languages</strong><br>
  <a href="README.md">English</a> |
  <a href="README.de.md">Deutsch</a> |
  <a href="README.es.md">Español</a> |
  <a href="README.fr.md">Français</a> |
  <a href="README.he.md">עברית</a> |
  <a href="README.ko.md">한국어</a> |
  <a href="README.be.md">Беларуская</a> |
  <a href="README.fi.md">Suomi</a> |
  <a href="README.lv.md">Latviešu</a> |
  <a href="README.et.md">Eesti</a> |
  <a href="README.lt.md">Lietuvių</a> |
  <a href="README.cs.md">Čeština</a> |
  <a href="README.sk.md">Slovenčina</a> |
  <a href="README.hu.md">Magyar</a> |
  <a href="README.ar.md">العربية</a> |
  <a href="README.pt.md">Português</a> |
  <a href="README.ru.md">Русский</a> |
  <a href="README.uk.md">Українська</a> |
  <a href="README.zh.md">中文</a>
</p>

**PulseAPK** is a professional-grade GUI for Android reverse engineering and security analysis, built with WPF and .NET 8. It combines the raw power of `apktool` with advanced static analysis capabilities, wrapped in a high-performance, cyberpunk-inspired interface. PulseAPK streamlines the entire workflow from decompilation to analysis, rebuilding, and signing.

[Watch the demo on YouTube](https://youtu.be/Mkdt0c-7Wwg)

![PulseAPK UI](images/apktool_decompile.png)

You can also perform the Smali code analysis. Just drag and drop the Smali folder into the Analysis tab.

![PulseAPK Smali Analysis](images/apktool_analysis.png)

If you want to build (and sign, if necessary) the Smali folder - use the "Build APK" section.

![PulseAPK Build APK](images/apktool_build_apk.png)

## Key Features

- **🛡️ Static Security Analysis**: Automatically scan Smali code for vulnerabilities, including root detection, emulator checks, hardcoded credentials, and insecure SQL/HTTP usage.
- **⚙️ Dynamic Rule Engine**: Fully customizable analysis rules via `smali_analysis_rules.json`. Modify detection patterns on the fly without restarting the application. Uses caching for optimal performance.
- **🚀 Modern UI/UX**: A responsive, dark-themed interface designed for efficiency, featuring drag-and-drop support and real-time console feedback.
- **📦 Complete Workflow**: Decompile, Analyze, Edit, Recompile, and Sign APKs in one unified environment.
- **⚡ Safe & Robust**: Includes intelligent validation and crash prevention mechanisms to protect your workspace and data.
- **🔧 Fully Configurable**: Manage tool paths (Java, Apktool), workspace settings, and analysis parameters with ease.

## Advanced Capabilities

### Security Analysis
PulseAPK includes a built-in static analyzer that scans decompiled code for common security indicators:
- **Root Detection**: Identifies checks for Magisk, SuperSU, and common root binaries.
- **Emulator Detection**: Finds checks for QEMU, Genymotion, and specific system properties.
- **Sensitive Data**: Scans for hardcoded API keys, tokens, and basic auth headers.
- **Insecure Networking**: Flags HTTP usage and potential data leakage points.

*Rules are defined in `smali_analysis_rules.json` and can be customized to your specific needs.*

### APK Management
- **Decompilation**: Effortlessly decode resources and sources with configurable options.
- **Recompilation**: Rebuild your modified projects into valid APKs.
- **Signing**: Integrated keystore management for signing rebuilt APKs, ensuring they are ready for device installation.

## Prerequisites

1.  **Java Runtime Environment (JRE)**: Required for `apktool`. Ensure `java` is in your system `PATH`.
2.  **Apktool**: Download `apktool.jar` from [ibotpeaches.github.io](https://ibotpeaches.github.io/Apktool/).
3.  **Ubersign (Uber APK Signer)**: Required for signing rebuilt APKs. Download the latest `uber-apk-signer.jar` from the [GitHub releases](https://github.com/patrickfav/uber-apk-signer/releases).
4.  **.NET 8.0 Runtime**: Required to run PulseAPK on Windows.

## Quick Start Guide

1.  **Download and Build**
    ```powershell
    dotnet build
    dotnet run
    ```

2.  **Setup**
    - Open **Settings**.
    - Link your `apktool.jar` path.
    - PulseAPK will automatically detect your Java installation based on environment variables.

3.  **Analyze an APK**
    - **Decompile** your target APK using the Decompile tab.
    - Switch to the **Analysis** tab.
    - Select your decompiled project folder.
    - Click **Analyze Smali** to generate a security report.

4.  **Modify & Rebuild**
    - Edit files in your project folder.
    - Use the **Build** tab to recompile into a new APK.
    - Use the **Sign** tab to sign the output APK.

## Technical Architecture

PulseAPK utilizes a clean MVVM (Model-View-ViewModel) architecture:

- **Core**: .NET 8.0, WPF.
- **Analysis**: Custom Regex-based static analysis engine with hot-reloadable rules.
- **Services**: dedicated services for Apktool interaction, file system monitoring, and settings management.

## License

This project is open-source and available under the [Apache License 2.0](LICENSE.md).

### ❤️ Support the project

If PulseAPK is useful for you, you can support its development by pressing the "Support" button at the top

Starring the repository also helps a lot.

### Contributing

We welcome contributions! Please note that all contributors must sign our [Contributor License Agreement (CLA)](CLA.md) to ensure that their work can be legally distributed.
By submitting a pull request, you agree to the terms of the CLA.
