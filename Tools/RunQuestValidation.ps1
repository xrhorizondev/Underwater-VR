param([ValidateSet('Validate','Build','Immersion','Manta','Performance','GrabCheck','GloveTravelCheck')][string]$Mode='Validate')
$ErrorActionPreference='Stop'
$root='C:/XR Horizon/Projects/Under Water'
$validation="$root/Tools/ReefValidation"
foreach($folder in @('Assets','Packages','ProjectSettings')){
 & robocopy "$root/$folder" "$validation/$folder" /E /NFL /NDL /NJH /NJS /NP | Out-Null
 if($LASTEXITCODE -gt 7){throw "Copy failed: $folder"}
}
$log="$root/Logs/quest-$Mode.log"
$arguments=@('-batchmode','-force-d3d11','-buildTarget','Android','-projectPath',('"'+$validation+'"'),'-executeMethod',('Underwater.Editor.QuestReefBuild.'+$Mode),'-quit','-logFile',('"'+$log+'"'))
$process=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.0.52f1/Editor/Unity.exe' -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
if($process.ExitCode -ne 0){Get-Content $log -Tail 45;throw "Quest $Mode failed"}
foreach($folder in @('Assets/UnderwaterArt','Assets/Ocean','Assets/XR','Assets/Scenes','Builds/Quest3','Renders')){
 if(Test-Path "$validation/$folder"){ & robocopy "$validation/$folder" "$root/$folder" /E /NFL /NDL /NJH /NJS /NP | Out-Null; if($LASTEXITCODE -gt 7){throw "Return copy failed"}}
}
Copy-Item "$validation/ProjectSettings/ProjectSettings.asset" "$root/ProjectSettings/ProjectSettings.asset" -Force
Copy-Item "$validation/ProjectSettings/GraphicsSettings.asset" "$root/ProjectSettings/GraphicsSettings.asset" -Force
Write-Output "QUEST_SUCCESS $Mode"






