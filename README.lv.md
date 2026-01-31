# PulseAPK

**PulseAPK** ir profesionāls Android reversās inženierijas un drošības analīzes GUI, kas veidots ar WPF un .NET 8. Tas apvieno `apktool` jaudu ar uzlabotām statiskās analīzes iespējām, ietērpts augstas veiktspējas, kiberpanka iedvesmotā saskarnē. PulseAPK racionalizē visu darbplūsmu no dekompilēšanas līdz analīzei, pārbūvei un parakstīšanai.

[Skatīt demo YouTube](https://youtu.be/Mkdt0c-7Wwg)

![PulseAPK UI](images/apktool_decompile.png)

Varat arī veikt Smali koda analīzi. Vienkārši velciet un nometiet Smali mapi uz Analysis cilni.

![PulseAPK Smali Analysis](images/apktool_analysis.png)

Ja vēlaties uzbūvēt (un, ja nepieciešams, parakstīt) Smali mapi, izmantojiet "Build APK" sadaļu.

![PulseAPK Build APK](images/apktool_build_apk.png)

## Galvenās funkcijas

- **🛡️ Statiskā drošības analīze**: automātiski skenē Smali kodu, lai noteiktu ievainojamības, tostarp root noteikšanu, emulatora pārbaudes, cieti iekodētus akreditācijas datus un nedrošu SQL/HTTP lietojumu.
- **⚙️ Dinamiska noteikumu dzinējs**: pilnībā pielāgojami analīzes noteikumi, izmantojot `smali_analysis_rules.json`. Noteikšanas paraugus var mainīt bez lietotnes restartēšanas. Kešošana nodrošina optimālu veiktspēju.
- **🚀 Moderns UI/UX**: responsīva tumšā tēma, kas paredzēta efektivitātei, ar vilkšanas un nomešanas atbalstu un reāllaika konsoles atgriezenisko saiti.
- **📦 Pilnīga darbplūsma**: dekompilēt, analizēt, rediģēt, pārbūvēt un parakstīt APK vienā vidē.
- **⚡ Drošs un stabils**: ietver gudru validāciju un avāriju novēršanas mehānismus, lai aizsargātu darba vidi un datus.
- **🔧 Pilnībā konfigurējams**: ērta rīku ceļu (Java, Apktool), darba vides iestatījumu un analīzes parametru pārvaldība.

## Paplašinātās iespējas

### Drošības analīze
PulseAPK ietver iebūvētu statisko analizatoru, kas skenē dekompilētu kodu, meklējot izplatītus drošības indikatorus:
- **Root noteikšana**: identificē Magisk, SuperSU un izplatītu root bināru pārbaudes.
- **Emulatora noteikšana**: atrod QEMU, Genymotion un specifisku sistēmas īpašību pārbaudes.
- **Jutīgi dati**: skenē cieti iekodētas API atslēgas, tokenus un Basic Auth galvenes.
- **Nedroša tīkla lietošana**: atzīmē HTTP lietojumu un iespējamos datu noplūdes punktus.

*Noteikumi ir definēti `smali_analysis_rules.json` failā un tos var pielāgot savām vajadzībām.*

### APK pārvaldība
- **Dekomplēšana**: viegli atkodē resursus un avotus ar konfigurējamām opcijām.
- **Pārbūve**: pārbūvē modificētus projektus derīgos APK.
- **Parakstīšana**: integrēta keystore pārvaldība pārbūvēto APK parakstīšanai, lai tie būtu gatavi instalēšanai.

## Prasības

1.  **Java Runtime Environment (JRE)**: nepieciešams `apktool`. Pārliecinieties, ka `java` ir jūsu sistēmas `PATH`.
2.  **Apktool**: lejupielādējiet `apktool.jar` no [ibotpeaches.github.io](https://ibotpeaches.github.io/Apktool/).
3.  **Ubersign (Uber APK Signer)**: nepieciešams pārbūvēto APK parakstīšanai. Lejupielādējiet jaunāko `uber-apk-signer.jar` no [GitHub releases](https://github.com/patrickfav/uber-apk-signer/releases).
4.  **.NET 8.0 Runtime**: nepieciešams PulseAPK palaišanai Windows vidē.

## Ātrās palaišanas ceļvedis

1.  **Lejupielādēt un uzbūvēt**
    ```powershell
    dotnet build
    dotnet run
    ```

2.  **Iestatīšana**
    - Atveriet **Settings**.
    - Norādiet ceļu uz `apktool.jar`.
    - PulseAPK automātiski noteiks Java instalāciju pēc vides mainīgajiem.

3.  **APK analīze**
    - **Dekomplējiet** mērķa APK cilnē Decompile.
    - Pārslēdzieties uz **Analysis** cilni.
    - Izvēlieties dekompilēto projekta mapi.
    - Noklikšķiniet uz **Analyze Smali**, lai izveidotu drošības pārskatu.

4.  **Modificēt un pārbūvēt**
    - Rediģējiet failus projekta mapē.
    - Izmantojiet **Build** cilni, lai uzbūvētu jaunu APK.
    - Izmantojiet **Sign** cilni, lai parakstītu izvades APK.

## Tehniskā arhitektūra

PulseAPK izmanto tīru MVVM (Model-View-ViewModel) arhitektūru:

- **Core**: .NET 8.0, WPF.
- **Analysis**: pielāgots regex balstīts statiskās analīzes dzinējs ar karsti pārlādējamiem noteikumiem.
- **Services**: specializēti servisi Apktool integrācijai, failu sistēmas uzraudzībai un iestatījumu pārvaldībai.

## Licence

Šis projekts ir atvērtā koda un pieejams saskaņā ar [Apache License 2.0](LICENSE.md).

### ❤️ Atbalstiet projektu

Ja PulseAPK jums ir noderīgs, varat atbalstīt tā izstrādi, nospiežot "Support" pogu lapas augšdaļā.

Arī zvaigznītes piešķiršana repozitorijam ļoti palīdz.

### Ieguldījums

Mēs priecājamies par ieguldījumiem! Lūdzu, ņemiet vērā, ka visiem līdzautoriem ir jāparaksta [Contributor License Agreement (CLA)](CLA.md), lai viņu darbs varētu tikt likumīgi izplatīts.
Iesniedzot pull request, jūs piekrītat CLA nosacījumiem.
