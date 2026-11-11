# Water.sim — testchecklist versie 1 (11 november 2026)

Vink af wat werkt; zet bij een fout een korte notitie (toestel, level, wat je deed) en eventueel een screenshot.

**Toestellen**
- [ ] Android-telefoon (recent)
- [ ] **Oudere Android-tablet** (snelheid!)
- [ ] iPhone (via Xcode)
- [ ] iPad (als de app op iPad komt)

**Builds**
- *Development build* (Build Settings › Development Build aan): Google's **testadvertenties** ("Test Ad"). Hiermee test je
  alles behalve de echte advertenties.
- *Releasebuild* (Development Build uit): **echte advertenties**. Alleen op een toestel dat in AdMob als
  **testtoestel** staat (AdMob › Instellingen › Testapparaten), anders kan een eigen tik als ongeldig verkeer tellen.

---

## 1. Algemeen

- [ ] Icoon op het beginscherm klopt (Android: adaptief, rond en vierkant afgerond; iOS)
- [ ] Opstarten: geen zwart of wit scherm van meer dan een paar seconden
- [ ] Startscherm: zes tegels, golf en pluimen bewegen; levelknop wisselen → animatie loopt door
- [ ] Taal NL ↔ EN: alle teksten wisselen, ook in quiz, infovak en level 5
- [ ] Niveau onderbouw / bovenbouw / XPRT: uitleg verandert mee
- [ ] Menu (⋮): Over Water.sim, Voortgang wissen (vraagt bevestiging?), Privacykeuzes alleen als nodig
- [ ] Android: terugknop/gebaar → terug naar het startscherm, en vanaf het startscherm de app uit
- [ ] Notch / afgeronde hoeken / navigatiebalk: niets valt erachter
- [ ] App naar de achtergrond en terug: gaat verder waar je was
- [ ] **Offline** (vliegtuigmodus) opstarten: alles werkt, alleen geen advertenties; geen foutmeldingen
- [ ] Telefoon én tablet: knoppen en teksten goed geschaald, niets overlapt

## 2. Advertenties

- [ ] Banner onderaan op **elke** pagina (start, levels 1–6, quiz), niets van de app eronder verborgen
- [ ] Geen grijze strook "advertentie (banner)" meer zodra de banner er is
- [ ] Interstitial: alleen na een **fout** antwoord op vraag 3 van de quiz, ongeveer de helft van de keren,
      hooguit één per minuut; sluiten → je komt terug in de quiz
- [ ] Releasebuild: advertenties zijn niet-persoonlijk (geen toestemmingsvenster, of alleen het korte venster van Google)

## 3. Per level

**Level 1 — het watermolecuul**
- [ ] Bol-staaf ↔ kalotten, δ+ δ−, beweging aan/uit, zoomen (+/− en knijpen), draaien
- [ ] Formulekaart open/dicht, wordt onthouden
- [ ] Quiz: 3 vragen, meerkeuze en getal (komma én punt, 6,022e23, 6,022·10²³), streak telt door

**Level 2 — de waterstofbrug**
- [ ] H-brug zichtbaar, labels goed leesbaar
- [ ] Quiz: na 3 vragen de tekenopdracht; slepen van H naar O/N werkt met de vinger
- [ ] Tekenopdracht: Wis, Regels, Controleer; foute bruggen rood met uitleg; punten 1/2/4 in de streak
- [ ] Alle niveaus één keer gedaan (XPRT: ook met methaan, butaan, H₂S en aceton)

**Level 3 — het ijskristal**
- [ ] Vliegen door het kristal: vloeiend (let op de oudere tablet), geen haperingen
- [ ] H-bruggen, kanalen, oneindig/los blok

**Level 4 — smelten**
- [ ] Verwarmen: temperatuur blijft 0 °C zolang er ijs is; kromme loopt mee
- [ ] Aantal H-bruggen per molecuul (≈ 3,5 bij 1 °C, ≈ 2,9 bij 90 °C)
- [ ] Snelheid op de oudere tablet (dit is het zwaarste level)

**Level 5 — IJs op aarde**
- [ ] IJs drijft: zoet water, zeewater, alcohol; smelten; waterniveau
- [ ] Poolkappen: landijs en zee-ijs smelten, zeespiegel
- [ ] Meertje: klimaat kiezen (alle 11), jaar aan/uit, geen zon, strenge winter, schaatsers bij 5/10/15 cm

**Level 6 — Ontdek**
- [ ] Lijst met onderwerpen, "binnenkort" duidelijk

## 4. Snelheid (oudere tablet)

- [ ] Levels 3 en 4 lopen vloeiend (richtwaarde ≥ 30 beelden per seconde)
- [ ] Toestel wordt niet heet en de batterij loopt niet hard leeg na 10 minuten
- [ ] Opstarttijd: ____ s

## 5. Notities

| Toestel | Level | Wat ging er mis | Screenshot |
|---|---|---|---|
| | | | |
