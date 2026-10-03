#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 编辑器工具：Tower Defense 项目设置。
    /// 菜单：Tools → Tower Defense → Project Setup
    /// </summary>
    public static class ProjectSetupTool
    {
        [MenuItem("Tools/Tower Defense/Project Setup/Apply Recommended Settings")]
        public static void ApplyRecommendedSettings()
        {
            // 设置为2D项目
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;

            // 设置公司和产品名
            PlayerSettings.companyName = "TowerDefense";
            PlayerSettings.productName = "塔防大作战";

            // 设置默认分辨率
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;

            // 设置颜色空间为Gamma（2D项目推荐，与程序化生成的Sprite兼容更好）
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            // 注：API Compatibility Level 在 Unity 2022+ 默认为 .NET Standard 2.1，无需手动设置
            // 如需修改可在 Player Settings → Other Settings → Configuration 中调整

            Debug.Log("[ProjectSetupTool] 已应用推荐项目设置。");
            EditorUtility.DisplayDialog("完成", "推荐项目设置已应用。", "确定");
        }

        [MenuItem("Tools/Tower Defense/Project Setup/Open README")]
        public static void OpenReadme()
        {
            string readmePath = System.IO.Path.Combine(Application.dataPath, "..", "README.md");
            readmePath = System.IO.Path.GetFullPath(readmePath);
            if (System.IO.File.Exists(readmePath))
            {
                EditorUtility.RevealInFinder(readmePath);
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "README.md 未找到。", "确定");
            }
        }
    }
}
#endif
