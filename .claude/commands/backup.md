Maak een backup van alle scripts van dit project.

Scripts-map: /Users/hanskovi/Creative Cloud/ConceptCraft/3D_interactive_projects/apps/Water.sim/Assets/Scripts
Backup-map:  /Users/hanskovi/Creative Cloud/ConceptCraft/3D_interactive_projects/apps/Water.sim/Assets/Scripts/Backups

Te backuppen: alle `*.cs`-bestanden in de Scripts-map **en de submappen** (Sim, Weergave, Besturing, UI), behalve de Backups-map zelf en bestanden waarvan de naam `_backup_` bevat.

Naamschema van de backup:
`<submap>__<origineelbestandsnaam>.cs.bak_JJJJMMDD_NN` (bij een bestand direct in Scripts zonder `<submap>__`).
- JJJJMMDD = datum van vandaag.
- NN = tweecijferig, nul-gevuld backupnummer van vandaag (01, 02, 03, …).
- De laatste extensie is dus NIET `.cs`, waardoor Unity het bestand negeert (geen compilatie, geen dubbele types). Herstellen = de `.bak_...`-suffix en het `<submap>__`-voorvoegsel weghalen en terugzetten in de submap.

Werkwijze:
1. Bepaal de datum van vandaag in het formaat JJJJMMDD.
2. Maak de Backup-map aan als die nog niet bestaat (`mkdir -p`).
3. Bepaal het volgende backupnummer NN: het laagste tweecijferige nummer dat vandaag nog niet voorkomt in de Backup-map (zoek naar `*.cs.bak_JJJJMMDD_*`). Begin bij 01.
4. Kopieer elk te backuppen `.cs`-bestand naar de Backup-map volgens het naamschema. De inhoud wordt NIET gewijzigd.
5. Rapporteer welke bestanden zijn aangemaakt en bevestig met `ls` welke bestanden in de Backup-map staan.

Verwijder nooit bestaande backups tenzij de gebruiker daar expliciet om vraagt.

Gebruik Bash-tools om de map te maken en de bestanden te kopiëren.
