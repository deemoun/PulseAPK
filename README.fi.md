# PulseAPK

**PulseAPK** on Androidin käänteismallinnuksen ja turvallisuusanalyysin ammattitason GUI, joka on rakennettu WPF:llä ja .NET 8:lla. Se yhdistää `apktool`-työkalun raakavoiman edistyneisiin staattisen analyysin ominaisuuksiin ja tarjoaa suorituskykyisen, kyberpunk-henkisen käyttöliittymän. PulseAPK sujuvoittaa koko työnkulun purkamisesta analyysiin, uudelleenrakennukseen ja allekirjoittamiseen.

[Katso demo YouTubessa](https://youtu.be/Mkdt0c-7Wwg)

![PulseAPK UI](images/apktool_decompile.png)

Voit myös suorittaa Smali-koodin analyysin. Vedä ja pudota Smali-kansio Analysis-välilehdelle.

![PulseAPK Smali Analysis](images/apktool_analysis.png)

Jos haluat rakentaa (ja tarvittaessa allekirjoittaa) Smali-kansion, käytä "Build APK" -osiota.

![PulseAPK Build APK](images/apktool_build_apk.png)

## Avainominaisuudet

- **🛡️ Staattinen turvallisuusanalyysi**: skannaa Smali-koodin haavoittuvuuksien varalta, mukaan lukien root-tunnistus, emulaattoritarkistukset, kovakoodatut tunnistetiedot ja turvaton SQL/HTTP-käyttö.
- **⚙️ Dynaaminen sääntökone**: täysin muokattavat analyysisäännöt `smali_analysis_rules.json`-tiedostolla. Tunnistusmalleja voi muuttaa ilman uudelleenkäynnistystä. Välimuisti parantaa suorituskykyä.
- **🚀 Moderni UI/UX**: responsiivinen, tumma käyttöliittymä tehokasta työskentelyä varten, sisältää vedä ja pudota -tuen sekä reaaliaikaisen konsolipalautteen.
- **📦 Täydellinen työnkulku**: purku, analyysi, muokkaus, uudelleenrakennus ja APK-allekirjoitus yhdessä ympäristössä.
- **⚡ Turvallinen ja luotettava**: älykäs validointi ja kaatumisen estomekanismit suojaavat työtilaa ja dataa.
- **🔧 Täysin konfiguroitava**: hallitse työkalupolut (Java, Apktool), työtilan asetukset ja analyysiparametrit helposti.

## Edistyneet ominaisuudet

### Turvallisuusanalyysi
PulseAPK sisältää sisäänrakennetun staattisen analysoijan, joka skannaa puretun koodin yleisten turvallisuusindikaattorien varalta:
- **Root-tunnistus**: tunnistaa Magisk-, SuperSU- ja yleiset root-binaarit.
- **Emulaattoritunnistus**: löytää QEMU-, Genymotion- ja tietyt järjestelmäominaisuuksien tarkistukset.
- **Arkaluonteiset tiedot**: skannaa kovakoodatut API-avaimet, tokenit ja Basic Auth -otsikot.
- **Turvaton verkotus**: merkitsee HTTP-käytön ja mahdolliset tietovuotokohdat.

*Säännöt määritellään `smali_analysis_rules.json`-tiedostossa ja ne voidaan räätälöidä tarpeisiisi.*

### APK-hallinta
- **Purku**: dekoodaa resurssit ja lähdekoodit helposti muokattavilla asetuksilla.
- **Uudelleenrakennus**: rakentaa muokatut projektit takaisin kelvollisiksi APK-tiedostoiksi.
- **Allekirjoitus**: integroitu keystore-hallinta allekirjoittaa uudelleenrakennetut APK:t asennusvalmiiksi.

## Esivaatimukset

1.  **Java Runtime Environment (JRE)**: vaaditaan `apktool`-työkalulle. Varmista, että `java` on järjestelmän `PATH`-polussa.
2.  **Apktool**: lataa `apktool.jar` osoitteesta [ibotpeaches.github.io](https://ibotpeaches.github.io/Apktool/).
3.  **Ubersign (Uber APK Signer)**: vaaditaan uudelleenrakennettujen APK:iden allekirjoittamiseen. Lataa uusin `uber-apk-signer.jar` [GitHub releases](https://github.com/patrickfav/uber-apk-signer/releases) -sivulta.
4.  **.NET 8.0 Runtime**: vaaditaan PulseAPK:n suorittamiseen Windowsissa.

## Pika-aloitus

1.  **Lataa ja rakenna**
    ```powershell
    dotnet build
    dotnet run
    ```

2.  **Määritys**
    - Avaa **Settings**.
    - Määritä `apktool.jar`-polku.
    - PulseAPK tunnistaa Java-asennuksen automaattisesti ympäristömuuttujien perusteella.

3.  **APK:n analysointi**
    - **Pura** kohde-APK Decompile-välilehdellä.
    - Siirry **Analysis**-välilehdelle.
    - Valitse purettu projektikansio.
    - Napsauta **Analyze Smali** luodaksesi turvallisuusraportin.

4.  **Muokkaa ja rakenna uudelleen**
    - Muokkaa projektikansion tiedostoja.
    - Käytä **Build**-välilehteä uuden APK:n rakentamiseen.
    - Käytä **Sign**-välilehteä tulos-APK:n allekirjoittamiseen.

## Tekninen arkkitehtuuri

PulseAPK hyödyntää selkeää MVVM-arkkitehtuuria (Model-View-ViewModel):

- **Core**: .NET 8.0, WPF.
- **Analysis**: oma regex-pohjainen staattinen analyysimoottori, jossa säännöt voidaan ladata uudelleen.
- **Services**: omistetut palvelut Apktool-integraatiolle, tiedostojärjestelmän seurannalle ja asetusten hallinnalle.

## Lisenssi

Tämä projekti on avointa lähdekoodia ja saatavilla [Apache License 2.0](LICENSE.md) -lisenssillä.

### ❤️ Tue projektia

Jos PulseAPK on sinulle hyödyllinen, voit tukea kehitystä painamalla yläreunan "Support"-painiketta.

Myös tähden antaminen repositoriolle auttaa paljon.

### Osallistuminen

Otamme mielellämme vastaan kontribuutioita! Huomioithan, että kaikkien osallistujien on allekirjoitettava [Contributor License Agreement (CLA)](CLA.md), jotta heidän työnsä voidaan jakaa laillisesti.
Pull requestin lähettämällä hyväksyt CLA:n ehdot.
