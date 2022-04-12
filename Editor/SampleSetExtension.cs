using System.Collections.Generic;
using System.IO;
using GLTFast;
using GLTFTest;
using GLTFTest.Sample;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GLTFTest.Editor {

    public static class SampleSetExtension
    {
        public static void CreateRenderTestScenes(this SampleSet sampleSet)
        {
#if GLTFAST_RENDER_TEST
            var allScenes = new List<EditorBuildSettingsScene>();
            var allScenePaths = new List<string>();
            // Texture2D dummyReference = null;
            var setName = sampleSet.name;

            var renderPipeline = RenderPipelineUtils.DetectRenderPipeline();
            
            foreach (var item in sampleSet.GetItems())
            {
                var testScene = EditorSceneManager.OpenScene("Assets/Scenes/TestScene.unity");
                
                var settingsGameObject = new GameObject("GraphicsTestSettings");
                var graphicsTestSettings = settingsGameObject.AddComponent<UniversalGraphicsTestSettings>();
                graphicsTestSettings.WaitFrames = renderPipeline== RenderPipeline.HighDefinition ? 2 : 0;

                var go = new GameObject(item.name);
                var gltfAsset = go.AddComponent<GltfBoundsAsset>();
                
                if(string.IsNullOrEmpty(sampleSet.streamingAssetsPath)) {
                    gltfAsset.url = Path.Combine(sampleSet.baseLocalPath, item.path);
                } else {
                    gltfAsset.url = Path.Combine(sampleSet.streamingAssetsPath, item.path);
                    gltfAsset.streamingAsset = true;
                }
                gltfAsset.loadOnStartup = false;
                gltfAsset.createBoxCollider = false;
                
                var sceneDirectory = CertifyDirectory(item.directoryParts, string.Format("Assets/Scenes/{0}", setName));
                var scenePath = Path.Combine(sceneDirectory, item.name+".unity");

                EditorSceneManager.SaveScene(testScene,scenePath);
                allScenes.Add(new EditorBuildSettingsScene(scenePath,true));
                allScenePaths.Add(scenePath);
                
                // CertifyDirectory(new[] { "ReferenceImages", "Linear", "OSXEditor", "Metal", "None" }, "Assets");
                // var referenceImagePath =
                //     Path.Combine(Application.dataPath, "ReferenceImages/Linear/OSXEditor/Metal/None", item.name + ".png");
                //
                // if (!File.Exists(referenceImagePath)) {
                //     Debug.LogFormat("Create dummy reference at path {0}", referenceImagePath);
                //     dummyReference = dummyReference!=null
                //         ? dummyReference
                //         : new Texture2D(
                //         graphicsTestSettings.ImageComparisonSettings.TargetWidth,
                //         graphicsTestSettings.ImageComparisonSettings.TargetHeight
                //     );
                //     File.WriteAllBytes(referenceImagePath, dummyReference.EncodeToPNG());
                // }
            }
            AssetDatabase.Refresh();
            EditorBuildSettings.scenes = allScenes.ToArray();
            
            Lightmapping.BakeMultipleScenes(allScenePaths.ToArray());
#else
            Debug.LogWarning("Please install the Graphics Test Framework for render tests to work.");
#endif
        }
    
        public static void CreateRenderSingleTestScene(this SampleSet sampleSet)
        {
#if GLTFAST_RENDER_TEST
            // Texture2D dummyReference = null;

            var testScene = EditorSceneManager.OpenScene("Assets/Scenes/TestScene.unity");
                
            foreach (var item in sampleSet.GetItems()) {
                    
                // var settingsGameObject = new GameObject("GraphicsTestSettings");
                // var graphicsTestSettings = settingsGameObject.AddComponent<UniversalGraphicsTestSettings>();

                var go = new GameObject(item.name);
                var gltfAsset = go.AddComponent<GltfBoundsAsset>();
                    
                if(string.IsNullOrEmpty(sampleSet.streamingAssetsPath)) {
                    gltfAsset.url = Path.Combine(sampleSet.baseLocalPath, item.path);
                } else {
                    gltfAsset.url = Path.Combine(sampleSet.streamingAssetsPath, item.path);
                    gltfAsset.streamingAsset = true;
                }
                gltfAsset.loadOnStartup = true;
                gltfAsset.createBoxCollider = false;
            }
            var scenePath = string.Format("Assets/Scenes/{0}.unity", sampleSet.name);
            EditorSceneManager.SaveScene(testScene,scenePath);
            AssetDatabase.Refresh();
#else
            Debug.LogWarning("Please install the Graphics Test Framework for render tests to work.");
#endif
        }
    
        static string CertifyDirectory(string[] directoryParts, string directoyPath) {
            if (!AssetDatabase.IsValidFolder(directoyPath)) {
                AssetDatabase.CreateFolder( Path.GetDirectoryName(directoyPath), Path.GetFileName(directoyPath));
            }
            foreach (var dirPart in directoryParts)
            {
                var newFolder = Path.Combine(directoyPath, dirPart);
                if (!AssetDatabase.IsValidFolder(newFolder))
                {
                    AssetDatabase.CreateFolder(directoyPath, dirPart);
                }

                directoyPath = newFolder;
            }

            return directoyPath;
        }
    }
}
