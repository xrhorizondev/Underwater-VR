$bytes=[IO.File]::ReadAllBytes((Join-Path $PSScriptRoot '../Assets/UnderwaterArt/LicensedGrouper/Grouper.glb.bytes'))
$length=[BitConverter]::ToInt32($bytes,12)
$g=[Text.Encoding]::UTF8.GetString($bytes,20,$length)|ConvertFrom-Json
$start=28+$length
function Offset($index){$a=$g.accessors[$index];return $start+$g.bufferViews[$a.bufferView].byteOffset+$a.byteOffset}
$parts=@()
foreach($primitive in $g.meshes[0].primitives){
 $vertices=@();$normals=@();$indices=@();$a=$g.accessors[$primitive.attributes.POSITION];$offset=Offset $primitive.attributes.POSITION;$normalOffset=Offset $primitive.attributes.NORMAL
 for($i=0;$i -lt $a.count;$i++){$o=$offset+$i*12;$vertices+=@{x=-25*[BitConverter]::ToSingle($bytes,$o);y=25*[BitConverter]::ToSingle($bytes,$o+8);z=-25*[BitConverter]::ToSingle($bytes,$o+4)};$o=$normalOffset+$i*12;$normals+=@{x=-[BitConverter]::ToSingle($bytes,$o);y=[BitConverter]::ToSingle($bytes,$o+8);z=-[BitConverter]::ToSingle($bytes,$o+4)}}
 $a=$g.accessors[$primitive.indices];$offset=Offset $primitive.indices
 for($i=0;$i -lt $a.count;$i+=3){$indices+=[int][BitConverter]::ToUInt16($bytes,$offset+$i*2);$indices+=[int][BitConverter]::ToUInt16($bytes,$offset+($i+2)*2);$indices+=[int][BitConverter]::ToUInt16($bytes,$offset+($i+1)*2)}
 $c=$g.materials[$primitive.material].pbrMetallicRoughness.baseColorFactor
 $parts+=@{vertices=$vertices;normals=$normals;indices=$indices;color=@{r=$c[0];g=$c[1];b=$c[2];a=1}}
}
@{parts=$parts}|ConvertTo-Json -Depth 8 -Compress|Set-Content (Join-Path $PSScriptRoot '../Assets/UnderwaterArt/LicensedGrouper/GrouperGeometry.json')
