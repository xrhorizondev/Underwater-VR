from pathlib import Path
p=Path('Assets/UnderwaterArt/Shaders/FishSkin.shader');s=p.read_text().replace('_Pattern("Reef species markings"','_Species("Reef color species",Float)=0\n  _Pattern("Reef species markings"').replace('float _Pattern;','float _Species;float _Pattern;')
marker='  o.Normal=UnpackScaleNormal'
pos=s.index(marker)
s=s[:pos]+'''  if(_Species>.5){
   float z=IN.bodyPosition.z,y=IN.bodyPosition.y;float3 pigment;
   if(_Species<1.5){pigment=lerp(float3(.78,.46,.006),float3(1,.74,.035),saturate(-y*2+.45));}
   else if(_Species<2.5){pigment=float3(.008,.11,.7);float band=smoothstep(-.03,.02,y)* (1-smoothstep(.14,.21,y+.04*sin(z*12)))*smoothstep(-.38,-.25,z)*(1-smoothstep(.26,.36,z));pigment=lerp(pigment,float3(.004,.009,.025),band);pigment=lerp(pigment,float3(.92,.62,.008),1-smoothstep(-.34,-.29,z));}
   else if(_Species<3.5){pigment=float3(.78,.8,.63);float stripes=min(abs(z-.25),min(abs(z),abs(z+.25)));pigment=lerp(pigment,float3(.65,.21,.015),1-smoothstep(.035,.07,stripes));float eyeSpot=1-smoothstep(.035,.05,length(float2(z+.17,y-.22)));pigment=lerp(pigment,float3(.008,.009,.012),eyeSpot);}
   else if(_Species<4.5){pigment=lerp(float3(.9,.59,.01),float3(.31,.012,.57),smoothstep(-.05,.035,z));}
   else if(_Species<5.5){pigment=lerp(float3(.94,.24,.045),float3(.66,.025,.19),smoothstep(.02,.24,y));pigment=lerp(pigment,float3(.94,.53,.19),smoothstep(.0,-.16,y));}
   else {pigment=lerp(float3(.62,.72,.65),float3(.8,.58,.04),smoothstep(.03,.2,y));float stripes=1-smoothstep(.14,.25,abs(frac((z+.5)*5)-.5));pigment=lerp(pigment,float3(.018,.025,.035),stripes*smoothstep(-.36,-.29,z));}
   o.Albedo=pigment*(.38+lifted*.9);
   if(z>.2&&scaleDetail<.022)o.Albedo=c.rgb*.8;
  }
'''+s[pos:];p.write_text(s)
p=Path('Assets/Ocean/Editor/RealisticReefBuilder.cs');s=p.read_text().replace('public static class RealisticReefBuilder','public static partial class RealisticReefBuilder').replace('path.EndsWith("PhotographicFish.fbx")','(path.EndsWith("PhotographicFish.fbx")||path.EndsWith("LoggerheadTurtle.fbx"))');s=s.replace('  Directory.CreateDirectory(Art+"Meshes")','  Directory.CreateDirectory(Art+"Meshes")')
s=s.replace('ImportTextures();Materials();Random.InitState','ImportTextures();Materials();VarietyMaterials();Random.InitState')
s=s.replace('float scale,Transform parent){\n if(name=="School member")','float scale,Transform parent,int variant=-1){\n if(name=="School member"&&variant>=0)return VarietyFish(variant,p,scale,parent);\n if(name=="School member")')
s=s.replace('var f=Fish("School member",spawn,mat,size*Random.Range(.82f,1.15f),root.transform);','int variant=count==7?i%6:mat==gold?4:mat==blue?1:5;var f=Fish("School member",spawn,mat,size*Random.Range(.82f,1.15f),root.transform,variant);')
a=s.index(' static void Turtle(');b=s.index(' static void Jelly(',a);s=s[:a]+' static void Turtle(Vector3 p,float angle){CreateLoggerhead(p,angle);}\n'+s[b:]
s=s.replace('  Debug.Log("REEF_RENDER_COMPLETE");','  Capture(cam,atmosphere,"08_Loggerhead_Closeup",new Vector3(-.1f,.6f,9.4f),new Vector3(1.7f,-.25f,7.5f));\n  Debug.Log("REEF_RENDER_COMPLETE");');p.write_text(s)
