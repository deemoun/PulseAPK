# PulseAPK

**PulseAPK** ist eine professionelle GUI für Android-Reverse-Engineering und Sicherheitsanalyse, gebaut mit WPF und .NET 8. Es kombiniert die rohe Leistung von `apktool` mit erweiterten statischen Analysefunktionen, verpackt in einer leistungsstarken, cyberpunk-inspirierten Oberfläche. PulseAPK optimiert den gesamten Workflow von der Dekompilierung über Analyse, Rebuild und Signierung.

[Demo auf YouTube ansehen](https://youtu.be/Mkdt0c-7Wwg)

![PulseAPK UI](images/apktool_decompile.png)

Du kannst auch eine Smali-Code-Analyse durchführen. Ziehe einfach den Smali-Ordner in den Analysis-Tab.

![PulseAPK Smali Analysis](images/apktool_analysis.png)

Wenn du den Smali-Ordner erstellen (und falls nötig signieren) möchtest, nutze den Bereich "Build APK".

![PulseAPK Build APK](images/apktool_build_apk.png)

## Hauptfunktionen

- **🛡️ Statische Sicherheitsanalyse**: Scannt Smali-Code automatisch auf Schwachstellen, einschließlich Root-Erkennung, Emulator-Checks, fest codierter Zugangsdaten und unsicherer SQL/HTTP-Nutzung.
- **⚙️ Dynamische Regel-Engine**: Vollständig anpassbare Analyse-Regeln über `smali_analysis_rules.json`. Erkennungs-Patterns lassen sich ohne Neustart ändern. Caching sorgt für optimale Performance.
- **🚀 Modernes UI/UX**: Reaktionsfähige, dunkle Oberfläche für effizientes Arbeiten mit Drag-and-Drop und Echtzeit-Konsolenfeedback.
- **📦 Vollständiger Workflow**: APKs dekompilieren, analysieren, bearbeiten, neu bauen und signieren – alles in einer Umgebung.
- **⚡ Sicher & robust**: Enthält intelligente Validierung und Crash-Prävention zum Schutz von Workspace und Daten.
- **🔧 Vollständig konfigurierbar**: Tool-Pfade (Java, Apktool), Workspace-Einstellungen und Analyseparameter bequem verwalten.

## Erweiterte Fähigkeiten

### Sicherheitsanalyse
PulseAPK enthält einen integrierten statischen Analyzer, der dekompilierten Code auf gängige Sicherheitsindikatoren scannt:
- **Root-Erkennung**: Identifiziert Checks für Magisk, SuperSU und gängige Root-Binaries.
- **Emulator-Erkennung**: Findet Checks für QEMU, Genymotion und spezifische Systemeigenschaften.
- **Sensible Daten**: Scannt nach fest codierten API-Keys, Tokens und Basic-Auth-Headern.
- **Unsichere Netzwerkkommunikation**: Markiert HTTP-Nutzung und potenzielle Datenlecks.

*Regeln sind in `smali_analysis_rules.json` definiert und können an deine Bedürfnisse angepasst werden.*

### APK-Management
- **Dekompilierung**: Ressourcen und Quellcodes mit konfigurierbaren Optionen mühelos decodieren.
- **Rekompilierung**: Geänderte Projekte zu gültigen APKs neu bauen.
- **Signierung**: Integriertes Keystore-Management zum Signieren neu gebauter APKs, damit sie bereit für die Geräteinstallation sind.

## Voraussetzungen

1.  **Java Runtime Environment (JRE)**: Erforderlich für `apktool`. Stelle sicher, dass `java` in deinem `PATH` liegt.
2.  **Apktool**: Lade `apktool.jar` von [ibotpeaches.github.io](https://ibotpeaches.github.io/Apktool/) herunter.
3.  **Ubersign (Uber APK Signer)**: Erforderlich zum Signieren neu gebauter APKs. Lade die neueste Version von `uber-apk-signer.jar` aus den [GitHub Releases](https://github.com/patrickfav/uber-apk-signer/releases) herunter.
4.  **.NET 8.0 Runtime**: Erforderlich, um PulseAPK unter Windows auszuführen.

## Schnellstart

1.  **Herunterladen und Build**
    ```powershell
    dotnet build
    dotnet run
    ```

2.  **Setup**
    - Öffne **Settings**.
    - Hinterlege den Pfad zu `apktool.jar`.
    - PulseAPK erkennt deine Java-Installation automatisch anhand der Umgebungsvariablen.

3.  **APK analysieren**
    - **Dekompiliere** dein Ziel-APK im Decompile-Tab.
    - Wechsle zum **Analysis**-Tab.
    - Wähle den dekompilierten Projektordner.
    - Klicke auf **Analyze Smali**, um einen Sicherheitsbericht zu erzeugen.

4.  **Ändern & neu bauen**
    - Bearbeite Dateien im Projektordner.
    - Nutze den **Build**-Tab, um ein neues APK zu bauen.
    - Nutze den **Sign**-Tab, um das Ausgabe-APK zu signieren.

## Technische Architektur

PulseAPK verwendet eine saubere MVVM-Architektur (Model-View-ViewModel):

- **Core**: .NET 8.0, WPF.
- **Analysis**: Eigener regex-basierter statischer Analyse-Engine mit hot-reloadbaren Regeln.
- **Services**: dedizierte Services für Apktool-Interaktion, Dateisystem-Monitoring und Einstellungsverwaltung.

## Lizenz

Dieses Projekt ist Open Source und unter der [Apache License 2.0](LICENSE.md) verfügbar.

### ❤️ Unterstütze das Projekt

Wenn PulseAPK für dich nützlich ist, kannst du die Entwicklung unterstützen, indem du oben auf den „Support“-Button klickst.

Ein Stern für das Repository hilft ebenfalls sehr.

### Beitrag

Wir freuen uns über Beiträge! Bitte beachte, dass alle Mitwirkenden unsere [Contributor License Agreement (CLA)](CLA.md) unterschreiben müssen, damit ihre Arbeit legal verteilt werden kann.
Mit dem Einreichen eines Pull Requests stimmst du den Bedingungen der CLA zu.
