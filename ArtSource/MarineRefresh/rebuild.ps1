param([string]$BlenderPath='C:/Program Files/Blender Foundation/Blender 5.3/blender.exe')
$ErrorActionPreference='Stop'
$repoRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $repoRoot
try {
 function Invoke-MarineStep([string]$Script,[string]$Kind='') {
  $arguments=@('-b','--python-exit-code','1','--python',(Join-Path $PSScriptRoot $Script))
  if($Kind){$arguments+=@('--',$Kind)}
  & $BlenderPath @arguments
  if($LASTEXITCODE -ne 0){throw "Blender asset step failed: $Script $Kind"}
 }
 # Original supplied files are retained in Source; no network or Unity build.
 Invoke-MarineStep 'inspect.py'
 foreach($kind in @('Dolphin','CombJelly','Lionfish')){Invoke-MarineStep 'prepare.py' $kind}
 Invoke-MarineStep 'polish_lion_only.py'
 Invoke-MarineStep 'polish_comb_only.py'
 Invoke-MarineStep 'shark_clean_swim.py'
 Invoke-MarineStep 'review.py'
} finally {Pop-Location}
