# PulseAPK

**PulseAPK** egy professzionális GUI Android visszafejtéshez és biztonsági elemzéshez, WPF-fel és .NET 8-cal készítve. Ötvözi az `apktool` nyers erejét a fejlett statikus elemzési képességekkel, mindezt egy nagy teljesítményű, cyberpunk ihlette felületen. A PulseAPK leegyszerűsíti a teljes munkafolyamatot a dekompilálástól az elemzésen át az újraépítésig és aláírásig.

[Demo megtekintése YouTube-on](https://youtu.be/Mkdt0c-7Wwg)

![PulseAPK UI](images/apktool_decompile.png)

Smali kód elemzés is végezhető. Egyszerűen húzd a Smali mappát az Analysis fülre.

![PulseAPK Smali Analysis](images/apktool_analysis.png)

Ha a Smali mappát szeretnéd összeállítani (és szükség esetén aláírni), használd a "Build APK" részt.

![PulseAPK Build APK](images/apktool_build_apk.png)

## Főbb funkciók

- **🛡️ Statikus biztonsági elemzés**: automatikusan vizsgálja a Smali kódot sérülékenységek után, beleértve a root észlelést, emulátor-ellenőrzéseket, keménykódolt hitelesítő adatokat és a nem biztonságos SQL/HTTP használatot.
- **⚙️ Dinamikus szabálymotor**: teljesen testreszabható elemzési szabályok a `smali_analysis_rules.json` fájlban. Az észlelési minták újraindítás nélkül módosíthatók. A gyorsítótárazás optimális teljesítményt biztosít.
- **🚀 Modern UI/UX**: reszponzív, sötét felület hatékony munkához, drag-and-drop támogatással és valós idejű konzolvisszajelzéssel.
- **📦 Teljes munkafolyamat**: APK-k dekompilálása, elemzése, szerkesztése, újraépítése és aláírása egyetlen környezetben.
- **⚡ Biztonságos és robusztus**: intelligens validációt és összeomlás-megelőző mechanizmusokat tartalmaz a munkaterület és adatok védelmére.
- **🔧 Teljesen konfigurálható**: eszközútvonalak (Java, Apktool), munkaterület-beállítások és elemzési paraméterek könnyű kezelése.

## Speciális képességek

### Biztonsági elemzés
A PulseAPK beépített statikus elemzőt tartalmaz, amely a dekompilált kódot gyakori biztonsági indikátorok után vizsgálja:
- **Root észlelés**: azonosítja a Magisk, SuperSU és gyakori root binárisok ellenőrzéseit.
- **Emulátor észlelés**: megtalálja a QEMU, Genymotion és bizonyos rendszerjellemzők ellenőrzéseit.
- **Érzékeny adatok**: keménykódolt API-kulcsok, tokenek és Basic Auth fejlécek keresése.
- **Nem biztonságos hálózat**: jelzi a HTTP használatot és a lehetséges adatkiáramlási pontokat.

*A szabályok a `smali_analysis_rules.json` fájlban vannak definiálva, és igény szerint testreszabhatók.*

### APK-kezelés
- **Dekompilálás**: erőforrások és források dekódolása konfigurálható opciókkal.
- **Újraépítés**: módosított projektek újbóli összeállítása érvényes APK-ká.
- **Aláírás**: integrált keystore-kezelés az újraépített APK-k aláírásához, hogy telepítésre készek legyenek.

## Előfeltételek

1.  **Java Runtime Environment (JRE)**: szükséges az `apktool` használatához. Győződj meg róla, hogy a `java` szerepel a `PATH`-ban.
2.  **Apktool**: töltsd le az `apktool.jar` fájlt innen: [ibotpeaches.github.io](https://ibotpeaches.github.io/Apktool/).
3.  **Ubersign (Uber APK Signer)**: szükséges az újraépített APK-k aláírásához. Töltsd le a legújabb `uber-apk-signer.jar` fájlt a [GitHub releases](https://github.com/patrickfav/uber-apk-signer/releases) oldalról.
4.  **.NET 8.0 Runtime**: szükséges a PulseAPK futtatásához Windows rendszeren.

## Gyors indítási útmutató

1.  **Letöltés és buildelés**
    ```powershell
    dotnet build
    dotnet run
    ```

2.  **Beállítás**
    - Nyisd meg a **Settings** menüt.
    - Állítsd be az `apktool.jar` útvonalát.
    - A PulseAPK automatikusan felismeri a Java telepítést a környezeti változók alapján.

3.  **APK elemzése**
    - **Dekompiláld** a cél APK-t a Decompile fülön.
    - Válts az **Analysis** fülre.
    - Válaszd ki a dekompilált projektmappát.
    - Kattints az **Analyze Smali** gombra a biztonsági jelentés létrehozásához.

4.  **Módosítás és újraépítés**
    - Szerkeszd a projektmappában lévő fájlokat.
    - A **Build** fülön készíts új APK-t.
    - A **Sign** fülön írd alá a kimeneti APK-t.

## Technikai architektúra

A PulseAPK tiszta MVVM (Model-View-ViewModel) architektúrát használ:

- **Core**: .NET 8.0, WPF.
- **Analysis**: egyedi regex alapú statikus elemzőmotor hot-reloadolható szabályokkal.
- **Services**: dedikált szolgáltatások Apktool integrációhoz, fájlrendszer-monitorozáshoz és beállításkezeléshez.

## Licenc

Ez a projekt nyílt forráskódú, és az [Apache License 2.0](LICENSE.md) alatt érhető el.

### ❤️ Támogasd a projektet

Ha a PulseAPK hasznos számodra, támogathatod a fejlesztést a felül található "Support" gomb megnyomásával.

A repó csillagozása is sokat segít.

### Közreműködés

Szívesen fogadjuk a hozzájárulásokat! Kérjük, vedd figyelembe, hogy minden közreműködőnek alá kell írnia a [Contributor License Agreement (CLA)](CLA.md) dokumentumot, hogy a munkája jogszerűen terjeszthető legyen.
Pull request beküldésével elfogadod a CLA feltételeit.
