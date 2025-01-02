using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    public static void BuildDedicatedServer()
    {
        var args = System.Environment.GetCommandLineArgs();

        string outputPath = "BuildOutput";

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-out")
            {
                outputPath = args[i + 1];
            }
        }

        Console.WriteLine($"Building dedicated server to {outputPath}");
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" }, // Include required scenes
            locationPathName = outputPath, // Output location
            target = BuildTarget.StandaloneWindows64, // Or StandaloneLinux64, as appropriate
            subtarget = (int)StandaloneBuildSubtarget.Server// For server mode without graphics
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Dedicated server build succeeded!");
        }
        else
        {
            Debug.LogError("Dedicated server build failed!");
        }
    }
}
