# PulseAPK

**PulseAPK** je profesionálne GUI na reverzné inžinierstvo Androidu a bezpečnostnú analýzu, postavené na WPF a .NET 8. Spája surovú silu `apktool` s pokročilými možnosťami statickej analýzy v rýchlom, cyberpunkom inšpirovanom rozhraní. PulseAPK zjednodušuje celý pracovný postup od dekompilácie cez analýzu, prestavbu až po podpis.

[Pozrieť demo na YouTube](https://youtu.be/Mkdt0c-7Wwg)

![PulseAPK UI](images/apktool_decompile.png)

Môžete tiež vykonať analýzu Smali kódu. Stačí pretiahnuť priečinok Smali do karty Analysis.

![PulseAPK Smali Analysis](images/apktool_analysis.png)

Ak chcete priečinok Smali zostaviť (a v prípade potreby podpísať), použite sekciu "Build APK".

![PulseAPK Build APK](images/apktool_build_apk.png)

## Kľúčové funkcie

- **🛡️ Statická bezpečnostná analýza**: automaticky skenuje Smali kód na zraniteľnosti, vrátane detekcie rootu, kontrol emulátora, natvrdo zakódovaných prihlasovacích údajov a nezabezpečeného použitia SQL/HTTP.
- **⚙️ Dynamický engine pravidiel**: úplne prispôsobiteľné analytické pravidlá cez `smali_analysis_rules.json`. Vzory detekcie možno meniť bez reštartu aplikácie. Kešovanie zabezpečuje optimálny výkon.
- **🚀 Moderné UI/UX**: responzívne tmavé rozhranie navrhnuté pre efektivitu, s podporou drag-and-drop a spätnou väzbou konzoly v reálnom čase.
- **📦 Kompletný workflow**: dekompilácia, analýza, úpravy, prestavba a podpis APK v jednom prostredí.
- **⚡ Bezpečné a robustné**: zahŕňa inteligentnú validáciu a prevenciu pádov na ochranu pracovného priestoru a dát.
- **🔧 Plne konfigurovateľné**: správa ciest nástrojov (Java, Apktool), nastavení pracovného priestoru a analytických parametrov.

## Pokročilé možnosti

### Bezpečnostná analýza
PulseAPK obsahuje vstavaný statický analyzátor, ktorý skenuje dekompilovaný kód na bežné bezpečnostné indikátory:
- **Detekcia rootu**: identifikuje kontroly Magisk, SuperSU a bežných root binárnych súborov.
- **Detekcia emulátora**: nachádza kontroly QEMU, Genymotion a špecifických systémových vlastností.
- **Citlivé údaje**: skenuje natvrdo zakódované API kľúče, tokeny a hlavičky Basic Auth.
- **Nezabezpečené siete**: označuje používanie HTTP a potenciálne miesta úniku dát.

*Pravidlá sú definované v `smali_analysis_rules.json` a dajú sa prispôsobiť vašim potrebám.*

### Správa APK
- **Dekomplikácia**: jednoduché dekódovanie zdrojov a kódu s konfigurovateľnými voľbami.
- **Prestavba**: prestavia upravené projekty do platných APK.
- **Podpisovanie**: integrovaná správa keystore na podpisovanie prestavaných APK, aby boli pripravené na inštaláciu.

## Požiadavky

1.  **Java Runtime Environment (JRE)**: vyžaduje sa pre `apktool`. Uistite sa, že `java` je v `PATH`.
2.  **Apktool**: stiahnite `apktool.jar` z [ibotpeaches.github.io](https://ibotpeaches.github.io/Apktool/).
3.  **Ubersign (Uber APK Signer)**: vyžaduje sa na podpisovanie prestavaných APK. Stiahnite najnovší `uber-apk-signer.jar` z [GitHub releases](https://github.com/patrickfav/uber-apk-signer/releases).
4.  **.NET 8.0 Runtime**: vyžaduje sa na spustenie PulseAPK vo Windows.

## Rýchly štart

1.  **Stiahnuť a zostaviť**
    ```powershell
    dotnet build
    dotnet run
    ```

2.  **Nastavenie**
    - Otvorte **Settings**.
    - Nastavte cestu k `apktool.jar`.
    - PulseAPK automaticky zistí inštaláciu Javy na základe premenných prostredia.

3.  **Analýza APK**
    - **Dekomplikujte** cieľový APK v karte Decompile.
    - Prepnite na kartu **Analysis**.
    - Vyberte priečinok dekompilovaného projektu.
    - Kliknite na **Analyze Smali**, aby sa vytvorila bezpečnostná správa.

4.  **Úpravy a prestavba**
    - Upravte súbory v priečinku projektu.
    - Použite kartu **Build** na zostavenie nového APK.
    - Použite kartu **Sign** na podpis výstupného APK.

## Technická architektúra

PulseAPK používa čistú MVVM (Model-View-ViewModel) architektúru:

- **Core**: .NET 8.0, WPF.
- **Analysis**: vlastný regexový statický analyzátor s pravidlami pre hot reload.
- **Services**: dedikované služby pre integráciu Apktool, monitoring súborového systému a správu nastavení.

## Licencia

Tento projekt je open-source a dostupný pod licenciou [Apache License 2.0](LICENSE.md).

### ❤️ Podporte projekt

Ak je PulseAPK pre vás užitočný, môžete podporiť jeho vývoj stlačením tlačidla "Support" hore.

Hviezdička repozitára tiež veľmi pomáha.

### Prispievanie

Príspevky vítame! Upozorňujeme, že všetci prispievatelia musia podpísať [Contributor License Agreement (CLA)](CLA.md), aby ich práca mohla byť legálne distribuovaná.
Odoslaním pull requestu súhlasíte s podmienkami CLA.
