$ErrorActionPreference = 'Stop'
$root = 'C:/XR Horizon/Projects/Under Water'
foreach ($asset in @('coast_sand_02','coral_ground_02')) {
    $meta = Get-Content -Raw -LiteralPath "$root/Tools/$asset.json" | ConvertFrom-Json
    foreach ($map in @('Diffuse','nor_gl','Rough','AO')) {
        $entry = $meta.$map.'2k'.jpg
        if ($entry) {
            $out = "$root/Assets/UnderwaterArt/Textures/" + [IO.Path]::GetFileName($entry.url)
            if (!(Test-Path -LiteralPath $out)) { Invoke-WebRequest -Uri $entry.url -OutFile $out }
            Write-Output ([IO.Path]::GetFileName($out))
        }
    }
}
Invoke-WebRequest -Uri 'https://huggingface.co/api/datasets/suvadityamuk/smithsonian-open-access-3d/tree/main' -OutFile "$root/Tools/smithsonian-files.json"
Invoke-WebRequest -Uri 'https://huggingface.co/datasets/suvadityamuk/smithsonian-open-access-3d/raw/main/README.md' -OutFile "$root/Tools/smithsonian-readme.md"
