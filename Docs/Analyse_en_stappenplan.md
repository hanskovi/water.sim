# Water.sim — analyse en stappenplan

*Opgesteld 8 november 2026. Nog niets gebouwd; dit is de basis voor de keuzes.*

## Uitgangspunt

- **Prototype:** `Docs/prototype/ijskristal-3d.html` (Three.js), live op
  https://claude.ai/artifact/Cny59fkcUFT2ctQcsAQr1q
  - ijs Ih met ijsregels, oneindig kristal, smelten/bevriezen met 0 °C-plateau,
    vloeistof met wisselende H-bruggen, neon-H-bruggen, vliegmodus.
- **Unity-project:** 6000.3.11f1, Universal 3D (URP 17.3), Input System 1.19 —
  gelijk aan C.chem.
- **C.chem** levert herbruikbare onderdelen: interface (uGUI + TextMesh Pro, Ubuntu-font),
  sterren/voortgang, leaderboards (Unity Services), meldingen, advertenties,
  5 talen, store-pijplijn.

---

## 1. De hele simulatie in Unity

### Wat er over moet

| Prototype (JavaScript) | Unity (C#) |
|---|---|
| IJs Ih-rooster + ijsregels (Euler-oriëntatie) | `IceLattice` — pure C#, testbaar met unit tests |
| Fasewisseling (smeltfront, 0 °C-plateau) | `PhaseModel` — liefst energiegebaseerd (zie §2) |
| Vloeistof: zachte bollen, celrooster, H-bruggen die draaien | `LiquidSim` — C# met Jobs + Burst |
| Oneindig kristal (dichtstbijzijnde kopie bij camera) | in de shader of compute shader: posities verschuiven op de GPU |
| InstancedMesh voor O, H, staafjes, bruggen | `Graphics.RenderMeshInstanced` / `RenderMeshIndirect` |
| Neon-H-bruggen (kern + additieve gloed) | Shader Graph-materiaal + URP **Bloom** (op telefoon); goedkope gloed-mesh als fallback |
| Nevel op afstand | eigen nevel in de shaders (afstand tot camera, niet diepte) |
| Vliegbesturing (WASD, slepen, knoppen) | Input System: toetsenbord, muis, aanraakjoystick, gamepad |

### Architectuur

```
Assets/Scripts/
  Sim/        (assembly WaterSim.Sim, géén Unity-afhankelijkheid)
              Constanten, IJsrooster, IJsregels, Fasemodel, Vloeistof, Brugzoeker
  Weergave/   Moleculentekenaar, Brugtekenaar, OneindigRooster, Nevel
  Besturing/  Vliegcamera, Aanraakjoystick, (later) XR-rig
  UI/         Levelscherm, opdrachten, sterren — overgenomen uit C.chem
Assets/Data/      ScriptableObjects per ijssoort (§3), levels, teksten
Assets/Tests/EditMode/   unit tests voor alles in Sim/
```
*(In fase 0 aangemaakt; klassennamen in het Nederlands, zoals in C.chem.)*

- **Simulatie en weergave strikt gescheiden.** Dan kan dezelfde simulatie draaien op telefoon,
  tablet, WebGL en VR, en kun je de natuurkunde testen zonder Unity te starten.

### Wat Unity beter kan dan de browser

- **Meer moleculen:** 5.000–15.000 in plaats van 1.800, dus verder zicht en grotere "bekers".
- **Echte bloom-gloed, schaduwen en zachte belichting.**
- **Soepeler op tablets:** native app in plaats van een webpagina.

### Aandachtspunten

- **Chromebooks.** Veel scholen gebruiken Chromebooks. Daarop draait een native Unity-app
  alleen als Android-app (als de school Play Store toestaat). Alternatief: een **WebGL-build**.
  Let op: compute shaders en Burst werken daar niet, dus houd een CPU-route over.
- **Mobiele prestaties.** Bloom kost GPU-tijd. Maak het daarom instelbaar (hoog / laag).

---

## 2. Kokend water

### Natuurkunde die de app moet laten zien

- **Twee plateaus in de opwarmcurve:** 0 °C (smelten) en 100 °C (koken).
- **Het kookplateau duurt ongeveer 6,8× zo lang als het smeltplateau.** Verdampen kost 2257 J/g,
  smelten 334 J/g. Dat kan de app alleen kloppend laten zien met een **energiemodel**:
  - warmte toevoeren in joule in plaats van een vaste smeltsnelheid;
  - soortelijke warmte: ijs 2,1, water 4,18 en stoom 2,0 J/(g·K).

  Dan ontstaat de juiste opwarmcurve vanzelf. Dit vervangt de huidige smeltsnelheid.
- **Verdampen gebeurt ook onder 100 °C.** De snelste moleculen ontsnappen aan het oppervlak.
- **Koken betekent belletjes ín de vloeistof.** Dat gebeurt als de dampdruk 1 atm wordt.
- **Stoom is bijna leeg:** ongeveer 1.700× zoveel volume per molecuul als water. De gemiddelde
  afstand tussen moleculen is ongeveer 37 Å, verder dan de nevel reikt. In de oneindige
  weergave zie je dus bijna niets. Dat is precies de les, maar het vraagt een aparte weergave.

### Voorgestelde weergaven

1. **Beker (vervangt "Los blok").**
   - Vloeistof met zwaartekracht en een vrij oppervlak, met daarboven open ruimte.
   - Je ziet de krimp bij smelten, verdamping aan het oppervlak en bij 100 °C belletjes.
   - Ontsnapte moleculen vliegen snel weg en trekken een kort spoor.
2. **Gas, oneindig.**
   - Weinig moleculen die snel rechtdoor vliegen, met heel af en toe een kort paar.
   - Met een tijdvertragingsfactor in beeld ("1 s = 1 ps").
   - Met een knop "toon op ware afstand" en een knop "samengedrukt (niet op schaal)".
3. **Fasediagram als bedieningspaneel.**
   - Een druk-temperatuurdiagram waarop je met je vinger een punt schuift.
   - Laat het tripelpunt zien (0,01 °C; 611 Pa), koken op de Mount Everest (≈ 70 °C) en
     in een snelkookpan (≈ 120 °C).
   - Dit diagram is ook de toegang tot de andere ijssoorten (§3).

### Gevolgen voor de techniek

- **Vrij oppervlak:** de vloeistofsimulatie heeft zwaartekracht, een zwakke aantrekking
  (cohesie) en een open bovenkant nodig.
- **Gas:** gasmoleculen gebruiken een eigen, eenvoudige botsingssimulatie (ballistisch),
  los van de vloeistofsimulatie.

---

## 3. Andere soorten ijs

Er zijn meer dan 20 bekende ijsfasen. Een goede selectie voor school:

| IJs | Bijzonder | Dichtheid (g/cm³) | Drijft? |
|---|---|---|---|
| **Ih** | gewoon ijs, zeshoekig | 0,92 | ja |
| **Ic** | kubisch: zelfde lagen, andere stapeling; komt hoog in de atmosfeer voor | 0,92 | ja |
| **XI** | Ih met geordende H-atomen, onder 72 K | 0,93 | ja |
| **III / V / VI** | hogedrukijs, dichter dan water | 1,16 / 1,24 / 1,31 | nee |
| **VII** | twee in elkaar geschoven roosters, in ijsmanen en exoplaneten | ≈ 1,6 | nee |
| **Amorf ijs (LDA)** | geen kristal; de meest voorkomende vorm in het heelal | 0,94 | ja |
| **Methaanhydraat** | "brandend ijs": methaan opgesloten in kooien van water | ≈ 0,9 | ja |

- **Didactische kern:** *niet alle ijs drijft.* Hogedrukijs is dichter dan water,
  omdat het rooster niet meer open is.
- **Gegevens: GenIce2** (open source, MIT). Dit programma maakt van meer dan 100 ijsstructuren
  eenheidscellen, inclusief de H-atomen volgens de ijsregels.
  - Werkwijze: genereer de cellen eenmalig, zet ze om naar JSON en laad ze in Unity
    als ScriptableObject.
  - Het rooster in de app moet dan ook schuine (trikliene) cellen aankunnen. De
    "dichtstbijzijnde kopie bij de camera" werkt daar net zo, maar dan in celcoördinaten.
- **Navigatie:** de gebieden in het fasediagram (§2) zijn de knoppen naar de ijssoorten.

---

## 4. VR-bril

### Mogelijkheden

| Bril | Route in Unity | Opmerking |
|---|---|---|
| **Meta Quest 3 / 3S** | OpenXR + XR Interaction Toolkit (of Meta XR SDK) | zelfstandig, betaalbaar, meest gebruikt op scholen |
| Apple Vision Pro | PolySpatial / visionOS | duur; Unity Pro-licentie nodig (te controleren) |
| Pico 4 (Ultra) | OpenXR | vergelijkbaar met Quest |

### Aandachtspunten

- **Bewegingsziekte.** Vrij vliegen in VR maakt veel mensen misselijk. Oplossingen:
  - langzaam bewegen, met een donkere rand (vignet) tijdens het vliegen;
  - verplaatsen door te teleporteren of in stapjes;
  - **of andersom: het kristal als model in je handen** dat je groot en klein knijpt.
    Op het grootste formaat sta je "midden in" het rooster.
- **Prestaties.** In VR moet elk beeld twee keer getekend worden, met 72–90 beelden per seconde.
  Bloom is dan te duur. Gebruik de goedkope gloed-mesh uit het prototype.
- **Interactie.**
  - Met handtracking een molecuul vastpakken en zijn 4 bruggen zien oplichten.
  - De temperatuur instellen met een draaiknop in de ruimte.
- **Klassikaal (later).** Een docent leidt een rondleiding en leerlingen kijken mee. Daarvoor
  zijn een multiplayer-verbinding en apparaatbeheer nodig (bijvoorbeeld ManageXR).

### Goedkope eerste test

Three.js ondersteunt WebXR. Het browserprototype kan met een knop "Bekijk in VR" in de
browser van een Quest draaien, in een paar uur werk. Zo test je bewegingsziekte en het
"wauw"-effect voordat je in Unity aan VR begint.

### Advies

- Ontwerp de Unity-app vanaf het begin **XR-klaar**: camera-rig en invoer als losse laag.
- Bouw VR als **aparte fase** na de telefoon- en tablet-versie.

---

## Stappenplan

| Fase | Inhoud | Klaar als |
|---|---|---|
| **0. Opzet** | git + .gitignore, mappenstructuur, CLAUDE.md met huisregels, prototype als referentie | project staat in git, structuur staat |
| **1. Kern** | `IceLattice` (Ih + ijsregels) in pure C#, met unit tests | tests: 4 bruggen per molecuul, 2 H per O, dichtheid 0,917 |
| **2. Weergave** | instanced O/H/staafjes, neon-H-bruggen, bloom, nevel op afstand, oneindig kristal, vliegbesturing (muis, toetsen, aanraken) | gelijk aan prototype v6 op iPad en Android, ≥ 60 fps |
| **3. Smelten** | energiemodel, vloeistof (Burst), bekerweergave met krimp | opwarmcurve klopt; H-bruggen ≈ 3,5 → 2,9 |
| **4. App-schil** | levelscherm, leagues, opdrachten, sterren, 5 talen — uit C.chem | level 3 en 4 speelbaar met opdrachten |
| **5. Koken** | verdamping, belletjes, gasweergave, fasediagram met druk | plateau 100 °C ≈ 6,8× langer dan 0 °C |
| **6. IJssoorten** | GenIce2 → JSON → ScriptableObjects; trikliene cellen; "zinkt of drijft?" | 5–8 ijssoorten via het fasediagram |
| **7. VR** | WebXR-test met prototype → Unity OpenXR-build voor Quest | rondleiding zonder misselijkheid, ≥ 72 fps |
| **8. Uitbrengen** | store-materiaal, privacy, testen met leerlingen | in App Store / Play Store |

- **Volgorde:** fase 7 (de WebXR-test) kan al vroeg, los van de rest.
- **Grootste risico's:**
  - prestaties op oudere schooltablets → vroeg testen in fase 2;
  - de gasweergave didactisch goed krijgen → eerst een papieren of browserschets in fase 5.

---

## Voortgang

| Fase | Status |
|---|---|
| 0. Opzet | klaar |
| 1. Kern (ijsrooster + ijsregels) | klaar — 21 tests |
| 2. Weergave | klaar — 60 fps op Android |
| 3. Smelten | klaar — energiemodel, vloeistof, los blok; werkt op Android |
| 4. App-schil | bezig — startscherm, simulatiescherm, icoon en tegels klaar; past op telefoons en tablets |

### Wat er nog moet gebeuren (bijgewerkt 9 november 2026)

**Fase 4 afmaken (app-schil)**
1. ~~Simulatiescherm in glas-en-ijsstijl~~ — klaar (donker waterglas, oranje actieve knoppen).
2. **Opdrachten (deel B):** eerst samen de inhoud vastleggen (per level × league: vraagsoort,
   tekst, antwoord, tip), daarna bouwen: tipbalk, antwoordvak/meerkeuze, check, sterren opslaan.
   De grootste stap van fase 4.
3. **Uitleg-schermpjes per level** (kort, met plaatje) en de teksten nalopen (NL + EN).
4. **Afronden:** fase 4 samenvoegen met main; test op Android (en iPad).

**Fase 5 · Koken**
5. Verdampen aan het oppervlak, belletjes, plateau bij 100 °C (energiemodel bestaat al),
   gasweergave (bijna leeg, met tijdvertraging), fasediagram met druk. Level 6 speelbaar maken.

**Levels 1 en 2**
6. Level 1 (één molecuul: hoek, δ+/δ−, dipool) en level 2 (twee moleculen, de H-brug) als eigen
   kleine 3D-scènes. Hergebruikt de weergave van het kristal.

**Fase 6 · Andere ijssoorten**
7. GenIce2 → eenheidscellen (Ic, XI, III, V, VI, VII, amorf, methaanhydraat); schuine cellen in
   het rooster; "zinkt of drijft?" via het fasediagram. Past in de XPRT League.

**Fase 7 · VR**
8. Eerst een snelle test (browserprototype in de Quest-browser), daarna een OpenXR-build.

**Fase 8 · Uitbrengen**
9. Store-materiaal (icoon is klaar), privacytekst, testen met leerlingen, naam vrij in de stores.

Doorlopend: de punten onder "Later afstemmen" (kalotten, details smelten).

### Later afstemmen (bewust uitgesteld)

- ~~Kalotten~~ — opgelost: berekende bollen (impostors), CPK-kleuren, donkere rand, Bondi-stralen.
- ~~Telefoontest~~ — 60 fps op Android met MSAA 4× en kalotten (oneindig kristal); het losse blok
  met kalotten nog even meten.
- **Liggende iPad:** de opmaak schaalt goed, maar oogt klein en de nevel rond de kolom is te wazig.
  Eventueel een eigen opmaak voor liggend (twee kolommen).
- **Details van het smelten en de vloeistof:** hoe het smeltfront eruitziet, hoe snel moleculen
  bewegen, wat er precies in beeld gebeurt bij de overgang ijs ↔ water.
- **Eerste keer laden in de editor:** shaders compileren dan even (in een build niet).
- **Testpaneel en snelheidsmeter** zijn tijdelijk; de echte bediening komt in fase 4.

## Gemaakte keuzes (8 november 2026)

1. **Platforms:** iOS + Android (telefoons en tablets), zoals C.chem. Geen WebGL-build.
   Chromebooks alleen via Android-apps. VR (Quest) blijft een latere fase.
   → Burst, Jobs en compute shaders mogen gebruikt worden.
2. **Verdienmodel:** zoals C.chem: gratis met advertenties.
   → AdManager, leaderboards en meldingen uit C.chem overnemen.
3. **Leagues:**
   - **Onderbouw:** fasen en het deeltjesmodel;
   - **Bovenbouw:** H-bruggen, dichtheid en smeltwarmte;
   - **XPRT:** ijssoorten, fasediagram en energie.
4. **Talen:** eerst NL + EN. Teksten wel vanaf het begin via een vertaalsysteem,
   zodat DE, FR en ES later makkelijk toe te voegen zijn.

5. **Naam:** **Water.sim** (gelijk aan de projectmap).
   - **Logo:** "Water." groot en "sim" klein, zoals bij "C.chem", met "H₂O" (kleine 2)
     als beeldmerk ernaast.
   - **App-icoon:** het H₂O-molecuul of een zeshoek met een gloeiende H-brug, plus
     de tekst "H₂O". De subscript-2 moet ook op een klein icoon (60 px) leesbaar blijven,
     dus groot en vet.
   - **In de store:**
     - naam "Water.sim"; ondertitel bijvoorbeeld "ijs, water & stoom in 3D";
     - zoekwoorden onder andere: h2o, ijs, waterstofbrug, fasen, dichtheid.
   - **Nog te controleren:** of de naam vrij is in de App Store en de Play Store.
