# Fruit Tree developer utilities

On Windows, after installing Unity 6 with Android Build Support:

```powershell
pwsh -File tools/verify-unity.ps1 -UnityEditor "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe" -BuildApk
```

`-BuildApk` is optional. EditMode and PlayMode test logs/results are written under `Builds/`.

No Unity license, Unity Editor, or Android SDK is present in the coding environment used to author this repository, so this command has **not** been executed here. Unity tests and an actual install remain mandatory before release.
