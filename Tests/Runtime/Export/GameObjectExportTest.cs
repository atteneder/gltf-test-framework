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

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Export;
using GLTFast.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
#if GLTF_VALIDATOR
using Unity.glTF.Validator;
#endif // GLTF_VALIDATOR
#endif // UNITY_EDITOR

namespace GLTFTest {
    
    [TestFixture, Category("Export")]
    class GameObjectExportTest {
        
        const string k_PackagePath = "Packages/com.atteneder.gltf-test-framework/";
        const string k_NamesFile = "ExportScene.txt";

#if UNITY_EDITOR
        [MenuItem("Tools/Update glTF export object list")]
        static void UpdateObjectList() {
            var sceneName = GetExportSceneName();
            var scene = EditorSceneManager.OpenScene($"{k_PackagePath}Runtime/Export/Scenes/{sceneName}.unity");
            var rootObjects = scene.GetRootGameObjects();
            var names = new List<string>();
            for (var i = 0; i < rootObjects.Length; i++) {
                if (rootObjects[i].hideFlags != HideFlags.None) {
                    continue;
                }
                names.Add(rootObjects[i].name);
            }
            Assert.AreEqual(46,names.Count);
            var path = Path.Combine(Application.streamingAssetsPath, k_NamesFile);
            File.WriteAllLines(path,names.ToArray());
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset($"{k_PackagePath}Tests/Runtime/glTF-test-framework.Tests.asmdef", ImportAssetOptions.ForceUpdate);
        }
#endif
        
        [OneTimeSetUp]
        public void SetupTest() {
            SceneManager.LoadScene( GetExportSceneName(), LoadSceneMode.Single);
        }

        [UnityTest]
        public IEnumerator SimpleTree() {

            var root = new GameObject("root");
            var childA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            childA.name = "child A";
            var childB = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            childB.name = "child B";
            childA.transform.parent = root.transform;
            childB.transform.parent = root.transform;
            childB.transform.localPosition = new Vector3(1, 0, 0);

            var logger = new CollectingLogger();
            var export = new GameObjectExport(logger:logger);
            export.AddScene(new []{root}, "UnityScene");
            var path = Path.Combine(Application.persistentDataPath, "root.gltf");
            var task = export.SaveToFileAndDispose(path);
            yield return Utils.WaitForTask(task);
            var success = task.Result;
            Assert.IsTrue(success);
            AssertLogger(logger);
#if GLTF_VALIDATOR && UNITY_EDITOR
            ValidateGltf(path, MessageCode.UNUSED_OBJECT);
#endif
        }
        
        [UnityTest,SceneRootObjectTestCase(k_NamesFile)]
        public IEnumerator ExportSceneJson(int index, string objectName) {
            var gameObject = GetGameObject(index, objectName);
            var task = ExportSceneGameObject(gameObject, false);
            yield return Utils.WaitForTask(task);
        }

        [UnityTest,SceneRootObjectTestCase(k_NamesFile)]
        public IEnumerator ExportSceneBinary(int index, string objectName) {
            var gameObject = GetGameObject(index, objectName);
            var task = ExportSceneGameObject(gameObject, true);
            yield return Utils.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ExportSceneAllJson() {
            yield return null;
            var task = ExportSceneAll(false);
            yield return Utils.WaitForTask(task);
        }
        
        [UnityTest]
        public IEnumerator ExportSceneAllBinary() {
            yield return null;
            var task = ExportSceneAll(true);
            yield return Utils.WaitForTask(task);
        }
        
        [UnityTest,SceneRootObjectTestCase(k_NamesFile)]
        public IEnumerator ExportSceneBinaryStream(int index, string objectName) {
            var gameObject = GetGameObject(index, objectName);
            var task = ExportSceneGameObject(gameObject, true, true);
            yield return Utils.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ExportSceneAllBinaryStream() {
            yield return null;
            var task = ExportSceneAll(true, true);
            yield return Utils.WaitForTask(task);
        }
        
        [Test]
        public void MeshMaterialCombinationTest() {

            var mc1 = new MeshMaterialCombination(42,new [] {1,2,3});
            var mc2 = new MeshMaterialCombination(42,new [] {1,2,3});

            Assert.AreEqual(mc1,mc2);

            mc1 = new MeshMaterialCombination(42,new [] {1,2,4});
            Assert.AreNotEqual(mc1,mc2);
            
            mc1 = new MeshMaterialCombination(42,new [] {1,2});
            Assert.AreNotEqual(mc1,mc2);
            
            mc1 = new MeshMaterialCombination(42,null);
            Assert.AreNotEqual(mc1,mc2);
            
            mc2 = new MeshMaterialCombination(42,null);
            Assert.AreEqual(mc1,mc2);
            
            mc1 = new MeshMaterialCombination(13,null);
            Assert.AreNotEqual(mc1,mc2);
        }
        
        [UnityTest]
        public IEnumerator TwoScenes() {

            var childA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            childA.name = "child A";

            var logger = new CollectingLogger();
            var export = new GameObjectExport(logger:logger);
            export.AddScene(new []{childA}, "scene A");
            export.AddScene(new []{childA}, "scene B");
            var path = Path.Combine(Application.persistentDataPath, "TwoScenes.gltf");
            var task = export.SaveToFileAndDispose(path);
            yield return Utils.WaitForTask(task);
            var success = task.Result;
            Assert.IsTrue(success);
            AssertLogger(logger);
#if GLTF_VALIDATOR && UNITY_EDITOR
            ValidateGltf(path, MessageCode.UNUSED_OBJECT);
#endif
        }
        
        [UnityTest]
        public IEnumerator Empty() {

            var logger = new CollectingLogger();
            var export = new GameObjectExport(logger:logger);
            var path = Path.Combine(Application.persistentDataPath, "Empty.gltf");
            var task = export.SaveToFileAndDispose(path);
            yield return Utils.WaitForTask(task);
            var success = task.Result;
            Assert.IsTrue(success);
            AssertLogger(logger);
#if GLTF_VALIDATOR && UNITY_EDITOR
            ValidateGltf(path, MessageCode.UNUSED_OBJECT);
#endif
        }

        [UnityTest]
        public IEnumerator ComponentMask() {

            var root = new GameObject("Root");
            
            var meshGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meshGo.name = "Mesh";
            meshGo.transform.SetParent(root.transform);
            meshGo.transform.localPosition = new Vector3(0, 0, 0);
            
            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(root.transform);
            lightGo.transform.localPosition = new Vector3(1, 0, 0);
            lightGo.AddComponent<Light>();
            
            var cameraGo = new GameObject("Camera");
            cameraGo.transform.SetParent(root.transform);
            cameraGo.transform.localPosition = new Vector3(.5f, 0, -3);
            cameraGo.AddComponent<Camera>();
            
            // Export no components
            var task = ExportTest(
                new[]{root},
                "ComponentMaskNone",
                new ExportSettings {
                    componentMask = ComponentType.None
                });
            yield return Utils.WaitForTask(task);
            
            // Export mesh only
            task = ExportTest(
                new[]{root},
                "ComponentMaskMesh",
                new ExportSettings {
                    componentMask = ComponentType.Mesh
                });
            yield return Utils.WaitForTask(task);
            
            // Export light only
            task = ExportTest(
                new[]{root},
                "ComponentMaskLight",
                new ExportSettings {
                    componentMask = ComponentType.Light
                });
            yield return Utils.WaitForTask(task);
            
            // Export Camera only
            task = ExportTest(
                new[]{root},
                "ComponentMaskCamera",
                new ExportSettings {
                    componentMask = ComponentType.Camera
                });
            yield return Utils.WaitForTask(task);
            
            // Clean up
            GameObject.Destroy(root);
        }

        [UnityTest]
        public IEnumerator LayerMask() {

            var root = new GameObject("Root");
            
            var childA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            childA.name = "a";
            childA.transform.SetParent(root.transform);
            childA.transform.localPosition = new Vector3(0, 0, 0);
            
            var childB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            childB.name = "b";
            childB.transform.SetParent(childA.transform);
            childB.transform.localPosition = new Vector3(1, 0, 0);
            
            var childC = GameObject.CreatePrimitive(PrimitiveType.Cube);
            childC.name = "c";
            childC.transform.SetParent(childB.transform);
            childC.transform.localPosition = new Vector3(1, 0, 0);

            childA.layer = 1; // On layer 0
            childB.layer = 1; // On layer 0
            childC.layer = 2; // On layer 1

            // Export all layers
            var task = ExportTest(
                new[]{root},
                "LayerMaskAll",
                gameObjectExportSettings: new GameObjectExportSettings {
                    layerMask = ~0
                });
            yield return Utils.WaitForTask(task);
            
            // Export layer 1
            task = ExportTest(
                new[]{root},
                "LayerMaskOne",
                gameObjectExportSettings: new GameObjectExportSettings {
                    layerMask = 1
                });
            yield return Utils.WaitForTask(task);
            
            // Export layer 2
            task = ExportTest(
                new[]{root},
                "LayerMaskTwo",
                gameObjectExportSettings: new GameObjectExportSettings {
                    layerMask = 2
                });
            yield return Utils.WaitForTask(task);
            
            // Export no layer
            task = ExportTest(
                new[]{root},
                "LayerMaskNone",
                gameObjectExportSettings: new GameObjectExportSettings {
                    layerMask = 0
                });
            yield return Utils.WaitForTask(task);
            
            // Clean up
            GameObject.Destroy(root);
        }

        [UnityTest]
        public IEnumerator SavedTwice() {

            var childA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            childA.name = "child A";

            var logger = new CollectingLogger();
            var export = new GameObjectExport(logger:logger);
            export.AddScene(new []{childA});
            var path = Path.Combine(Application.persistentDataPath, "SavedTwice1.gltf");
            var task = export.SaveToFileAndDispose(path);
            yield return Utils.WaitForTask(task);
            var success = task.Result;
            Assert.IsTrue(success);
            AssertLogger(logger);
#if GLTF_VALIDATOR && UNITY_EDITOR
            ValidateGltf(path, MessageCode.UNUSED_OBJECT);
#endif
            Assert.Throws<InvalidOperationException>(delegate {
                export.AddScene(new []{childA});
            });
            path = Path.Combine(Application.persistentDataPath, "SavedTwice2.gltf");
            AssertThrowsAsync<InvalidOperationException>(async () => await export.SaveToFileAndDispose(path));
        }

        static async Task ExportTest(
            GameObject[] objects,
            string testName,
            ExportSettings exportSettings = null,
            GameObjectExportSettings gameObjectExportSettings = null
            )
        {
            var logger = new CollectingLogger();
            var export = new GameObjectExport(
                exportSettings: exportSettings,
                gameObjectExportSettings: gameObjectExportSettings,
                logger: logger
                );
            export.AddScene(objects);
            var resultPath = Path.Combine(Application.persistentDataPath, $"{testName}.gltf");
            var success = await export.SaveToFileAndDispose(resultPath);
            Assert.IsTrue(success);
            AssertLogger(logger);
#if GLTF_VALIDATOR && UNITY_EDITOR
            ValidateGltf(resultPath, MessageCode.UNUSED_OBJECT);
#endif
            // AssertGltfJson($"{testName}.gltf", resultPath);
        }

        static GameObject GetGameObject(int index, string objectName) {
            var scene = SceneManager.GetActiveScene();
            var objects = scene.GetRootGameObjects();
            var gameObject = objects[index];
            if (gameObject.name != objectName) {
                // GameObject order is not deterministic in builds, so here we
                // search by traversing all root objects.
                foreach (var obj in objects) {
                    if (obj.name == objectName) {
                        gameObject = obj;
                        break;
                    }
                }
            }

            Assert.NotNull(gameObject);
            Assert.AreEqual(objectName, gameObject.name);
            return gameObject;
        }
        
        static async Task ExportSceneGameObject(GameObject gameObject, bool binary, bool toStream = false) {
            var logger = new CollectingLogger();
            var export = new GameObjectExport(
                new ExportSettings {
                    format = binary ? GltfFormat.Binary : GltfFormat.Json,
                    fileConflictResolution = FileConflictResolution.Overwrite,
                },
                logger: logger
            );
            export.AddScene(new []{gameObject}, gameObject.name);
            var extension = binary ? GltfGlobals.glbExt : GltfGlobals.gltfExt;
            var fileName = $"{gameObject.name}{extension}";
            var path = Path.Combine(Application.persistentDataPath, fileName);

            bool success;
            if (toStream) {
                var glbStream = new MemoryStream();
                success = await export.SaveToStreamAndDispose(glbStream);
                Assert.Greater(glbStream.Length,20);
                glbStream.Close();
            }
            else {
                success = await export.SaveToFileAndDispose(path);
            }
            Assert.IsTrue(success);
            AssertLogger(logger);

#if UNITY_EDITOR
            if (!binary) {
                AssertGltfJson(fileName, path);
            }
#endif

#if GLTF_VALIDATOR && UNITY_EDITOR
            ValidateGltf(path, new [] {
                MessageCode.ACCESSOR_MAX_MISMATCH,
                MessageCode.ACCESSOR_MIN_MISMATCH,
                MessageCode.NODE_EMPTY,
                MessageCode.UNUSED_OBJECT,
            });
#endif
        }

        static void AssertGltfJson(string testName, string resultPath) {
            var pathPrefix = $"{k_PackagePath}Tests/Resources/ExportTargets";
#if UNITY_2020_2_OR_NEWER
            const string targetFolder = "Default";
#else
                const string targetFolder = "Legacy";
#endif

            var renderPipeline = RenderPipelineUtils.renderPipeline;
            string rpSubfolder;
            switch (renderPipeline) {
                case RenderPipeline.Universal:
                    rpSubfolder = "/URP";
                    break;
                case RenderPipeline.HighDefinition:
                    rpSubfolder = "/HDRP";
                    break;
                default:
                    rpSubfolder = "";
                    break;
            }

            var assetPath = $"{pathPrefix}/{targetFolder}{rpSubfolder}/{testName}.txt";
            var targetJsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (targetJsonAsset == null) {
                assetPath = $"{pathPrefix}/{targetFolder}/{testName}.txt";
                targetJsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            }

            Assert.NotNull(targetJsonAsset, $"Target glTF JSON for {testName} was not found");
            var actualJson = GltfJsonSetGenerator(File.ReadAllText(resultPath));
            var targetJson = GltfJsonSetGenerator(targetJsonAsset.text);
            Assert.AreEqual(targetJson, actualJson, $"JSON did not match for {testName}");
        }

        static async Task ExportSceneAll(bool binary, bool toStream = false) {
            var sceneName = GetExportSceneName();
            SceneManager.LoadScene( sceneName, LoadSceneMode.Single);

            var scene = SceneManager.GetActiveScene();

            var rootObjects = scene.GetRootGameObjects();

            var logger = new CollectingLogger();
            var export = new GameObjectExport(
                new ExportSettings {
                    format = binary ? GltfFormat.Binary : GltfFormat.Json,
                    fileConflictResolution = FileConflictResolution.Overwrite,
                },
                logger: logger
            );
            export.AddScene(rootObjects, sceneName);
            var extension = binary ? GltfGlobals.glbExt : GltfGlobals.gltfExt;
            var path = Path.Combine(Application.persistentDataPath, $"ExportScene{extension}");

            bool success;
            if (toStream) {
                var glbStream = new MemoryStream();
                success = await export.SaveToStreamAndDispose(glbStream);
                Assert.Greater(glbStream.Length,20);
                glbStream.Close();
            }
            else {
                success = await export.SaveToFileAndDispose(path);
            }
            Assert.IsTrue(success);
            AssertLogger(logger);
#if GLTF_VALIDATOR && UNITY_EDITOR
            ValidateGltf(path, new [] {
                MessageCode.ACCESSOR_ELEMENT_OUT_OF_MAX_BOUND,
                MessageCode.ACCESSOR_MAX_MISMATCH,
                MessageCode.ACCESSOR_MIN_MISMATCH,
                MessageCode.NODE_EMPTY,
                MessageCode.UNUSED_OBJECT,
            });
#endif
        }

        static void AssertLogger(CollectingLogger logger) {
            logger.LogAll();
            if (logger.Count > 0) {
                foreach (var item in logger.Items) {
#if !UNITY_IMAGECONVERSION
                    if (item.type == LogType.Warning && item.code == LogCode.ImageConversionNotEnabled) {
                        continue;
                    }
#endif
                    Assert.AreEqual(LogType.Log, item.type, item.ToString());
                }
            }
        }

        /// <summary>
        /// Fill-in for NUnit's Assert.ThrowsAsync
        /// Source: https://forum.unity.com/threads/can-i-replace-upgrade-unitys-nunit.488580/#post-6543523
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        /// <typeparam name="TActual"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        static void AssertThrowsAsync<TActual>(AsyncTestDelegate code, string message = "", params object[] args) where TActual : Exception {
            Assert.Throws<TActual>(() =>
            {
                try
                {
                    code.Invoke().Wait(); // Will wrap any exceptions in an AggregateException
                }
                catch (AggregateException e)
                {
                    if (e.InnerException is null)
                    {
                        throw;
                    }
                    throw e.InnerException; // Throw the unwrapped exception
                }
            }, message, args);
        }

        delegate Task AsyncTestDelegate();


#if GLTF_VALIDATOR && UNITY_EDITOR
        static void ValidateGltf(string path, params MessageCode[] expectedMessages) {
            var report = Validator.Validate(path);
            Assert.NotNull(report, $"Report null for {path}");
            // report.Log();
            if (report.issues != null) {
                foreach (var message in report.issues.messages) {
                    if (((IList)expectedMessages).Contains(message.codeEnum)) {
                        continue;
                    }
                    Assert.Greater(message.severity, 0, $"Error {message} (path {Path.GetFileName(path)})");
                    Assert.Greater(message.severity, 1, $"Warning {message} (path {Path.GetFileName(path)})");
                }
            }
        }
#endif

        static string GetExportSceneName() {
            switch (RenderPipelineUtils.renderPipeline) {
                case RenderPipeline.HighDefinition:
                    return "ExportSceneHighDefinition";
                case RenderPipeline.Universal:
                    return "ExportSceneUniversal";
                default:
                    return "ExportSceneBuiltIn";
            }
        }
        
        /// <summary>
        /// Takes the JSON portion of a glTF file and sets/replaces the
        /// `asset.generator` property (if found) by another string.
        /// </summary>
        /// <param name="json">glTF JSON</param>
        /// <param name="newGenerator">New generator value.</param>
        /// <returns>glTF JSON with replaced asset.generator property.</returns>
        static string GltfJsonSetGenerator(string json, string newGenerator="") {
            const string searchKey = "\"generator\"";
            var start= json.IndexOf(searchKey);
            if (start < 0) return json;
            start += searchKey.Length;
            start = json.IndexOf('"', start);
            start++;
            var end = json.IndexOf('"', start);
            var sb = new StringBuilder();
            sb.Append(json, 0, start);
            sb.Append(newGenerator);
            sb.Append(json, end, json.Length-end);
            return sb.ToString();
        }
    }
}
