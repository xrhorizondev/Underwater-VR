$ErrorActionPreference='Stop'
$root='C:/XR Horizon/Projects/Under Water'
$meta=Get-Content -Raw "$root/Tools/coast_rocks_01.json"|ConvertFrom-Json
$asset=$meta.gltf.'2k'.gltf
$target="$root/Tools/CoastRock"
New-Item -ItemType Directory -Force "$target/textures"|Out-Null
Invoke-WebRequest $asset.url -OutFile "$target/CoastRock.gltf"
foreach($file in $asset.include.PSObject.Properties){
 Invoke-WebRequest $file.Value.url -OutFile "$target/$($file.Name)"
 if($file.Name.StartsWith('textures/')){Copy-Item "$target/$($file.Name)" "$root/Assets/UnderwaterArt/Textures/" -Force}
}
