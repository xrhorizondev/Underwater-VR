using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ReferenceLionfishAsset
{
    const string Folder = "Assets/UnderwaterArt/ReferenceLionfish/";
    const string PreviewScene = "Assets/Scenes/Lionfish_Preview.unity";
    [MenuItem("Underwater/Open reference lionfish preview")]
    public static void OpenPreview()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(PreviewScene);
        Selection.activeGameObject = GameObject.Find("Reference Lionfish");
        if (SceneView.lastActiveSceneView)
            SceneView.lastActiveSceneView.Frame(new Bounds(Vector3.zero, Vector3.one * .65f), true);
    }
    public static void CreatePreview()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "ReferenceLionfish.prefab");
        if (!prefab) throw new Exception("Missing prepared lionfish prefab");
        var fish = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        fish.name = "Reference Lionfish";
        var camera = new GameObject("Preview Camera").AddComponent<Camera>();
        camera.tag = "MainCamera"; camera.transform.position = new Vector3(.48f, .23f, .65f);
        camera.transform.LookAt(new Vector3(0, .025f, 0));
        camera.orthographic = true; camera.orthographicSize = .29f;
        camera.nearClipPlane = .01f; camera.farClipPlane = 10;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.055f, .07f, .085f);
        foreach (var spec in new[] { new Vector4(40, -35, 0, 1.1f), new Vector4(20, 140, 0, .65f) }) {
            var light = new GameObject("Preview soft light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = spec.w;
            light.transform.rotation = Quaternion.Euler(spec.x, spec.y, spec.z);
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.35f, .35f, .35f);
        RenderSettings.fog = false;
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, PreviewScene);
        AssetDatabase.SaveAssets();
        Debug.Log("LIONFISH_PREVIEW_SCENE_CREATED " + PreviewScene);
    }
    [MenuItem("Underwater/Prepare reference lionfish asset")]
    public static void Prepare()
    {
        string modelPath = Folder + "Lionfish.fbx";
        AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.importAnimation = true; importer.importCameras = false; importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips) { clip.name = clip.takeName.Split('|').Last(); clip.loopTime = true; clip.wrapMode = WrapMode.Loop; }
        importer.clipAnimations = clips; importer.SaveAndReimport();
        var texImporter = (TextureImporter)AssetImporter.GetAtPath(Folder + "Lionfish_Albedo.png");
        texImporter.maxTextureSize = 1024; texImporter.mipmapEnabled = true; texImporter.alphaIsTransparency = true;
        texImporter.wrapMode = TextureWrapMode.Clamp; texImporter.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "Lionfish_Albedo.png");
        var shader = Shader.Find("Underwater/Reference Lionfish");
        if (!shader || UnityEditor.ShaderUtil.ShaderHasError(shader)) throw new Exception("Lionfish shader failed");
        var materials = new Material[2];
        for (int i = 0; i < 2; i++) {
            string path = Folder + (i == 0 ? "LionfishSkin.mat" : "LionfishFins.mat");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader; material.mainTexture = texture; material.SetFloat("_Cutout", i);
            material.SetFloat("_Cull", i == 0 ? 2 : 0); EditorUtility.SetDirty(material); materials[i] = material;
        }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var root = new GameObject("Reference Lionfish");
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(model); visual.transform.SetParent(root.transform, false);
        foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>()) {
            renderer.sharedMaterial = materials[renderer.name.Contains("membrane") ? 1 : 0];
            renderer.updateWhenOffscreen = false; renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 1.1f);
        }
        var player = visual.GetComponent<Animation>();
        foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")))
            player.AddClip(clip, clip.name);
        player.clip = player.GetClip("Swim"); player.playAutomatically = true;
        Transform jaw = visual.GetComponentsInChildren<Transform>().Single(t => t.name == "Jaw");
        Transform body = visual.GetComponentsInChildren<Transform>().Single(t => t.name == "Body");
        Vector3 forward = jaw.position - body.position;
        visual.transform.localRotation = Quaternion.Euler(0, -Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 0);
        root.AddComponent<ReferenceLionfishSwim>();
        if (!player.GetClip("Swim") || !player.GetClip("Idle") || !player.GetClip("Breath")) throw new Exception("Missing clips");
        int triangles = visual.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(r => r.sharedMesh.triangles.Length / 3);
        if (triangles > 3200 || triangles < 2000) throw new Exception("Unexpected geometry " + triangles);
        foreach (string name in new[] { "Idle", "Swim", "Breath" }) {
            var clip = player.GetClip(name);
            foreach (float phase in new[] { 0f, .125f, .375f, .625f, .875f, 1f }) {
                clip.SampleAnimation(visual, phase * clip.length);
                foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                    var mesh = skin.sharedMesh; var weights = mesh.boneWeights;
                    var matrices = skin.bones.Select((b, i) => b.localToWorldMatrix * mesh.bindposes[i]).ToArray();
                    var vertices = mesh.vertices;
                    for (int v = 0; v < vertices.Length; v++) {
                        var w = weights[v]; var p = vertices[v];
                        Vector3 world = matrices[w.boneIndex0].MultiplyPoint3x4(p) * w.weight0 + matrices[w.boneIndex1].MultiplyPoint3x4(p) * w.weight1 + matrices[w.boneIndex2].MultiplyPoint3x4(p) * w.weight2 + matrices[w.boneIndex3].MultiplyPoint3x4(p) * w.weight3;
                        if (!skin.bounds.Contains(world)) throw new Exception("Animated lionfish outside bounds: " + name);
                    }
                }
            }
        }
        player.GetClip("Swim").SampleAnimation(visual, 0);
        PrefabUtility.SaveAsPrefabAsset(root, Folder + "ReferenceLionfish.prefab");
        UnityEngine.Object.DestroyImmediate(root); AssetDatabase.SaveAssets();
        Debug.Log("REFERENCE_LIONFISH_UNITY_PASSED triangles=" + triangles + " clips=Idle,Swim,Breath materials=2");
    }
}
