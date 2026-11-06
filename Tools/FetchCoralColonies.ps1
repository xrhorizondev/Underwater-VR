$ErrorActionPreference='Stop'
$root='C:/XR Horizon/Projects/Under Water'
$colonies=@{Secale='cb2e1f4d-8eaa-454b-b61d-cc78b6b64b7b';Hyacinthus='1a0a3b2d-5ef6-4bc0-af39-0a35d95f1f01'}
foreach($name in $colonies.Keys){
 $base='https://3d-api.si.edu/content/document/3d_package:'+$colonies[$name]+'/'
 Invoke-WebRequest ($base+'document.json') -OutFile "$root/Tools/$name-scene.json"
 $doc=Get-Content -Raw "$root/Tools/$name-scene.json"|ConvertFrom-Json
 $asset=$doc.models[0].derivatives|Where-Object {$_.quality -eq 'Medium' -and $_.usage -eq 'Web3D'}|Select-Object -First 1
 if(!$asset){throw "No medium resolution for $name"}
 Invoke-WebRequest ($base+$asset.assets[0].uri) -OutFile "$root/Assets/UnderwaterArt/Scans/$name.glb"
 Write-Output "$name downloaded"
}
