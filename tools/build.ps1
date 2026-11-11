param([string]$GameRoot = 'E:\steam\steamapps\common\Probably Stolen', [string]$OutputRoot, [switch]$CodeOnly)
# Compile-only batches preserve the bytes of already approved portrait textures.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (!$OutputRoot) { $OutputRoot = Join-Path $projectRoot 'dist\jinsong_poc' }
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$managed = Join-Path $GameRoot 'Probably Stolen_Data\Managed'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'Existing .NET Framework compiler was not found.' }
$references = @('mscorlib','System','System.Core','netstandard','Assembly-CSharp','Assembly-CSharp-firstpass','0Harmony','UnityEngine','UnityEngine.CoreModule','UnityEngine.AudioModule','UnityEngine.Physics2DModule','UnityEngine.InputLegacyModule','UnityEngine.SceneManagementModule','UnityEngine.UI','UnityEngine.UIModule','UnityEngine.TextRenderingModule','Unity.TextMeshPro','Unity.Localization','Unity.ResourceManager')
$arguments = @('/nologo','/noconfig','/nostdlib+','/target:library','/langversion:5','/optimize+','/utf8output',('/out:' + (Join-Path $OutputRoot 'JinSongPoc.dll')))
foreach ($reference in $references) {
    $path = Join-Path $managed ($reference + '.dll')
    if (Test-Path -LiteralPath $path) { $arguments += '/reference:' + $path }
}
$arguments += Join-Path $projectRoot 'src\JinSongPoc.cs'
$arguments += Join-Path $projectRoot 'src\AssassinStory.cs'
$arguments += Join-Path $projectRoot 'src\CityStory.cs'
$arguments += Join-Path $projectRoot 'src\CityStoryCampaign.cs'
$arguments += Join-Path $projectRoot 'src\CityEndingPresentation.cs'
$arguments += Join-Path $projectRoot 'src\CityRules.cs'
$arguments += Join-Path $projectRoot 'src\WindowTrade.cs'
$arguments += Join-Path $projectRoot 'src\MenuBackdrop.cs'
$arguments += Join-Path $projectRoot 'src\MenuBrand.cs'
$arguments += Join-Path $projectRoot 'src\OrdinaryPortraits.cs'
$arguments += Join-Path $projectRoot 'src\RosterPortraits.cs'
$arguments += Join-Path $projectRoot 'src\NpcNames.cs'
$arguments += Join-Path $projectRoot 'src\OrganizationText.cs'
$arguments += Join-Path $projectRoot 'src\DailyServicesText.cs'
$arguments += Join-Path $projectRoot 'src\InteriorArt.cs'
$arguments += Join-Path $projectRoot 'src\UnifiedInterior.cs'
$arguments += Join-Path $projectRoot 'src\ConceptFixtures.cs'
$arguments += Join-Path $projectRoot 'src\ShopHotspots.cs'
$arguments += Join-Path $projectRoot 'src\ShopMenuSkin.cs'
$arguments += Join-Path $projectRoot 'src\CommonUiArt.cs'
$arguments += Join-Path $projectRoot 'src\ReportedUiSkin.cs'
$arguments += Join-Path $projectRoot 'src\SceneActionArt.cs'
$arguments += Join-Path $projectRoot 'src\ShopGoods.cs'
$arguments += Join-Path $projectRoot 'src\RemainderItemText.cs'
$arguments += Join-Path $projectRoot 'src\WaterPresentation.cs'
$arguments += Join-Path $projectRoot 'src\CraftingText.cs'
$arguments += Join-Path $projectRoot 'src\ChemicalText.cs'
$arguments += Join-Path $projectRoot 'src\AccessCraftingText.cs'
$arguments += Join-Path $projectRoot 'src\FixtureText.cs'
$arguments += Join-Path $projectRoot 'src\SerializedLabels.cs'
$arguments += Join-Path $projectRoot 'src\Correspondence.cs'
$arguments += Join-Path $projectRoot 'src\ShopAudio.cs'
$arguments += Join-Path $projectRoot 'src\DirectShopAudio.cs'
$arguments += Join-Path $projectRoot 'src\MapCards.cs'
$arguments += Join-Path $projectRoot 'src\LocationArt.cs'
$arguments += Join-Path $projectRoot 'src\ProgressText.cs'
$arguments += Join-Path $projectRoot 'src\PaperText.cs'
$arguments += Join-Path $projectRoot 'src\NewsText.cs'
$arguments += Join-Path $projectRoot 'src\NewsPresentation.cs'
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $LASTEXITCODE" }

if ($CodeOnly) {
    @{outcome='passed'; output=$OutputRoot; code_only=$true} | ConvertTo-Json -Compress
    return
}

$textureSource = Join-Path $projectRoot 'assets\v01\source'
$textureStage = Join-Path $OutputRoot 'Textures'
New-Item -ItemType Directory -Path $textureStage -Force | Out-Null
Add-Type -AssemblyName System.Drawing
foreach ($image in Get-ChildItem -LiteralPath $textureSource -Filter '*.png' -File) {
    $target = Join-Path $textureStage $image.Name
    if ($image.BaseName -notlike 'jin_npc_*') {
        Copy-Item -LiteralPath $image.FullName -Destination $target -Force
        continue
    }

    # Use the refined originals and retain enough detail for the wanted poster crop.
    $original = [System.Drawing.Bitmap]::new($image.FullName)
    try {
        if ($original.Width -eq 720 -and $original.Height -eq 528) {
            Copy-Item -LiteralPath $image.FullName -Destination $target -Force
            continue
        }
        $packed = [System.Drawing.Bitmap]::new(720, 528, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($packed)
            try {
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.DrawImage($original, 0, 0, $packed.Width, $packed.Height)
            } finally { $graphics.Dispose() }
            $packed.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $packed.Dispose() }
    } finally { $original.Dispose() }
}
@{outcome='passed'; output=$OutputRoot; textures=@(Get-ChildItem -LiteralPath $textureStage -Filter '*.png' -File).Count} | ConvertTo-Json -Compress
