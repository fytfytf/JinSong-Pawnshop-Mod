$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$output = Join-Path $project 'test-only/alpha240-ui-survey'
$managed = 'E:/steam/steamapps/common/Probably Stolen/Probably Stolen_Data/Managed'
New-Item -ItemType Directory -Path $output -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $output 'manifest.xml'), '<Manifest><ID>jinsong_poc_probe</ID><Name>JinSong TEST ONLY - remove after validation</Name><Author>POC validation</Author><ModVersion>0.1-test</ModVersion><Description>Temporary memory-only UI survey. Never open existing saves.</Description><GameVersions><Version>0.46D</Version></GameVersions><Prerequisites/><Assemblies><Assembly>JinSongProbe.dll</Assembly></Assemblies><Capabilities/></Manifest>', (New-Object Text.UTF8Encoding($false)))
$compile = @('/nologo','/noconfig','/nostdlib+','/target:library','/langversion:5','/optimize+','/utf8output',('/out:' + (Join-Path $output 'JinSongProbe.dll')))
foreach ($name in @('mscorlib','System','System.Core','netstandard','Assembly-CSharp','0Harmony','UnityEngine','UnityEngine.CoreModule','UnityEngine.AudioModule','UnityEngine.UI','UnityEngine.UIModule','Unity.TextMeshPro','UnityEngine.TextRenderingModule','Unity.Localization','UnityEngine.ImageConversionModule')) {
    $file = Join-Path $managed ($name + '.dll')
    if (Test-Path -LiteralPath $file) { $compile += '/reference:' + $file }
}
$compile += Join-Path $PSScriptRoot 'ProbeUiSurvey.cs'
& 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' @compile
if ($LASTEXITCODE -ne 0) { throw 'UI survey helper compile failed' }
