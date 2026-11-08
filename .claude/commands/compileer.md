Compileer de simulatiecode en de tests van Water.sim buiten Unity om, met Unity's eigen compiler, en draai de tests. Wijzigt niets in het project.

```bash
S="/Applications/Unity/Hub/Editor/2023.1.2f1/PlaybackEngines/AndroidPlayer/SDK/6000.3.11f1/Unity.app/Contents/Resources/Scripting"
NS="$S/NetStandard/ref/2.1.0/netstandard.dll"
MS="$S/NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll"
NU=$(find Library/PackageCache -name nunit.framework.dll | head -1)
T=<tijdelijke map buiten het project>
CSC(){ "$S/NetCoreRuntime/dotnet" exec "$S/DotNetSdkRoslyn/csc.dll" -nologo -nostdlib -noconfig "$@"; }
CSC -target:library -r:"$NS" -out:$T/sim.dll $(find Assets/Scripts/Sim -name '*.cs')
CSC -target:library -r:"$NS" -r:"$MS" -r:"$NU" -r:$T/sim.dll -out:$T/tests.dll $(find Assets/Tests/EditMode -name '*.cs')
# tests draaien (Tools/Testdraaier: kleine runner die [Test] en [TestCaseSource] uitvoert)
CSC -target:exe -r:"$NS" -out:$T/runner.dll Tools/Testdraaier/Runner.cs
cp "$NU" Tools/Testdraaier/runner.runtimeconfig.json $T/
(cd $T && "$S/NetCoreRuntime/dotnet" runner.dll tests.dll)
```

- Bestaat het Unity-pad niet meer, zoek het dan met `ps -o command= -p $(pgrep -x Unity)`.
- Unity-code (Weergave, Besturing, UI, Editor) compileer je met de verwijzingen uit Unity's eigen
  responsbestand: `find Library/Bee -name WaterSim.rsp` (neem de nieuwste). Haal daar de regels
  `-r:`/`-define:`/`-langversion`/`-nowarn` uit (zonder WaterSim-verwijzingen), voeg `-r:sim.dll` toe, en
  voor de Editor-map ook `$S/Managed/UnityEngine/UnityEditor*.dll`.
- Meld errors met bestand:regel, en per test OK of FOUT.
- De runner is een snelle controle. De echte uitslag blijft de Test Runner in Unity:
  Window › General › Test Runner › EditMode › Run All.
