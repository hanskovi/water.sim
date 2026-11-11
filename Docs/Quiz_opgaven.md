# Water.sim — quizopgaven (voorstel)

Per level (knop) en per niveau 10 opgaven: 6 × 3 × 10 = 180. Het is een voorstel om uit te kiezen,
te schrappen en aan te vullen. Het bouwen komt daarna.

- **Soorten opgaven:**
  - **MK**: meerkeuze. Het goede antwoord staat vet, daarna volgen de afleiders.
  - **GT**: een getal invullen, met de marge tussen haakjes.
  - **DOE**: iets laten gebeuren in de simulatie. Deze kunnen alleen bij levels die al een simulatie hebben.
- **Gegevens:** Binas / `Constanten.cs`.
  - c(water) = 4,18 J/(g·K), c(ijs) = 2,1 J/(g·K)
  - smeltwarmte L_s = 334 J/g, verdampingswarmte L_v = 2257 J/g
  - M(H₂O) = 18,0 g/mol, R = 8,314 J/(mol·K)
- **Alle getallen zijn nagerekend.** Bij XPRT-schattingen staat erbij welke vereenvoudiging is gebruikt.

Levels 1, 2 en 6 zijn nog leeg. Hier neem ik molecuul, H-brug en koken aan, zoals op de tegels.

---

## Level 1 · Het watermolecuul

### Onderbouw
1. MK · Uit welke atomen bestaat een watermolecuul? **1 O + 2 H** / 2 O + 1 H / 1 O + 1 H / 2 O + 2 H
2. MK · Welke kleur heeft zuurstof in het model? **rood** / wit / blauw / zwart
3. MK · Is een watermolecuul recht of gebogen? **gebogen** / recht / ringvormig / plat vierkant
4. MK · Wat is H₂O in gewone woorden? **water** / waterstof / zuurstof / zout
5. MK · Wat gebeurt er met de moleculen zelf als ijs smelt? **niets, ze blijven H₂O** / ze worden groter / ze splitsen in H en O / ze verdwijnen
6. GT · Uit hoeveel atomen bestaat één watermolecuul? **3**
7. MK · Wat zit er tussen de moleculen in? **niets (lege ruimte)** / lucht / water / stof
8. MK · Zijn moleculen met het blote oog te zien? **nee, veel te klein** / ja, als stipjes / alleen onder een vergrootglas / alleen in ijs
9. MK · Bewegen de moleculen in een glas water stil? **nee, ze bewegen altijd** / ja / alleen als je roert / alleen als het kookt
10. MK · Wat stelt het model niet goed op schaal voor? **de grootte en de snelheid van de moleculen** / het aantal atomen in een molecuul / de kleur van water / niets

### Bovenbouw
1. GT · Hoe groot is de hoek H–O–H (in °)? **104,5** (±1)
2. MK · Welk atoom trekt het hardst aan de bindingselektronen? **O** / H / allebei even hard / geen van beide
3. MK · Welke lading hebben de H-atomen? **δ+** / δ− / neutraal / 1+
4. MK · Waarom is water een dipool? **door de polaire O–H-bindingen en de gebogen vorm** / omdat het vloeibaar is / omdat het ionen bevat / omdat het zuurstof bevat
5. GT · Hoeveel vrije elektronenparen heeft het O-atoom? **2**
6. GT · Molecuulmassa van H₂O in u? **18,0** (±0,1)
7. MK · Stel dat water lineair was, zoals CO₂. Dan zou water… **geen dipool zijn** / zwaarder zijn / niet bestaan / sterker polair zijn
8. GT · Hoeveel moleculen zitten er in 1,0 g water? **3,3·10²²** (±0,1·10²²)
9. MK · Welke binding is binnen een watermolecuul aanwezig? **atoombinding (covalent)** / ionbinding / metaalbinding / waterstofbrug
10. GT · Lengte van de O–H-binding in Å? **0,96** (±0,02)

### XPRT League
1. MK · Waarom is de hoek 104,5° en niet 109,5° (tetraëder)? **vrije elektronenparen stoten sterker af dan bindende paren** / H-atomen trekken elkaar aan / door de waterstofbruggen / door warmtebeweging
2. GT · Hoeveel trillingswijzen (normaalmodi) heeft een H₂O-molecuul? (3N − 6) **3**
3. MK · Een H₂O-molecuul heeft 3 trillingswijzen (normaalmodi). Welke zijn dat? **symmetrische strek, asymmetrische strek, buiging** / drie rotaties / drie translaties / twee strek, één torsie
4. GT · Dipoolmoment van water in debye? **1,85** (±0,05)
5. MK · Zwaar water (D₂O) smelt bij… **3,8 °C** / 0 °C / −3,8 °C / 100 °C
6. MK · Waarom heeft D₂O een hogere dichtheid dan H₂O (1,11 vs 1,00 g/cm³)? **zwaardere kernen, vrijwel even groot molecuul** / grotere moleculen / sterkere covalente binding / D₂O is geen water
7. GT · Totaal aantal elektronen in een H₂O-molecuul? **10**
8. MK · Welke hybridisatie wordt meestal aan O in water toegekend (vereenvoudigd model)? **sp³** / sp² / sp / geen
9. GT · Bindingsenergie O–H in kJ/mol (Binas)? **463** (±10)
10. MK · Waarom is de elektrische geleiding van zuiver water laag maar niet nul? **autoprotolyse: een klein deel vormt H₃O⁺ en OH⁻** / er zit altijd zout in / elektronen bewegen vrij / door de dipool

---

## Level 2 · De waterstofbrug

### Onderbouw
1. MK · Wat houdt watermoleculen bij elkaar? **een aantrekkende kracht tussen moleculen** / lijm / ijzerdraadjes / niets
2. MK · Een waterstofbrug zit… **tussen twee moleculen** / binnen één molecuul / tussen twee H-atomen van hetzelfde molecuul / in de kern
3. MK · Wat is sterker? **de binding binnen het molecuul** / de waterstofbrug / even sterk / hangt van de kleur af
4. MK · Waarom kookt water pas bij 100 °C, terwijl het molecuul zo licht is? **de moleculen houden elkaar stevig vast** / water is zwaar / door de lucht / door het zout
5. MK · Waarom kan een schaatsenrijder (het waterinsect) op water lopen? **oppervlaktespanning door de aantrekking tussen moleculen** / water is hard / het insect is gewichtloos / de lucht duwt
6. MK · Waarom vormt water ronde druppels? **de moleculen trekken elkaar naar binnen** / door de zwaartekracht / door de wind / water is plakkerig
7. MK · Wat gebeurt er met de bruggen als water warmer wordt? **er breken er meer** / ze worden sterker / er komen er meer bij / niets
8. GT · Met hoeveel buren is een molecuul in ijs verbonden? **4**
9. MK · In het model zijn H-bruggen… **blauwe stippellijntjes** / rode bollen / witte staafjes / onzichtbaar
10. MK · Blijft een waterstofbrug altijd bestaan? **nee, ze breken en vormen steeds opnieuw** / ja / alleen in ijs / alleen bij koken

### Bovenbouw
1. MK · Een H-brug loopt van… **H (δ+) van het ene molecuul naar O (δ−) van een ander** / O naar O / H naar H / O naar H in hetzelfde molecuul
2. GT · Maximaal aantal H-bruggen per watermolecuul? **4** (2 als donor, 2 als acceptor)
3. MK · H₂S (zelfde vorm, zwaarder) kookt bij… **−60 °C** / 100 °C / 200 °C / 0 °C
4. MK · Verklaring voor 3? **H₂S vormt nauwelijks H-bruggen** / H₂S is lichter / H₂S is een ion / zwavel is kleiner
5. GT · Hoeveel keer zwakker is een H-brug (≈ 20 kJ/mol) dan een O–H-binding (463 kJ/mol)? **≈ 20–25×** (antwoord 23, ±3)
6. GT · Afstand H···O in een H-brug in Å, als O–O = 2,76 en O–H = 0,96? **1,80** (±0,02)
7. MK · Welke stof mengt goed met water door H-bruggen? **ethanol** / hexaan / olie / tetra
8. MK · Waarom heeft water een grote soortelijke warmte (4,18 J/(g·K))? **een deel van de warmte gaat naar het verbreken van H-bruggen** / water is zwaar / water geleidt goed / door opgeloste lucht
9. GT · Gemiddeld aantal H-bruggen per molecuul in water van 90 °C (model)? **2,9** (±0,2)
10. MK · Welke stoffen vormen óók H-bruggen? **NH₃ en HF** / CH₄ en CO₂ / NaCl / O₂ en N₂

### XPRT League
1. GT · Schat de energie van één H-brug uit de smeltwarmte. Bij smelten breekt ≈ 12,5% van de bruggen (van 2 naar 1,75 per molecuul), dus 0,25 mol bruggen per mol water. Antwoord in kJ/mol. **24** (±2)
2. GT · Doe hetzelfde met de verdampingswarmte: bij verdampen breken alle ≈ 1,75 bruggen per molecuul. In kJ/mol? **23** (±2). (Vereenvoudigd: zonder volumewerk en andere bijdragen.)
3. MK · Wat leer je uit 1 en 2? **beide schattingen geven ≈ 20–25 kJ/mol: het brugmodel is consistent** / smelten kost meer per brug / verdampen breekt geen bruggen / de schatting klopt niet
4. GT · RT bij 25 °C in kJ/mol? **2,48** (±0,02). Om 1 mol waterstofbruggen te verbreken is ongeveer 20 kJ nodig (de bindingsenergie). Hoeveel keer RT is dat? **≈ 8**
5. MK · Waarom is een H-brug ongeveer lineair (O–H···O ≈ 180°)? **de aantrekking tussen δ+ en het vrije elektronenpaar is het sterkst in één lijn** / door de zwaartekracht / toeval / door de rooster­constante
6. MK · Levensduur van een H-brug in vloeibaar water? **≈ picoseconden (10⁻¹² s)** / seconden / uren / milliseconden
7. MK · Gevolg van 6 voor "geheugen" van water? **de structuur past zich binnen ns aan de temperatuur aan; geen geheugen van eerder** / water onthoudt vormen / water onthoudt stoffen / bruggen zijn permanent
8. MK · Waarom heeft ijs een open structuur? **H-bruggen leggen 4 buren vast in een tetraëder** / er zit lucht in / de moleculen zijn groter / door druk
9. GT · Oppervlaktespanning van water bij 25 °C in mN/m? **72** (±2)
10. MK · Welk spectroscopisch effect wijst op H-bruggen? **de O–H-strekband schuift naar lagere golfgetallen en wordt breder** / de massa stijgt / de kleur wordt blauw / geen

---

## Level 3 · Het ijskristal

### Onderbouw
1. DOE · Vlieg door een leeg kanaal. *(camera in een kanaal, 2 s)*
2. GT · Hoeveel moleculen vormen een zeshoek? **6**
3. MK · Hoe heet de vorm van het ijsrooster? **honingraat (zeshoeken)** / kubus / bol / spiraal
4. MK · Waarom heeft een sneeuwvlok zes punten? **het ijsrooster is zeshoekig** / toeval / door de wind / door de wolken
5. MK · Bewegen moleculen in ijs? **ja, ze trillen op hun plek** / nee, helemaal niet / ja, ze vliegen rond / alleen bij smelten
6. MK · Wat zit er in de kanalen? **niets** / lucht / water / zout
7. MK · Wat heeft meer ruimte nodig: 1 kg ijs of 1 kg water? **ijs** / water / even veel / hangt af van de vorm
8. MK · Bij welke temperatuur smelt ijs (bij normale druk)? **0 °C** / 4 °C / −10 °C / 100 °C
9. MK · Waarom vriezen leidingen kapot? **water zet uit bij bevriezen** / ijs is zwaar / door de kou krimpt het metaal / door luchtbellen
10. DOE · Zet kalotten aan en vind de kanalen terug. *(kalotten aan + camera in kanaal)*

### Bovenbouw
1. GT · O–O-afstand bij een H-brug in ijs, in Å? **2,76** (±0,02)
2. MK · Hoek tussen twee H-bruggen rond één O in ijs? **≈ 109,5° (tetraëder)** / 90° / 120° / 180°
3. MK · Hoe heet gewoon ijs? **ijs Ih (hexagonaal)** / ijs Ic / ijs VII / glas
4. GT · Dichtheid van ijs in g/cm³ (afgelezen)? **0,917** (±0,005)
5. MK · Waar zitten de H-atomen in ijs? **op de lijn tussen twee O-atomen, dicht bij één ervan** / precies in het midden / in de kanalen / willekeurig
6. MK · Ijsregels: elk O-atoom heeft… **2 H dichtbij en 2 H veraf** / 4 H dichtbij / 1 H / geen H
7. GT · Hoeveel H-bruggen zijn er per molecuul, als je elke brug maar één keer telt? **2**
8. MK · Waarom is een ijsoppervlak glad? **er zit een dun vloeibaar laagje op (premelting en wrijvingswarmte)** / ijs is vettig / door de druk van de schaats alleen / ijs is altijd nat
9. MK · Wat is sublimatie? **ijs gaat direct over in damp** / ijs smelt / damp wordt water / water bevriest
10. MK · Waarom droogt wasgoed ook bij vorst? **het ijs sublimeert** / de wind blaast het weg / het smelt toch / het droogt niet

### XPRT League
1. GT · Aantal H-bruggen in een kristal van 1000 moleculen (randen negeren)? **2000**
2. GT · Dichtheid uit de cel 4,52 × 7,83 × 7,36 Å met 8 moleculen? **0,919 g/cm³** (±0,005)
3. GT · Restentropie volgens Pauling, S = R ln 1,5, in J/(mol·K)? **3,37** (±0,05)
4. MK · Waarom blijft er restentropie over bij 0 K? **de H-atomen kunnen op veel manieren liggen volgens de ijsregels** / de O-atomen trillen nog / er zit lucht in / meetfout
5. GT · Hoeveel moleculen zitten er in de hexagonale eenheidscel van ijs Ih? **4**
6. MK · Welk ijs is dichter dan water en zinkt? **ijs VII (hoge druk)** / ijs Ih / ijs Ic / geen enkel ijs
7. GT · Smeltentropie ΔS = ΔH/T in J/(mol·K) (ΔH = 6,01 kJ/mol)? **22,0** (±0,3)
8. GT · ΔG van smelten bij −10 °C in J/mol (ΔH en ΔS constant genomen)? **+220** (±10). Smelt ijs dan vanzelf? **nee**
9. MK · Hoe heet ijs met geordende H-atomen (onder ≈ 72 K, met KOH als katalysator)? **ijs XI** / ijs Ic / ijs II / amorf ijs
10. MK · Wat is amorf ijs? **ijs zonder kristalrooster (snel ingevroren)** / ijs met luchtbellen / ijs uit zeewater / hexagonaal ijs

---

## Level 4 · Smelten en bevriezen

### Onderbouw
1. DOE · Laat al het ijs smelten. *(100% gesmolten)*
2. MK · Wat doet de temperatuur tijdens het smelten? **blijft 0 °C** / stijgt / daalt / springt
3. DOE · Laat het water weer bevriezen. *(van 100% terug naar 0% gesmolten)*
4. MK · Waar gaat de warmte tijdens het smelten naartoe? **naar het losmaken van de moleculen** / naar de lucht / het ijs wordt warmer / nergens
5. MK · Welke overgang is bevriezen? **vloeibaar → vast** / vast → vloeibaar / vloeibaar → gas / gas → vast
6. MK · Wat bewegen sneller: moleculen in water van 20 °C of in ijs van −20 °C? **in water** / in ijs / even snel / ze bewegen niet
7. MK · Waarom koelt een ijsblokje je drinken goed? **smelten kost veel warmte** / ijs is kouder dan 0 °C / ijs drijft / ijs is zwaar
8. MK · Smeltpunt en stolpunt van water zijn… **gelijk (0 °C)** / verschillend / 4 °C en 0 °C / afhankelijk van de hoeveelheid
9. MK · Zout op de weg zorgt ervoor dat… **ijs bij lagere temperatuur smelt** / ijs harder wordt / water sneller bevriest / niets
10. DOE · Houd een mengsel van ijs en water in stand. *(gesmolten 30–70%, 5 s)*

### Bovenbouw
1. GT · Warmte om 100 g ijs van 0 °C te smelten, in kJ? **33,4** (±0,3)
2. GT · Warmte om 100 g ijs van −20 °C te laten smelten (eindigt als water van 0 °C), in kJ? **37,6** (±0,4)
3. GT · Hoeveel H-bruggen per molecuul bij ≈ 1 °C (model)? **3,5** (±0,2)
4. DOE · Houd het mengsel 40–60% gesmolten, 5 s. *(tip: 0 °C)*
5. MK · Waarom duurt het smelten lang terwijl de temperatuur gelijk blijft? **de smeltwarmte (334 J/g) is groot vergeleken met c = 2,1 J/(g·K)** / er komt geen warmte binnen / de thermometer is traag / door de dichtheid
6. GT · Hoeveel graden kun je 1 g water verwarmen met de smeltwarmte van 1 g ijs? **80** (334/4,18; ±1)
7. MK · Waarom besproeien fruittelers hun bomen bij nachtvorst? **bij bevriezen komt warmte vrij, de knoppen blijven 0 °C** / water isoleert / om ijs te maken als kunst / om insecten te doden
8. GT · Vriespuntsdaling van 1 kg water met 1 mol NaCl (K_f = 1,86 K·kg/mol, i = 2), in K? **3,7** (±0,1)
9. MK · Wat is onderkoeling? **water dat onder 0 °C nog vloeibaar is** / ijs onder 0 °C / koud water in de koelkast / smelten bij −5 °C
10. MK · Waarom is onderkoelen mogelijk? **ijs moet eerst een kiem vormen** / water kan niet bevriezen / door de druk / door het zout

### XPRT League
1. GT · 100 g water van 80 °C + 50 g ijs van 0 °C. Eindtemperatuur in °C? **26,7** (±0,3)
2. GT · 250 g cola (c als water) van 25 °C + 40 g ijs van 0 °C. Eindtemperatuur? **10,5** (±0,3)
3. GT · Smeltpuntsdaling per bar druk (Clausius-Clapeyron; L = 334 J/g, Δv = −0,0905 cm³/g). Hoeveel bar voor 1 K? **≈ 135** (±5)
4. MK · Een schaats geeft ≈ 23 bar. Smelt het ijs door de druk? **nee, dat is maar ≈ 0,2 K; wrijving en een vloeibaar oppervlaktelaagje verklaren het** / ja, altijd / alleen bij −20 °C / ja, 23 K
5. MK · Waarom heeft de smeltlijn in het fasediagram van water een negatieve helling? **ijs heeft een groter volume dan water** / ijs is zwaarder / smelten geeft warmte / door de H-brug­energie
6. GT · Kookplateau ÷ smeltplateau bij gelijke warmtetoevoer? **6,8** (±0,1)
7. GT · Tot welke temperatuur kan zuiver water ongeveer onderkoeld worden (homogene kiemvorming), in °C? **≈ −40** (±3)
8. MK · Wat bepaalt de kans op bevriezen van onderkoeld water? **de kans om een kritische ijskiem te vormen (kiemvorming is een kansproces)** / alleen de tijd / de kleur van de beker / de druk
9. GT · ΔS van de omgeving als 1 mol water bevriest bij 0 °C, in J/(mol·K)? **+22,0** (±0,3)
10. MK · Totale entropieverandering bij bevriezen precies bij 0 °C? **nul (evenwicht)** / positief / negatief / oneindig

---

## Level 5 · Waarom ijs drijft

### Onderbouw
1. MK · Wat heeft de grootste dichtheid? **water** / ijs / even groot / hangt af van de hoeveelheid
2. DOE + MK · Laat het blok smelten. Het volume wordt… **kleiner** / groter / gelijk
3. MK · Waarom kan een volle fles in de vriezer knappen? **water zet uit bij bevriezen** / krimpt / glas krimpt / er komt gas vrij
4. MK · Waarom vriest een sloot van boven dicht? **ijs drijft** / de bodem is warm / de wind / water zakt naar boven
5. MK · Wat gebeurt er met vissen in een bevroren meer? **die zwemmen in het water onder het ijs** / die bevriezen / die gaan slapen in het ijs / die verdwijnen
6. MK · Een ijsberg steekt… **maar een klein stukje boven water uit** / helemaal boven water uit / voor de helft uit / niet uit
7. MK · Dichtheid betekent… **massa per volume** / gewicht / grootte / temperatuur
8. MK · Wat doet een ijsblokje in alcohol (ρ ≈ 0,79)? **het zinkt** / het drijft / het blijft zweven / het lost direct op
9. MK · Waarom heeft ijs een kleinere dichtheid? **de moleculen zitten in een open honingraat** / er zit lucht in / het is koud / ijs is licht
10. MK · Zijn ijsmoleculen groter dan watermoleculen? **nee, ze zitten alleen verder uit elkaar** / ja / ja, twee keer zo groot / ze zijn kleiner

### Bovenbouw
1. GT · Hoeveel procent groter is het volume van ijs dan dat van water? **9** (±0,5)
2. GT · Welk deel van een ijsblok in zoet water zit onder water, in %? **92** (±1)
3. GT · Bij welke temperatuur heeft water de grootste dichtheid, in °C? **4** (±0,5)
4. MK · Waarom krimpt water tussen 0 en 4 °C bij verwarmen? **ijsachtige stukjes netwerk breken af en moleculen schuiven dichter op elkaar** / water verdampt / door luchtbellen / meetfout
5. GT · Massa van 1,0 L ijs, in g? **917** (±5)
6. GT · Volume van 1,0 kg ijs, in L? **1,09** (±0,01)
7. MK · Wat zit er bij 4 °C op de bodem van een diep meer in de winter? **water van ≈ 4 °C** / ijs / water van 0 °C / water van 10 °C
8. MK · Wat gebeurt er met de zeespiegel als drijvend ijs smelt (zoet water in zoet water)? **niets** / stijgt / daalt / hangt af van de vorm
9. MK · En als landijs smelt? **de zeespiegel stijgt** / daalt / blijft gelijk / hangt af van het weer
10. MK · Welke kracht laat ijs drijven? **opwaartse kracht (Archimedes)** / magnetisme / oppervlaktespanning / wind

### XPRT League
1. DOE + GT · Zoek met de schuif het dichtheidsmaximum. **3,98 °C** (±0,5)
2. GT · Deel van een ijsberg onder water in zeewater (ρ = 1,025), in %? **89** (±1)
3. MK · Drijvend zoet ijs smelt in zout zeewater. De zeespiegel… **stijgt een heel klein beetje** / daalt / blijft precies gelijk / stijgt sterk
4. MK · Waarom bevriest een meer van boven en blijft de bodem ≈ 4 °C? **water van 4 °C is het dichtst en zakt; kouder water blijft boven en bevriest; ijs isoleert** / warm water zakt / ijs zinkt / de bodem verwarmt
5. GT · Volumeverandering bij het smelten van 1 mol ijs, in cm³? **−1,6** (18,0/1,000 − 18,0/0,917; ±0,1)
6. MK · Waarom heeft zeewater geen dichtheidsmaximum boven het vriespunt? **opgelost zout verschuift het maximum onder het vriespunt** / zeewater bevriest niet / zout is lichter / door golven
7. GT · Uitzettingscoëfficiënt van water bij 4 °C? **0** (dichtheid is daar maximaal)
8. MK · Welke andere stof zet ook uit bij stollen? **silicium (en gallium, bismut)** / ijzer / kaarsvet / koolstofdioxide
9. GT · Relatieve dichtheidsverandering van water tussen 4 en 0 °C, in ppm? **≈ 130** (model: 1,00000 − 0,99987; Binas geeft ≈ 160; ±40)
10. MK · Waarom is het dichtheidsmaximum belangrijk voor het leven? **meren vriezen niet tot de bodem dicht; er blijft vloeibaar water onder het ijs** / vissen hebben warmte nodig / het ijs blijft dun / geen

---

## Level 6 · Koken en verdampen

### Onderbouw
1. MK · Welke overgang is koken? **vloeibaar → gas** / vast → vloeibaar / gas → vloeibaar / vast → gas
2. MK · Wat zit er in de belletjes van kokend water? **waterdamp** / lucht / zuurstof / waterstof
3. MK · Wat doet de temperatuur tijdens het koken? **blijft 100 °C** / stijgt / daalt / springt
4. MK · Verdampen gebeurt ook… **onder 100 °C, aan het oppervlak** / alleen bij 100 °C / alleen in de zon / nooit
5. MK · Waarom koelt zweten je af? **verdampen kost warmte** / zweet is koud / door de wind / zout koelt
6. MK · Is de witte "rook" boven een pan waterdamp? **nee, kleine druppeltjes; damp is onzichtbaar** / ja / het is rook / het is lucht
7. MK · Bewegen moleculen in damp dichter bij elkaar dan in water? **nee, veel verder uit elkaar** / ja / even ver / ze bewegen niet
8. MK · Op een hoge berg kookt water bij… **lagere temperatuur** / hogere temperatuur / 100 °C / het kookt niet
9. MK · Wat is condenseren? **gas → vloeibaar** / vloeibaar → gas / vast → gas / vloeibaar → vast
10. MK · Waarom beslaat een koude fles? **waterdamp uit de lucht condenseert erop** / de fles lekt / het glas zweet / door de kou ontstaat water

### Bovenbouw
1. GT · Warmte om 100 g water van 20 °C aan de kook te brengen, in kJ? **33,4** (±0,3)
2. GT · Warmte om daarna die 100 g te laten verdampen, in kJ? **226** (±2)
3. GT · Waterkoker 2000 W, 1,0 L van 20 °C → 100 °C. Tijd in s (zonder verlies)? **167** (±3)
4. GT · Hoe lang daarna tot hij droogkookt, in s? **1128** (±15)
5. MK · Wanneer kookt een vloeistof? **als de dampdruk gelijk is aan de buitendruk** / bij 100 °C / als er bellen zijn / als alle bruggen breken
6. MK · Waarom gaat koken in een snelkookpan sneller? **hogere druk → hoger kookpunt (≈ 120 °C)** / minder water / door de stoom / door het deksel
7. GT · Kookpunt op de Mount Everest (≈ 0,34 bar), in °C? **≈ 71** (±3)
8. GT · Kookpuntsverhoging van 1 kg water met 1 mol NaCl (K_b = 0,51, i = 2), in K? **1,0** (±0,05)
9. MK · Wat gebeurt er met de H-bruggen bij verdampen? **ze breken allemaal** / de helft breekt / geen enkele / ze worden sterker
10. GT · Hoeveel energie (kJ) voert 0,5 L zweet af (≈ 2,4 kJ/g bij 37 °C)? **1200** (±50)

### XPRT League
1. GT · Molaire verdampingsenthalpie in kJ/mol? **40,7** (±0,3)
2. GT · Verdampingsentropie bij 100 °C, in J/(mol·K)? **109** (±1)
3. MK · Regel van Trouton zegt ≈ 85–88 J/(mol·K). Waarom ligt water hoger? **door H-bruggen is vloeibaar water extra geordend** / water is licht / door de druk / meetfout
4. GT · v_rms van watermoleculen in damp van 100 °C, in m/s? **≈ 720** (±10)
5. GT · Volume van 1 mol waterdamp bij 100 °C en 1,013 bar (ideaal gas), in L? **30,6** (±0,3)
6. GT · Hoeveel keer groter is dat dan het volume van 1 mol vloeibaar water (18 mL)? **≈ 1700** (±50)
7. GT · Arbeid tegen de buitendruk bij verdampen van 1 mol (pΔV ≈ RT bij 100 °C), in kJ? **3,1** (±0,1)
8. GT · Tripelpunt van water: temperatuur in °C en druk in Pa? **0,01 °C en 612 Pa** (druk ±5)
9. GT · Kritische temperatuur van water in °C? **374** (±2)
10. MK · Wat gebeurt er boven het kritische punt? **geen onderscheid meer tussen vloeistof en gas (superkritisch)** / water bevriest / water ontleedt / het kookt heftiger
