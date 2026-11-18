// Run with Node after replacing Tools/glove-{left,right}.glb.
// Retains authored finger membership for the existing prepared glove vertices.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
for(const side of ['Left','Right']){
 const data=fs.readFileSync(path.join(root,'Tools',`glove-${side.toLowerCase()}.glb`));
 const length=data.readUInt32LE(12),json=JSON.parse(data.subarray(20,20+length).toString()),blob=data.subarray(28+length);
 function values(id){
  const a=json.accessors[id],v=json.bufferViews[a.bufferView],bytes={5121:1,5123:2,5126:4}[a.componentType];
  if(a.type!=='VEC4'||!bytes)throw new Error('Expected VEC4 joint/weight accessor');
  const stride=v.byteStride||bytes*4,offset=(v.byteOffset||0)+(a.byteOffset||0);
  return Array.from({length:a.count},(_,i)=>Array.from({length:4},(_,k)=>{
   const p=offset+i*stride+k*bytes;
   return a.componentType===5126?blob.readFloatLE(p):a.componentType===5121?blob.readUInt8(p):blob.readUInt16LE(p);
  }));
 }
 const attributes=json.meshes[0].primitives[0].attributes,joints=values(attributes.JOINTS_0),weights=values(attributes.WEIGHTS_0);
 const names=json.skins[0].joints.map(id=>json.nodes[id].name),digits=['index-finger','middle-finger','ring-finger','pinky-finger','thumb'];
 const output=Buffer.alloc(4+joints.length);output.writeInt32LE(joints.length);
 for(let i=0;i<joints.length;i++){
  const scores=digits.map(name=>joints[i].reduce((sum,j,k)=>sum+(names[j].startsWith(name)?weights[i][k]:0),0)),maximum=Math.max(...scores);
  output[4+i]=maximum>.25?scores.indexOf(maximum)+1:0;
 }
 fs.writeFileSync(path.join(root,'Assets','Ocean','Resources',`Glove${side}Contact.bytes`),output);
}
