# Water.sim — voorstel opdrachten (levels 3, 4 en 5)

Voorstel om samen door te nemen en aan te passen. Pas als de inhoud vastligt, bouwen we het scherm.
Alle antwoorden zijn nagerekend met de getallen die de app zelf gebruikt (`Constanten.cs`, Binas).

## Opzet

- **Per level × league 3 opdrachten**, van makkelijk naar moeilijk. Levels 3, 4 en 5 samen: 27 opdrachten.
- **Drie soorten opdrachten:**
  - **MK** · meerkeuze, met 4 antwoorden in een willekeurige volgorde.
  - **GT** · een getal invullen, met een marge (op een cijfertoetsenbord met een komma).
  - **DOE** · iets doen in de simulatie. De app ziet zelf wanneer het gelukt is (bijvoorbeeld de
    temperatuur, het percentage gesmolten, of waar de camera is).
- **Tip:** onder de opdracht staat een tipknop. Na twee foute antwoorden gaat de tip vanzelf open.
- **Sterren (voorstel):** 1 ster per opdracht, als het antwoord bij de eerste of tweede poging goed is.
  Daarna verschijnt de uitleg en telt de opdracht als gedaan, maar zonder ster. Opnieuw spelen mag
  altijd.
  - Per league zijn er 6 levels × 3 = **18 sterren**. De balk op het startscherm toont de sterren van
    de gekozen league (x/18 in plaats van x/30).
- **Uitleg na het goede antwoord:** 1 à 2 zinnen. Dat is het leermoment.

---

## Level 3 · Het ijskristal

*Simulatie: het oneindige kristal onder 0 °C. Vliegen, kalotten of bol-staaf, H-bruggen aan/uit.*

### Onderbouw

| # | Soort | Opdracht | Antwoord | Tip |
|---|---|---|---|---|
| 1 | DOE | Vlieg het ijs in en zoek een van de lege kanalen. Vlieg erdoorheen. | camera in een kanaal (dicht bij de as), 2 s | De kanalen lopen kaarsrecht. Draai tot je erdoorheen kunt kijken. |
| 2 | MK | Uit welke atomen bestaat één watermolecuul? | **1 O en 2 H** · 2 O en 1 H · 1 O en 1 H · 2 O en 2 H | Rood is zuurstof (O), wit is waterstof (H). |
| 3 | MK | Hoeveel moleculen vormen samen één zeshoek van de honingraat? | 4 · **6** · 8 · 12 | Tel de rode bollen rond één kanaal. |

### Bovenbouw

| # | Soort | Opdracht | Antwoord | Tip |
|---|---|---|---|---|
| 1 | GT | Met hoeveel buren is elk molecuul in ijs verbonden via een waterstofbrug? | **4** | Zet de H-bruggen aan en tel de blauwe streepjes rond één O-atoom. |
| 2 | MK | Een waterstofbrug loopt van… | **een H-atoom (δ+) van het ene molecuul naar een O-atoom (δ−) van een ander** · een O-atoom naar een O-atoom · een H-atoom naar een H-atoom · een O-atoom naar een H-atoom in hetzelfde molecuul | Denk aan de ladingen δ+ en δ−. |
| 3 | MK | Waarom zit er zoveel lege ruimte in ijs? | **Elk molecuul zit vast aan 4 buren onder vaste hoeken (tetraëder, ≈ 109°); zo ontstaat een open rooster** · Er zit lucht in het ijs · De moleculen stoten elkaar af · De moleculen trillen te hard | Kijk naar de hoeken tussen de 4 H-bruggen van één molecuul. |

### XPRT League

| # | Soort | Opdracht | Antwoord | Tip |
|---|---|---|---|---|
| 1 | GT | Een ijskristal bestaat uit 1000 moleculen. Hoeveel waterstofbruggen zitten er in totaal in? (randen negeren) | **2000** | Elk molecuul heeft er 4, maar elke brug hoort bij twee moleculen. |
| 2 | GT | Bereken de dichtheid van ijs (g/cm³) uit het rooster. Een cel van 4,52 × 7,83 × 7,36 Å bevat 8 moleculen. | **0,919** (marge 0,005) | m = 8 × 18,015 / N_A g; 1 Å³ = 10⁻²⁴ cm³. |
| 3 | GT | De H-atomen in ijs kunnen op heel veel manieren liggen (de ijsregels). Volgens Pauling zijn er per molecuul ongeveer 1,5 mogelijkheden. Bereken de restentropie S = R ln 1,5 in J/(mol·K). | **3,37** (marge 0,05) | R = 8,314 J/(mol·K). Gemeten: ≈ 3,4 J/(mol·K). |

---

## Level 4 · Smelten en bevriezen

*Simulatie: het oneindige kristal met de schuif voor de omgevingstemperatuur. In beeld: de temperatuur,
het percentage gesmolten en het aantal H-bruggen per molecuul.*

### Onderbouw

| # | Soort | Opdracht | Antwoord | Tip |
|---|---|---|---|---|
| 1 | DOE | Laat al het ijs smelten. | 100% gesmolten | Zet de omgeving boven 0 °C, bijvoorbeeld op +20 °C. |
| 2 | MK | Wat doet de temperatuur terwijl het ijs smelt? | **Die blijft 0 °C** · Die stijgt gelijkmatig · Die daalt · Die springt meteen naar de omgevingstemperatuur | Laat het opnieuw smelten en kijk naar het grote getal. |
| 3 | MK | Wat gebeurt er met de moleculen als ijs smelt? | **Ze laten hun vaste plek los en schuiven langs elkaar** · Ze worden groter · Ze worden kleiner · Ze vallen uit elkaar in H en O | Zet Beweging aan en vergelijk ijs met water. |

### Bovenbouw

| # | Soort | Opdracht | Antwoord | Tip |
|---|---|---|---|---|
| 1 | MK | Tijdens het smelten blijft de temperatuur 0 °C. Waar gaat de warmte dan heen? | **Naar het verbreken van waterstofbruggen** · Naar het verwarmen van het water · Naar het splitsen van moleculen in H en O · Die gaat verloren aan de omgeving | Kijk tijdens het smelten naar het aantal H-bruggen. |
| 2 | GT | Maak water van ongeveer 1 °C. Hoeveel H-bruggen heeft een molecuul dan gemiddeld? | **3,5** (marge 0,2; het model geeft ≈ 3,45) | In ijs zijn het er 4. Zet de omgeving net boven 0 °C. |
| 3 | DOE | Houd een mengsel van ijs en water in stand: tussen 40% en 60% gesmolten, 5 seconden lang. | gesmolten 40–60%, 5 s | Bij welke omgevingstemperatuur smelt en bevriest er niets? |

### XPRT League

| # | Soort | Opdracht | Antwoord | Tip |
|---|---|---|---|---|
| 1 | GT | Hoeveel warmte (J) is er nodig om 10 g ijs van −20 °C te laten smelten tot water van 0 °C? | **3760** (marge 40) | Q = m·c·ΔT + m·L, met c(ijs) = 2,1 J/(g·K) en L = 334 J/g. |
| 2 | GT | De warmte wordt in gelijke porties toegevoerd. Hoeveel keer zo lang duurt het kookplateau als het smeltplateau? | **6,8** (marge 0,1) | Verdampingswarmte 2257 J/g, smeltwarmte 334 J/g. |
| 3 | MK | Bij 1 °C heeft een molecuul ≈ 3,5 H-bruggen, bij 90 °C ≈ 2,9. Hoe komt dat? | **Snellere moleculen verbreken bruggen vaker dan er nieuwe ontstaan (dynamisch evenwicht)** · De moleculen worden kleiner · Water wordt bij hoge temperatuur minder polair · De bruggen worden langer, maar blijven allemaal bestaan | Zet Beweging aan en vergelijk 1 °C met 90 °C. |

---

## Level 5 · Waarom ijs drijft

*Simulatie: het losse blok. Bij smelten zakt de bovenkant. In beeld: de dichtheid ρ.*

### Onderbouw

| # | Soort | Opdracht | Antwoord | Tip |
|---|---|---|---|---|
| 1 | MK | Wat heeft de grootste dichtheid: ijs of water? | **Water** · IJs · Ze zijn even groot · Hangt af van de hoeveelheid | IJs drijft. Wat zegt dat? |
| 2 | DOE + MK | Laat het blok helemaal smelten. Wat gebeurt er met het volume? | DOE: 100% gesmolten → MK: **Het wordt kleiner** · Het wordt groter · Het blijft gelijk | Kijk naar de bovenkant van het blok. |
| 3 | MK | Waarom kan een volle fles water in de vriezer knappen? | **Water zet uit als het bevriest** · Water krimpt als het bevriest · Het glas krimpt in de kou · Er ontstaat gas | Vergelijk het blok voor en na het smelten. |

### Bovenbouw

| # | Soort | Opdracht | Antwoord | Tip |
|---|---|---|---|---|
| 1 | GT | Lees de dichtheid af van ijs (0,917) en van water van 0 °C (1,000). Hoeveel procent groter is het volume van ijs? | **9** (marge 0,5) | V = m/ρ. Neem 1 g. |
| 2 | GT | Welk deel van een ijsblok in zoet water zit onder water (in %)? | **92** (marge 1) | Drijven: het gewicht van het ijs = het gewicht van het verplaatste water, dus deel = ρ(ijs) / ρ(water). |
| 3 | MK | Waarom heeft ijs een kleinere dichtheid dan water? | **De H-bruggen houden in ijs een open rooster met lege kanalen; in water breken bruggen en schuiven moleculen dichter op elkaar** · IJs bevat lucht · Moleculen in ijs zijn groter · Moleculen in water zijn zwaarder | Vergelijk de kanalen in level 3 met het water hier. |

### XPRT League

| # | Soort | Opdracht | Antwoord | Tip |
|---|---|---|---|---|
| 1 | DOE + GT | Zoek met de schuif de temperatuur waarbij water de grootste dichtheid heeft. | **4 °C** (marge 0,5; het model gebruikt 3,98 °C) | Lees ρ af bij 0, 2, 4, 6 en 8 °C. |
| 2 | GT | Een ijsberg drijft in zeewater (ρ = 1,025 g/cm³). Welk deel zit onder water (in %)? | **89** (marge 1) | Hetzelfde als in zoet water, maar met een andere ρ(water). |
| 3 | MK | Waarom bevriest een meer van bovenaf, terwijl het water op de bodem ≈ 4 °C blijft? | **Water van 4 °C is het dichtst en zakt naar de bodem; kouder water blijft boven en bevriest; het ijs drijft en isoleert** · Warm water zakt altijd · IJs zinkt en smelt op de bodem · De bodem verwarmt het water | Kijk wat de dichtheid doet tussen 0 en 8 °C. |

---

## Wat er in de app nog moet veranderen

1. **ρ met meer decimalen** (alleen in level 5, XPRT). Bij 3 decimalen staat er tussen 0 en 8 °C
   steeds 1,000, dus dan is het maximum bij 4 °C niet te vinden. Met 5 decimalen kan het wel:
   0,99987 bij 0 °C, 1,00000 bij 4 °C, 0,99988 bij 8 °C.
2. **Kanaal-detectie** voor opdracht 3.1. Die zit al in het model (`IJsrooster.DichtstbijzijndKanaal`).
3. **Het percentage gesmolten en de H-bruggen** worden al uitgelezen. Die zijn genoeg voor de
   DOE-opdrachten van level 4 en 5.
4. **Teksten** gaan via het vertaalsysteem (NL + EN), net als nu.

## Vragen voor jou

1. Klopt het niveau per league ongeveer? Onderbouw = klas 2–3, Bovenbouw = havo/vwo 4–6,
   XPRT = verder dan het examen.
2. Wil je de sterrenregel zo, of liever 3 sterren per opdracht (3 bij de eerste poging, 2 bij de tweede,
   1 met tip)?
3. Moeten de leerlingen een league "vrijspelen", of mogen ze er meteen elke kiezen?
4. Wil je opdrachten schrappen, vervangen of toevoegen?
