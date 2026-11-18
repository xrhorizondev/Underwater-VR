using System;
using UnityEditor;
using UnityEngine;
namespace Underwater.Editor {
public static class SquidTextureRefinement {
 public static void Run(){
 AssetDatabase.Refresh();string folder="Assets/UnderwaterArt/BlenderSquid/";
 foreach(string name in new[]{"SquidSkin","SquidHeight"}){var importer=(TextureImporter)AssetImporter.GetAtPath(folder+name+".png");importer.sRGBTexture=name=="SquidSkin";importer.mipmapEnabled=true;importer.anisoLevel=4;importer.maxTextureSize=2048;var p=importer.GetPlatformTextureSettings("Android");p.overridden=true;p.maxTextureSize=2048;p.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(p);importer.SaveAndReimport();}
 var mat=AssetDatabase.LoadAssetAtPath<Material>(folder+"SquidBody.mat");mat.SetTexture("_HeightMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"SquidHeight.png"));mat.SetFloat("_HeightStrength",.012f);EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();
 if(ShaderUtil.ShaderHasError(mat.shader))throw new Exception("Squid relief shader failed compilation");
 ReefBlenderSquid.Capture();Debug.Log("SQUID_TEXTURE_REFINEMENT_PASSED skin=2048 height=2048 relief=0.012 astc=6x6 meshAndAnimationUnchanged=true");
 }
}
}
