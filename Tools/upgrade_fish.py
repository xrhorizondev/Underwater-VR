from pathlib import Path
p=Path('Assets/Ocean/Editor/RealisticReefBuilder.cs');s=p.read_text();s=s.replace('static Material[] corals,scanPalette;','static Material[] corals,scanPalette,fishPalette;')
s=s.replace('imp.isReadable=false;','imp.isReadable=path.EndsWith("PhotographicFish.fbx");')
needle='sand=Mat("01 Calcareous reef sand"'
pos=s.index(needle)
s=s[:pos]+'''fishPalette=new Material[3];Color[] fishColors={new(.82f,.88f,.92f),new(.92f,.78f,.52f),new(.63f,.77f,.84f)};for(int i=0;i<3;i++){var m=new Material(Shader.Find("Underwater/Living Fish Skin"));m.mainTexture=Tex("FishScan_albedo.png");m.SetTexture("_BumpMap",Tex("FishScan_normal.png"));m.color=fishColors[i];fishPalette[i]=Save(m,Art+"Materials/Natural fish skin "+i+".mat");}
 '''+s[pos:]
a=s.index('  for(int i=0;i<4;i++){var fish=Fish(');b=s.index('  CreateDiver();',a)
s=s[:a]+'''  School(new Vector3(-.5f,-.3f,1),7,3,silver,.48f);
'''+s[b:]
a=s.index(' if(name=="School member"){');b=s.index('\n var f=Model(',a)
s=s[:a]+''' if(name=="School member"){
  var g=Model("Living reef fish",Mesh("PhotographicFishCanonical",CanonicalFish),fishPalette[mat==gold?1:mat==blue?2:0],p,Vector3.one*scale,parent);
  var a=g.AddComponent<FishAgent>();a.bodyLength=scale;a.cruiseSpeed=scale*Random.Range(1.2f,1.7f);return g;
 }
'''+s[b:]
a=s.index(' static void School(');b=s.index(' static void Turtle(',a)
s=s[:a]+''' static Mesh CanonicalFish(){
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Scans/PhotographicFish.fbx");
  var temp=(GameObject)PrefabUtility.InstantiatePrefab(source);
  var parts=temp.GetComponentsInChildren<MeshFilter>();var combine=new CombineInstance[parts.Length];
  for(int i=0;i<parts.Length;i++)combine[i]=new CombineInstance{mesh=parts[i].sharedMesh,transform=parts[i].transform.localToWorldMatrix};
  var mesh=new Mesh{name="Fish canonical: head +Z, tail -Z",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combine,true,true);UnityEngine.Object.DestroyImmediate(temp);
  mesh.RecalculateBounds();var bounds=mesh.bounds;var vertices=mesh.vertices;float length=bounds.size.z;
  if(length<bounds.size.y||length<.01f)throw new Exception("Fish import orientation invalid: "+bounds.size);
  for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-bounds.center)/length;
  mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.bounds=new Bounds(Vector3.zero,new Vector3(.55f,.65f,1.2f));return mesh;
 }
 static void School(Vector3 p,int count,float radius,Material mat,float size){
  var root=new GameObject("School | "+count+" reef fish");root.transform.SetParent(world);
  var school=root.AddComponent<FishSchool>();school.habitatCenter=p;school.habitatRadius=radius+2;
  for(int i=0;i<count;i++){
   Vector3 spawn=p+new Vector3(Random.Range(-radius,radius),Random.Range(-radius*.18f,radius*.18f),Random.Range(-radius*.6f,radius*.6f));
   var f=Fish("School member",spawn,mat,size*Random.Range(.82f,1.15f),root.transform);
   f.transform.rotation=Quaternion.Euler(0,Random.Range(65,105),0);
  }
 }
'''+s[b:]
p.write_text(s)
p=Path('Assets/Ocean/FishSchool.cs');s=p.read_text().replace('Mathf.Abs(GetInstanceID()%53)','Mathf.Abs(Mathf.RoundToInt(habitatCenter.z*3)%53)');p.write_text(s)
