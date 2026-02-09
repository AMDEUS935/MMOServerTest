using UnityEditor;
using UnityEngine;

public class MultiPlayerTest
{
    [MenuItem("Tools/Build MultiPlayer/2 Players")]
    static void PerformWin64Build2()
    {
        PerformWinBuild(2);
    }

    [MenuItem("Tools/Build MultiPlayer/3 Players")]
    static void PerformWin64Build3()
    {
        PerformWinBuild(3);
    }

    [MenuItem("Tools/Build MultiPlayer/4 Players")]
    static void PerformWin64Build4()
    {
        PerformWinBuild(4);
    }

    static void PerformWinBuild(int playerCount)
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

        for (int i = 1; i <= playerCount; i++)
        {
            BuildPipeline.BuildPlayer(
                GetScenePaths(),
                $"Builds/Win64/{GetProjectName()}{i}/{GetProjectName()}{i}.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.AutoRunPlayer
            );
        }
    }

    static string GetProjectName()
    {
        string[] s = Application.dataPath.Split('/');
        return s[s.Length - 2];
    }

    static string[] GetScenePaths()
    {
        string[] scenes = new string[EditorBuildSettings.scenes.Length];

        for (int i = 0; i < scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }

        return scenes;
    }
}
