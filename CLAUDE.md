# Water.sim

Unity-app (C#) voor iOS en Android die water op molecuulniveau laat zien: ijs (honingraat),
smelten, vloeibaar water, koken, en waarom ijs drijft. Doelgroep: scheikunde en natuurkunde op de
middelbare school. Broertje van C.chem (zelfde stijl, zelfde Unity-versie 6000.3.11f1, URP).

- Analyse, keuzes en stappenplan: `Docs/Analyse_en_stappenplan.md` — volg de fasen daaruit.
- Werkend browserprototype als referentie: `Docs/prototype/ijskristal-3d.html`.
- Stijlvoorbeeld: C.chem (`../C.chem`, alleen lezen; daar niets wijzigen).

## Huisregels

- Communicatie, commentaar in de code en teksten: **Nederlands**. Klassen en bestanden ook in het
  Nederlands (zoals in C.chem), namespace `WaterSim`.
- **Simulatie en weergave strikt gescheiden.**
  - `Assets/Scripts/Sim` (assembly `WaterSim.Sim`) heeft geen Unity-afhankelijkheid. Alle
    natuurkunde staat daar en wordt getest in `Assets/Tests/EditMode`.
  - Weergave, besturing en UI lezen alleen uit de simulatie.
- Eenheden in de code:
  - afstand in Å, temperatuur in °C, warmte in J/g, dichtheid in g/cm³;
  - vaste getallen in `Constanten.cs`, niet los in de code.
- Natuurkunde moet kloppen met het prototype en Binas. Deze controlegetallen houden we vast in tests:
  - ijs Ih: elk O-atoom heeft 2 H-atomen en 4 H-bruggen; dichtheid ≈ 0,92;
  - vloeibaar water: ≈ 3,5 H-bruggen per molecuul bij 1 °C, ≈ 2,9 bij 90 °C;
  - het kookplateau duurt ≈ 6,8× zo lang als het smeltplateau.
- Vereenvoudigingen in het model altijd benoemen in de app ("model, niet op schaal").
- Teksten nooit hard in de code: altijd via het vertaalsysteem (eerst NL + EN).
- Platforms: iOS en Android. Burst, Jobs en compute shaders mogen gebruikt worden.
  - Houd de camera en de invoer los van elkaar (VR komt later).
  - Test de snelheid vroeg op een oudere tablet.
- Werkwijze zoals bij C.chem:
  - `/analyse` = alleen analyseren, niets wijzigen;
  - vóór grotere wijzigingen eerst `/backup` (of een git-commit);
  - een afgeronde opdracht sluiten we af met **klaar :)**.

## Controle buiten Unity

De simulatiecode compileert los, met Unity's eigen compiler. Zie de commando's in
`.claude/commands/compileer.md`.
