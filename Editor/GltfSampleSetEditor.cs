// Copyright 2020-2022 Andreas Atteneder
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System.IO;
using UnityEditor;
using UnityEngine;

#if GLTFAST_RENDER_TEST
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using GLTFast;
using UnityEditor.SceneManagement;
#endif

namespace GLTFTest.Editor {

    using Sample;

    [CustomEditor(typeof(SampleSet))]
    public class SampleSetEditor : UnityEditor.Editor
    {
        private SampleSet _sampleSet;
        private string searchPattern = "*.gl*";

        public void OnEnable() {
            _sampleSet = (SampleSet)target;
        }

        public override void OnInspectorGUI() {
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search Pattern");
            searchPattern = GUILayout.TextField(searchPattern);
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Find in path")) {
                _sampleSet.LoadItemsFromPath(searchPattern);
            }
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Activate All")) {
                _sampleSet.SetAllActive();
            }
            if (GUILayout.Button("Deactivate All")) {
                _sampleSet.SetAllActive(false);
            }
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Create JSONs")) {
                CreateJSON(_sampleSet,target);
            }
            
            if (GUILayout.Button("Create list file")) {
                CreateListFile(_sampleSet,target);
            }
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create render test scenes")) {
                CreateRenderTestScenes(_sampleSet);
            }
            
            if (GUILayout.Button("Create single test scene")) {
                CreateRenderSingleTestScene(_sampleSet);
            }
            GUILayout.EndHorizontal();
            
            base.OnInspectorGUI();
            
            if (GUI.changed) {
                EditorUtility.SetDirty(_sampleSet);
            }
        }
        
        public static void CreateRenderTestScenes(SampleSet sampleSet)
        {
    #if GLTFAST_RENDER_TEST
            var allScenes = new List<EditorBuildSettingsScene>();
            var allScenePaths = new List<string>();
            // Texture2D dummyReference = null;

            foreach (var item in sampleSet.GetItems())
            {
                var testScene = EditorSceneManager.OpenScene("Assets/Scenes/TestScene.unity");
                
                var settingsGameObject = new GameObject("GraphicsTestSettings");
                var graphicsTestSettings = settingsGameObject.AddComponent<UniversalGraphicsTestSettings>();

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
                
                var sceneDirectory = CertifyDirectory(item.directoryParts, string.Format("Assets/Scenes/{0}", sampleSet.name));
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

        static void CreateRenderSingleTestScene(SampleSet sampleSet)
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

        static void CreateJSON(SampleSet sampleSet, Object target) {
            sampleSet.CreateJSON();
        }

        static void CreateListFile(SampleSet sampleSet, Object target) {
            sampleSet.CreateListFile();
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
