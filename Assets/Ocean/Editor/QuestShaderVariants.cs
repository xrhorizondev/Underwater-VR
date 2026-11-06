using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
namespace Underwater.Editor {
// The Quest scene uses forward lighting, realtime hard directional shadows and exponential-squared fog.
public class QuestShaderVariants:IPreprocessShaders {
 public int callbackOrder=>0;
 static readonly HashSet<string> Unused=new HashSet<string>{"POINT","POINT_COOKIE","SPOT","DIRECTIONAL_COOKIE","SHADOWS_CUBE","SHADOWS_SOFT","LIGHTMAP_ON","DIRLIGHTMAP_COMBINED","DYNAMICLIGHTMAP_ON","LIGHTMAP_SHADOW_MIXING","SHADOWS_SHADOWMASK","FOG_LINEAR","FOG_EXP"};
 public void OnProcessShader(Shader shader,ShaderSnippetData snippet,IList<ShaderCompilerData> variants){
  if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android||!(shader.name.StartsWith("Underwater/")||shader.name=="Standard"||shader.name=="Standard (Specular setup)"))return;
  if(snippet.passType==PassType.ForwardAdd||snippet.passType==PassType.Deferred||snippet.passType==PassType.Meta){variants.Clear();return;}
  for(int i=variants.Count-1;i>=0;i--){foreach(var keyword in variants[i].shaderKeywordSet.GetShaderKeywords())if(Unused.Contains(keyword.name)){variants.RemoveAt(i);break;}}
 }
}
}

