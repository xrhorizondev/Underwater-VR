#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Local, profile-driven Unity build entry point for Codex and PowerShell.
/// The default path is read-only validation: project settings are never changed
/// unless -codexAllowTargetSwitch is explicitly supplied.
/// </summary>
public static class CodexBuildAutomation
{
    [Serializable]
    private sealed class Profile
    {
        public int schemaVersion;
        public string name;
        public string displayName;
        public string buildTarget;
        public string artifactKind;
        public string artifactExtension;
        public string applicationIdentifier;
        public string[] scenes;
        public string orientation;
        public string scriptingBackend;
        public string[] requiredArchitectures;
        public int[] supportedUnityMajorVersions;
        public UrpChecks urp;
        public SigningChecks signing;
        public ObbChecks obb;
        public ValidationChecks validation;
        public bool developmentBuild;
    }

    [Serializable]
    private sealed class UrpChecks
    {
        public bool required;
        public float minRenderScale;
        public float maxRenderScale;
        public bool requireHdrOff;
    }

    [Serializable]
    private sealed class SigningChecks
    {
        public bool requireCustomKeystore;
        public bool disallowDebugSigning;
    }

    [Serializable]
    private sealed class ObbChecks
    {
        public bool enabled;
        public bool required;
        public int expectedVersionCode;
        public string packageIdentifier;
    }

    [Serializable]
    private sealed class ValidationChecks
    {
        public bool requireArm64Library;
        public bool requireAndroidManifest;
        public bool requireNonEmptyArtifact;
    }

    [Serializable]
    private sealed class Diagnostic
    {
        public string severity;
        public string code;
        public string message;
    }

    [Serializable]
    private sealed class ValidationReport
    {
        public string status;
        public string profile;
        public string displayName;
        public string unityVersion;
        public string buildTarget;
        public string artifact;
        public string validationMode;
        public string[] scenes;
        public Diagnostic[] diagnostics;
        public string buildResult;
    }

    private static readonly List<Diagnostic> Diagnostics = new List<Diagnostic>();

    public static void Build()
    {
        RunBuild(validateOnly: HasArgument("-codexValidateOnly"));
    }

    public static void Validate()
    {
        RunBuild(validateOnly: true);
    }

    private static void RunBuild(bool validateOnly)
    {
        Diagnostics.Clear();
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var profileName = GetArgument("-codexProfile", "quest");
        var validationMode = GetArgument("-codexValidation", "standard").ToLowerInvariant();
        var reportPath = GetArgument("-codexReport", Path.Combine(projectRoot, "Builds", "Codex", profileName + ".unity-report.json"));
        var outputPath = GetArgument("-codexOutput", Path.Combine(projectRoot, "Builds", "Codex", profileName + ".bin"));
        var profilePath = GetArgument("-codexProfilePath", Path.Combine(projectRoot, "CodexBuild", "Profiles", profileName + ".json"));

        try
        {
            var profile = LoadProfile(profilePath);
            var target = ParseBuildTarget(profile.buildTarget);
            var scenes = ResolveScenes(projectRoot, profile);

            ValidateUnityVersion(profile);
            ValidateActiveTarget(target);
            ValidateScenes(projectRoot, scenes);
            ValidateProfileBasics(profile, target);
            ValidateUrp(profile);
            ValidatePlatformSettings(projectRoot, profile, target);

            var report = new ValidationReport
            {
                status = HasErrors() ? "failed" : "passed",
                profile = profile.name,
                displayName = profile.displayName,
                unityVersion = Application.unityVersion,
                buildTarget = target.ToString(),
                artifact = outputPath,
                validationMode = validationMode,
                scenes = scenes.ToArray(),
                diagnostics = Diagnostics.ToArray(),
                buildResult = validateOnly ? "not-run" : "pending"
            };

            if (HasErrors())
            {
                report.buildResult = "blocked-by-validation";
                WriteReport(reportPath, report);
                LogSummary(report);
                EditorApplication.Exit(2);
                return;
            }

            if (validateOnly)
            {
                report.buildResult = "not-run-validation-only";
                WriteReport(reportPath, report);
                LogSummary(report);
                EditorApplication.Exit(0);
                return;
            }

            EnsureOutputDirectory(outputPath);
            var buildOptions = BuildOptions.None;
            if (profile.developmentBuild)
            {
                buildOptions |= BuildOptions.Development;
            }

            var buildReport = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = outputPath,
                target = target,
                options = buildOptions
            });

            report.buildResult = buildReport.summary.result.ToString();
            report.status = buildReport.summary.result == BuildResult.Succeeded ? "passed" : "failed";
            if (buildReport.summary.result != BuildResult.Succeeded)
            {
                AddError("BUILD_FAILED", "Unity reported a non-successful build result: " + buildReport.summary.result);
            }
            report.diagnostics = Diagnostics.ToArray();
            report.status = HasErrors() ? "failed" : "passed";
            WriteReport(reportPath, report);
            LogSummary(report);
            EditorApplication.Exit(HasErrors() ? 3 : 0);
        }
        catch (Exception exception)
        {
            AddError("UNHANDLED_EXCEPTION", exception.GetType().Name + ": " + exception.Message);
            var failure = new ValidationReport
            {
                status = "failed",
                profile = profileName,
                displayName = profileName,
                unityVersion = Application.unityVersion,
                buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                artifact = outputPath,
                validationMode = validationMode,
                scenes = new string[0],
                diagnostics = Diagnostics.ToArray(),
                buildResult = "exception"
            };
            WriteReport(reportPath, failure);
            LogSummary(failure);
            EditorApplication.Exit(4);
        }
    }

    private static Profile LoadProfile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Build profile was not found.", path);
        }

        var profile = JsonUtility.FromJson<Profile>(File.ReadAllText(path));
        if (profile == null || profile.schemaVersion != 1)
        {
            throw new InvalidDataException("Unsupported or invalid build profile schema: " + path);
        }

        if (profile.urp == null) profile.urp = new UrpChecks();
        if (profile.signing == null) profile.signing = new SigningChecks();
        if (profile.obb == null) profile.obb = new ObbChecks();
        if (profile.validation == null) profile.validation = new ValidationChecks();
        if (profile.scenes == null) profile.scenes = new string[0];
        if (profile.requiredArchitectures == null) profile.requiredArchitectures = new string[0];
        return profile;
    }

    private static BuildTarget ParseBuildTarget(string value)
    {
        if (string.Equals(value, "Android", StringComparison.OrdinalIgnoreCase)) return BuildTarget.Android;
        if (string.Equals(value, "StandaloneWindows64", StringComparison.OrdinalIgnoreCase)) return BuildTarget.StandaloneWindows64;
        throw new InvalidDataException("Unsupported build target in profile: " + value);
    }

    private static List<string> ResolveScenes(string projectRoot, Profile profile)
    {
        var selected = new List<string>();
        if (profile.scenes != null && profile.scenes.Length > 0)
        {
            selected.AddRange(profile.scenes.Where(scene => !string.IsNullOrWhiteSpace(scene)));
        }
        else
        {
            selected.AddRange(EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path));
        }

        for (var index = 0; index < selected.Count; index++)
        {
            selected[index] = selected[index].Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(selected[index]))
            {
                selected[index] = selected[index].Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            selected[index] = selected[index].Replace(Path.DirectorySeparatorChar, '/');
        }
        return selected;
    }

    private static void ValidateUnityVersion(Profile profile)
    {
        var majorText = Application.unityVersion.Split('.')[0];
        int major;
        if (!int.TryParse(majorText, out major))
        {
            AddError("UNITY_VERSION_UNKNOWN", "Could not parse Unity version: " + Application.unityVersion);
            return;
        }

        if (profile.supportedUnityMajorVersions != null && profile.supportedUnityMajorVersions.Length > 0 &&
            !profile.supportedUnityMajorVersions.Contains(major))
        {
            AddError("UNITY_VERSION_UNSUPPORTED", "Profile does not support Unity major version " + major + ".");
        }
    }

    private static void ValidateActiveTarget(BuildTarget target)
    {
        if (EditorUserBuildSettings.activeBuildTarget == target) return;
        if (HasArgument("-codexAllowTargetSwitch"))
        {
            var group = target == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.Standalone;
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
            {
                AddError("TARGET_SWITCH_FAILED", "Unity could not switch to the requested target: " + target);
            }
            else
            {
                AddWarning("TARGET_SWITCHED", "Active target was switched in this Unity invocation: " + target);
            }
        }
        else
        {
            AddError("ACTIVE_TARGET_MISMATCH", "Active target is " + EditorUserBuildSettings.activeBuildTarget + "; profile requires " + target + ". Rerun with an explicitly approved target switch or select it in Unity.");
        }
    }

    private static void ValidateScenes(string projectRoot, List<string> scenes)
    {
        if (scenes.Count == 0)
        {
            AddError("NO_SCENES", "No enabled build scenes were found and the profile did not specify scenes.");
            return;
        }

        foreach (var scene in scenes)
        {
            var absolute = Path.Combine(projectRoot, scene.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolute))
            {
                AddError("SCENE_MISSING", "Build scene does not exist: " + scene);
            }
        }
    }

    private static void ValidateProfileBasics(Profile profile, BuildTarget target)
    {
        if (string.IsNullOrWhiteSpace(profile.name)) AddError("PROFILE_NAME_EMPTY", "Profile name is empty.");
        if (string.IsNullOrWhiteSpace(profile.artifactExtension)) AddWarning("ARTIFACT_EXTENSION_EMPTY", "Profile has no artifact extension.");
        if (target == BuildTarget.Android)
        {
            if (profile.validation.requireArm64Library && !ContainsIgnoreCase(profile.requiredArchitectures, "ARM64"))
            {
                AddError("PROFILE_ARM64_MISSING", "Android profile requires ARM64 validation but does not declare ARM64.");
            }
        }
    }

    private static void ValidatePlatformSettings(string projectRoot, Profile profile, BuildTarget target)
    {
        if (target != BuildTarget.Android) return;

        var group = BuildTargetGroup.Android;
        if (!string.IsNullOrWhiteSpace(profile.applicationIdentifier))
        {
            var actualIdentifier = PlayerSettings.GetApplicationIdentifier(group);
            if (!string.Equals(actualIdentifier, profile.applicationIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                AddError("PACKAGE_IDENTIFIER_MISMATCH", "Android package identifier does not match the profile.");
            }
        }

        if (ContainsIgnoreCase(profile.requiredArchitectures, "ARM64") &&
            (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
        {
            AddError("ARM64_DISABLED", "Android target architectures do not include ARM64.");
        }

        if (string.Equals(profile.scriptingBackend, "IL2CPP", StringComparison.OrdinalIgnoreCase) &&
            PlayerSettings.GetScriptingBackend(group) != ScriptingImplementation.IL2CPP)
        {
            AddError("IL2CPP_DISABLED", "Android scripting backend is not IL2CPP.");
        }

        ValidateOrientation(profile.orientation);

        var useCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        if (profile.signing.requireCustomKeystore && !useCustomKeystore)
        {
            AddError("CUSTOM_KEYSTORE_REQUIRED", "Profile requires a custom keystore, but Unity is configured for debug signing.");
        }
        if (profile.signing.disallowDebugSigning && !useCustomKeystore)
        {
            AddError("DEBUG_SIGNING_DISALLOWED", "Profile disallows debug signing, but Unity is not using a custom keystore.");
        }
        if (useCustomKeystore)
        {
            var keystoreName = PlayerSettings.Android.keystoreName;
            if (string.IsNullOrWhiteSpace(keystoreName))
            {
                AddError("KEYSTORE_PATH_EMPTY", "Unity reports custom signing enabled but no keystore path is configured.");
            }
            else
            {
                var absoluteKeystore = Path.IsPathRooted(keystoreName) ? keystoreName : Path.Combine(projectRoot, keystoreName);
                if (!File.Exists(absoluteKeystore))
                {
                    AddError("KEYSTORE_MISSING", "Configured custom keystore file was not found.");
                }
                else
                {
                    AddInfo("KEYSTORE_PRESENT", "Configured custom keystore exists; signing material was not read or printed.");
                }
            }
        }
    }

    private static void ValidateOrientation(string orientation)
    {
        var normalized = (orientation ?? "auto").ToLowerInvariant();
        if (normalized == "auto") return;

        var actual = PlayerSettings.defaultInterfaceOrientation;
        if (normalized == "landscape")
        {
            var landscape = actual == UIOrientation.LandscapeLeft || actual == UIOrientation.LandscapeRight || actual == UIOrientation.AutoRotation;
            if (!landscape) AddError("ORIENTATION_NOT_LANDSCAPE", "Android default orientation is not landscape-compatible.");
        }
        else if (normalized == "portrait")
        {
            var portrait = actual == UIOrientation.Portrait || actual == UIOrientation.PortraitUpsideDown || actual == UIOrientation.AutoRotation;
            if (!portrait) AddError("ORIENTATION_NOT_PORTRAIT", "Android default orientation is not portrait-compatible.");
        }
        else
        {
            AddWarning("ORIENTATION_UNCHECKED", "Unknown orientation policy in profile: " + orientation);
        }
    }

    private static void ValidateUrp(Profile profile)
    {
        if (!profile.urp.required) return;

        var pipeline = GraphicsSettings.defaultRenderPipeline;
        if (pipeline == null)
        {
            AddError("URP_ASSET_MISSING", "The project has no default render pipeline asset.");
            return;
        }

        var typeName = pipeline.GetType().FullName ?? pipeline.GetType().Name;
        if (typeName.IndexOf("UniversalRenderPipelineAsset", StringComparison.OrdinalIgnoreCase) < 0)
        {
            AddError("URP_ASSET_WRONG_TYPE", "The default render pipeline is not a Universal Render Pipeline asset.");
            return;
        }

        var renderScale = ReadFloatProperty(pipeline, "renderScale");
        if (renderScale.HasValue)
        {
            if (profile.urp.minRenderScale > 0f && renderScale.Value < profile.urp.minRenderScale)
                AddWarning("URP_RENDER_SCALE_LOW", "URP render scale is below the profile minimum.");
            if (profile.urp.maxRenderScale > 0f && renderScale.Value > profile.urp.maxRenderScale)
                AddWarning("URP_RENDER_SCALE_HIGH", "URP render scale is above the profile maximum.");
            AddInfo("URP_PRESENT", "URP asset detected; render scale is " + renderScale.Value.ToString("0.###") + ".");
        }
        else
        {
            AddInfo("URP_PRESENT", "URP asset detected; render-scale reflection was unavailable in this editor version.");
        }

        if (profile.urp.requireHdrOff)
        {
            var hdr = ReadBoolProperty(pipeline, "supportsHDR");
            if (hdr.HasValue && hdr.Value) AddWarning("URP_HDR_ENABLED", "URP HDR is enabled while the profile requests HDR off.");
        }
    }

    private static float? ReadFloatProperty(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (property == null || property.PropertyType != typeof(float)) return null;
        return (float)property.GetValue(instance, null);
    }

    private static bool? ReadBoolProperty(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (property == null || property.PropertyType != typeof(bool)) return null;
        return (bool)property.GetValue(instance, null);
    }

    private static bool ContainsIgnoreCase(string[] values, string expected)
    {
        return values != null && values.Any(value => string.Equals(value, expected, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureOutputDirectory(string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
    }

    private static void WriteReport(string path, ValidationReport report)
    {
        try
        {
            EnsureOutputDirectory(path);
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
        }
        catch (Exception exception)
        {
            Debug.LogError("[CODEX_BUILD] Could not write report: " + exception.Message);
        }
    }

    private static void LogSummary(ValidationReport report)
    {
        Debug.Log("[CODEX_BUILD] status=" + report.status + " profile=" + report.profile + " result=" + report.buildResult);
        foreach (var diagnostic in report.diagnostics)
        {
            var line = "[CODEX_BUILD] " + diagnostic.severity + " " + diagnostic.code + ": " + diagnostic.message;
            if (diagnostic.severity == "ERROR") Debug.LogError(line);
            else if (diagnostic.severity == "WARNING") Debug.LogWarning(line);
            else Debug.Log(line);
        }
    }

    private static bool HasErrors()
    {
        return Diagnostics.Any(diagnostic => diagnostic.severity == "ERROR");
    }

    private static void AddInfo(string code, string message)
    {
        Diagnostics.Add(new Diagnostic { severity = "INFO", code = code, message = message });
    }

    private static void AddWarning(string code, string message)
    {
        Diagnostics.Add(new Diagnostic { severity = "WARNING", code = code, message = message });
    }

    private static void AddError(string code, string message)
    {
        Diagnostics.Add(new Diagnostic { severity = "ERROR", code = code, message = message });
    }

    private static bool HasArgument(string name)
    {
        return Environment.GetCommandLineArgs().Any(argument => string.Equals(argument, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetArgument(string name, string fallback)
    {
        var arguments = Environment.GetCommandLineArgs();
        for (var index = 0; index < arguments.Length; index++)
        {
            if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase) && index + 1 < arguments.Length)
                return arguments[index + 1];
            if (arguments[index].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                return arguments[index].Substring(name.Length + 1);
        }
        return fallback;
    }
}
#endif
