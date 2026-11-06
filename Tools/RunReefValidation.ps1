param([ValidateSet('CreateAndRender','Render','BuildWindows')][string]$Mode='CreateAndRender')
$ErrorActionPreference='Stop'
$root='C:/XR Horizon/Projects/Under Water'
$validation="$root/Tools/ReefValidation"
New-Item -ItemType Directory -Force $validation | Out-Null
foreach($folder in @('Assets','Packages','ProjectSettings')) {
    & robocopy "$root/$folder" "$validation/$folder" /E /NFL /NDL /NJH /NJS /NP | Out-Null
    if($LASTEXITCODE -gt 7){throw "Could not copy $folder"}
}
$log="$root/Logs/reef-$Mode.log"
$method='Underwater.Editor.RealisticReefBuilder.'+$Mode
$args=@('-batchmode','-force-d3d11','-projectPath',('"'+$validation+'"'),'-executeMethod',$method,'-quit','-logFile',('"'+$log+'"'))
$process=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/6000.0.52f1/Editor/Unity.exe' -ArgumentList $args -PassThru -Wait -WindowStyle Hidden
if($process.ExitCode -ne 0){Get-Content -Tail 50 $log;throw "Unity failed: $($process.ExitCode)"}
foreach($folder in @('Assets/UnderwaterArt','Assets/Ocean','Assets/Scenes','Assets/XR','Renders','Builds/RealisticWindows')){
    if(Test-Path "$validation/$folder"){
        & robocopy "$validation/$folder" "$root/$folder" /E /NFL /NDL /NJH /NJS /NP | Out-Null
        if($LASTEXITCODE -gt 7){throw "Could not return $folder"}
    }
}
Copy-Item "$validation/ProjectSettings/EditorBuildSettings.asset" "$root/ProjectSettings/EditorBuildSettings.asset" -Force
Write-Output "REEF_VALIDATION_SUCCESS $Mode"
