Compileer de simulatiecode en de tests van Water.sim buiten Unity om, met Unity's eigen compiler. Wijzigt niets in het project.

```bash
S="/Applications/Unity/Hub/Editor/2023.1.2f1/PlaybackEngines/AndroidPlayer/SDK/6000.3.11f1/Unity.app/Contents/Resources/Scripting"
NS="$S/NetStandard/ref/2.1.0/netstandard.dll"
MS="$S/NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll"
NU=$(find Library/PackageCache -name nunit.framework.dll | head -1)
T=<tijdelijke map buiten het project>
"$S/NetCoreRuntime/dotnet" exec "$S/DotNetSdkRoslyn/csc.dll" -nologo -nostdlib -noconfig -target:library \
  -r:"$NS" -out:$T/sim.dll $(find Assets/Scripts/Sim -name '*.cs')
"$S/NetCoreRuntime/dotnet" exec "$S/DotNetSdkRoslyn/csc.dll" -nologo -nostdlib -noconfig -target:library \
  -r:"$NS" -r:"$MS" -r:"$NU" -r:$T/sim.dll -out:$T/tests.dll $(find Assets/Tests/EditMode -name '*.cs')
```

- Bestaat het Unity-pad niet meer, zoek het dan met `ps -o command= -p $(pgrep -x Unity)`.
- Meld errors met bestand:regel.
- De tests zelf draaien in Unity: Window › General › Test Runner › EditMode › Run All.
