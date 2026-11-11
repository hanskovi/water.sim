# Water.sim — privacy en store-gegevens (concept, 11 november 2026)

Wat Water.sim met gegevens doet, wat er daarvoor in het gedeelde privacystatement (www.conceptcraft.nl) moet
veranderen, en wat je invult in de Play Console en App Store Connect.
Basis: `WordCrumble/Docs/Privacystatement.md` (het gedeelde statement) en `C.chem/Docs/Privacy_CChem.md` (zelfde opzet).

---

## 1. Wat Water.sim met gegevens doet

| Wat | Waar | Wanneer |
|---|---|---|
| Instellingen en voortgang: taal, laatst gekozen level en niveau, klimaat (level 5), sterren, quizstreaks per level en niveau, open/dicht van de formulekaart | alleen op het toestel (PlayerPrefs) | altijd |
| Advertenties (AdMob): advertentie-ID, IP (globale locatie), toestel- en advertentie-interacties, diagnostiek | Google | altijd |

- **Geen account, geen ranglijst, geen meldingen, geen eigen analyse.** De app maakt zelf geen internetverbinding;
  alleen de AdMob-SDK doet dat.
- **Advertenties alleen niet-persoonlijk.** Water.sim is voor scholieren vanaf 14 jaar. Alle gebruikers krijgen de
  tienerbehandeling van Google (`AgeRestrictedTreatment.Teen`, en "onder de leeftijd van toestemming" voor het
  toestemmingsvenster): geen gepersonaliseerde advertenties, inhoud hooguit rating T. Daardoor verschijnt het
  toestemmingsvenster meestal niet.
- **Geen tracking-vraag op iOS (ATT).** De app vraagt niet om de advertentie-ID (IDFA) en volgt gebruikers niet.
- Verwijderen: app verwijderen (alle lokale gegevens zijn dan weg). Menu (⋮) › "Voortgang wissen" wist de
  voortgang zonder de app te verwijderen.

---

## 2. Aanvulling privacystatement (www.conceptcraft.nl)

Drie aanpassingen in het gedeelde statement; de rest blijft zoals het is.

### 2a. In de inleiding: Water.sim aan de lijst toevoegen

NL — "zoals WordCrumble, Atom.sim, Chemistry Formulas & Names en C.chem" wordt:
> zoals WordCrumble, Atom.sim, Chemistry Formulas & Names, C.chem en Water.sim

EN — "such as WordCrumble, Atom.sim, Chemistry Formulas & Names and C.chem" wordt:
> such as WordCrumble, Atom.sim, Chemistry Formulas & Names, C.chem and Water.sim

### 2b. Nieuwe alinea (na de alinea's over C.chem)

**NL**

> **Water.sim**
> Water.sim bewaart alleen je instellingen en voortgang op je eigen toestel: de taal, het level en niveau dat je
> het laatst koos, het klimaat in level 5, je sterren en je quizreeksen (streaks). Deze gegevens verlaten je toestel
> niet en worden verwijderd als je de app verwijdert. Via het menu (⋮ → Voortgang wissen) wis je ze ook zonder de
> app te verwijderen.
> Water.sim is gemaakt voor scholieren vanaf 14 jaar. Daarom toont Water.sim **alleen niet-gepersonaliseerde
> advertenties**, die geschikt zijn voor tieners. Google kan ook dan gegevens gebruiken om advertenties te tonen en te
> meten (zie "Advertenties" hieronder), maar niet om ze op jou af te stemmen. De app vraagt niet om je toestemming
> voor tracking en volgt je niet.

**EN**

> **Water.sim**
> Water.sim only stores your settings and progress on your own device: the language, the level and difficulty you
> last chose, the climate in level 5, your stars and your quiz streaks. This data does not leave your device and is
> deleted when you delete the app. You can also clear it via the menu (⋮ → Clear progress) without deleting the app.
> Water.sim is made for students aged 14 and over. That is why Water.sim **only shows non-personalized ads** that
> are suitable for teenagers. Google may still use data to show and measure ads (see "Advertising" below), but not
> to tailor them to you. The app does not ask for permission to track you and does not track you.

### 2c. Onder "Toestemming en je keuzes", eerste punt aanvullen

NL — na "…'Privacy-instellingen voor advertenties')." toevoegen:
> In Water.sim krijg je altijd niet-gepersonaliseerde advertenties; is er toch een keuze nodig, dan staat die in het
> menu (⋮ → Privacykeuzes (advertenties)).

EN — na "…'Ad privacy settings')." toevoegen:
> In Water.sim you always get non-personalized ads; if a choice is needed after all, you find it in the menu
> (⋮ → Privacy choices (ads)).

Bovenaan de datum bijwerken ("Laatst bijgewerkt"). Zet daarna de **URL** van het statement in de Play Console en
App Store Connect (zie hieronder).

**Nog te doen in de app:** Google Play vraagt de link naar het privacystatement óók ín de app (de AdMob-SDK verzamelt
gegevens). Water.sim heeft die link nog niet. Zodra de URL bekend is, komt er in het menu (⋮) een knop
"Privacystatement" / "Privacy statement" die de pagina opent.

---

## 3. Google Play Console → App-content (Water.sim)

### Gegevensveiligheid (Data safety)

**Verzamelt of deelt je app gegevens?** Ja (alleen via AdMob).
**Versleuteld tijdens overdracht?** Ja (HTTPS).
**Kunnen gebruikers verwijdering aanvragen?** Niet van toepassing: de app verzamelt zelf niets; lokale gegevens
verdwijnen met de app. (Kies het antwoord zoals bij Atom.sim / Chemistry Formulas & Names, die ook geen account hebben.)

| Gegevenstype (Play) | Verzameld | Gedeeld | Verplicht/optioneel | Doel |
|---|---|---|---|---|
| Apparaat- of andere ID's (advertentie-ID, via AdMob) | ja | ja | verplicht | Advertenties of marketing, Analyse |
| Locatie → **Globale locatie** (IP, via AdMob) | ja | ja | verplicht | Advertenties of marketing |
| App-activiteit → **App-interacties** (via AdMob) | ja | ja | verplicht | Advertenties of marketing, Analyse |
| App-info en prestaties → **Diagnostiek** (via AdMob) | ja | ja | verplicht | Analyse |

Houd de antwoorden gelijk aan die van je andere apps met AdMob.

### Overig in App-content
- **Privacybeleid**: URL van het statement op www.conceptcraft.nl.
- **Advertenties**: "Ja, mijn app bevat advertenties".
- **Toegang tot de app**: alle functies zonder inloggen beschikbaar.
- **Overheidsapps / financiële functies / gezondheid**: nee.
- **Doelgroep en content**: zie §5.
- **Machtigingen**: alleen internet (AdMob). Geen meldingen, geen locatie.

### Contentclassificatie (IARC-vragenlijst)
- Categorie: **Referentie, nieuws of educatief**.
- Geweld, angst, seks, grof taalgebruik, drugs, gokken: **nee**.
- Gebruikers kunnen met elkaar communiceren / content delen: **nee**.
- Locatie delen: **nee**. Digitale aankopen: **nee**.
- Verwachte uitkomst: PEGI 3 / Iedereen. (Advertenties tellen hier niet mee; die begrenst de app zelf tot rating T.)

---

## 4. App Store Connect → App-privacy

- **Data Used to Track You**: geen (geen ATT, geen IDFA, niet-gepersonaliseerde advertenties).
- **Data Not Linked to You** (via AdMob):
  - Identifiers → **Device ID** — Third-Party Advertising, Analytics
  - Usage Data → **Advertising Data** — Third-Party Advertising, Analytics
  - Location → **Coarse Location** — Third-Party Advertising
  - Diagnostics → **Crash Data / Performance Data** — Analytics
- Geen account, dus geen "account verwijderen" nodig.
- **Leeftijdsclassificatie**: alle vragen "None"; uitkomst 4+. Zet bij "Made for Kids" **niet** aan (de app is voor
  14+; met Made for Kids gelden strengere advertentieregels).

---

## 5. Doelgroep (Play Console → Doelgroep en content)

- Kies **13–15, 16–17 en 18+**, niet de groepen onder 13. Dan valt Water.sim niet onder het Families-programma.
- Beschrijving en screenshots niet op kinderen onder 13 richten (geen "voor kinderen" o.i.d.).
- "Kan de app onbedoeld kinderen aantrekken?": **nee** (educatieve app voor middelbare scholieren, geen
  spelfiguren of kinderlijke stijl).
- In de app is dit al geregeld: tienerbehandeling bij AdMob, inhoud hooguit rating T, geen persoonlijke advertenties.
