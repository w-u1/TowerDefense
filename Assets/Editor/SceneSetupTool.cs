#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TowerDefense.Core;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 编辑器工具：提供菜单选项快速创建游戏场景。
    /// 菜单：Tools → Tower Defense → Create Game Scene
    /// </summary>
    public static class SceneSetupTool
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string SceneDirectory = "Assets/Scenes";

        [MenuItem("Tools/Tower Defense/Create Game Scene %#t")]
        public static void CreateGameScene()
        {
            // 确保目录存在
            if (!AssetDatabase.IsValidFolder(SceneDirectory))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            // 创建新场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 创建启动器物体
            var bootstrapperGo = new GameObject("GameBootstrapper");
            bootstrapperGo.AddComponent<GameBootstrapper>();

            // 保存场景
            EditorSceneManager.SaveScene(scene, ScenePath);

            // 添加到Build Settings
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[SceneSetupTool] 游戏场景已创建: {ScenePath}");
            EditorUtility.DisplayDialog("完成", "游戏场景创建成功！\n点击 Play 即可运行。", "确定");
        }

        [MenuItem("Tools/Tower Defense/Add Bootstrapper to Current Scene")]
        public static void AddBootstrapperToCurrentScene()
        {
            // 检查是否已存在
            var existing = Object.FindObjectOfType<GameBootstrapper>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("提示", "场景中已存在 GameBootstrapper。", "确定");
                return;
            }

            var go = new GameObject("GameBootstrapper");
            go.AddComponent<GameBootstrapper>();
            Selection.activeGameObject = go;

            Debug.Log("[SceneSetupTool] 已向当前场景添加 GameBootstrapper。");
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // 检查是否已存在
            bool exists = false;
            foreach (var s in scenes)
            {
                if (s.path == path)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
#endif
