using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WebGLBuild
{
    public static void Build()
    {
        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "docs");
        Directory.CreateDirectory(outputPath);

        string[] scenes = new string[]
        {
            "Assets/Scenes/Main Menu.unity",
            "Assets/Scenes/LoadingScene.unity",
            "Assets/Scenes/Level_1.unity",
            "Assets/Scenes/Level_2.unity"
        };

        Debug.Log("Building Fruity Snake WebGL export to: " + outputPath);

        string error = BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.WebGL, BuildOptions.None);

        if (!string.IsNullOrEmpty(error))
        {
            throw new Exception("WebGL build failed: " + error);
        }

        Debug.Log("Fruity Snake WebGL build completed.");
    }
}
