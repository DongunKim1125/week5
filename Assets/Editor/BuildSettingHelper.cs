using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class BuildSettingsHelper
{
    [MenuItem("Tools/Tiler Swift/Add All Scenes to Build Settings")]
    public static void AddAllScenesToBuildSettings()
    {
        // 1. 프로젝트 내의 모든 씬 파일의 GUID를 찾습니다.
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        
        // 2. GUID를 실제 경로(Path)로 변환하고 EditorBuildSettingsScene 객체로 만듭니다.
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        
        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            // 패키지 폴더 등에 들어있는 기본 씬들을 제외하고 싶다면 여기서 필터링 가능합니다.
            if (scenePath.StartsWith("Assets/"))
            {
                buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
        }

        // 3. 빌드 세팅에 적용합니다.
        EditorBuildSettings.scenes = buildScenes.ToArray();

        Debug.Log($"[Tiler Swift] 총 {buildScenes.Count}개의 씬이 빌드 설정에 추가되었습니다!");
    }
}