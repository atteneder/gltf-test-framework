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
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Export;
using GLTFast.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using System.Text;
using UnityEditor;
#if GLTF_VALIDATOR
using Unity.glTF.Validator;
#endif // GLTF_VALIDATOR
#endif // UNITY_EDITOR

namespace GLTFTest {
    
    [TestFixture, Category("Export")]
    public class GameObjectExportTest {

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
        
        [UnityTest]
        public IEnumerator ExportSceneGameObjectsJson() {
            yield return null;
            var task = ExportSceneGameObjects(false);
            yield return Utils.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ExportSceneGameObjectsBinary() {
            yield return null;
            var task = ExportSceneGameObjects(true);
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
        
        [UnityTest]
        public IEnumerator ExportSceneGameObjectsBinaryStream() {
            yield return null;
            var task = ExportSceneGameObjects(true, true);
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
            Assert.Throws<InvalidOperationException>(delegate()
            {
                export.AddScene(new []{childA});
            });
            path = Path.Combine(Application.persistentDataPath, "SavedTwice2.gltf");
            AssertThrowsAsync<InvalidOperationException>(async () => await export.SaveToFileAndDispose(path));
        }

        async Task ExportSceneGameObjects(bool binary, bool toStream = false) {
            var scene = SceneManager.GetActiveScene();

            var rootObjects = scene.GetRootGameObjects();

            Assert.AreEqual(38,rootObjects.Length);
            foreach (var gameObject in rootObjects) {
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
                    var targetJsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>($"Packages/com.atteneder.gltf-test-framework/Tests/Resources/Target/{fileName}.txt");
                    Assert.NotNull(targetJsonAsset, $"Target glTF JSON for {fileName} was not found");
                    var actualJson = GltfJsonSetGenerator(File.ReadAllText(path));
                    var targetJson = GltfJsonSetGenerator(targetJsonAsset.text);
                    Assert.AreEqual(targetJson,actualJson);
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
        }

        async Task ExportSceneAll(bool binary, bool toStream = false) {
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

        void AssertLogger(CollectingLogger logger) {
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
        public static TActual AssertThrowsAsync<TActual>(AsyncTestDelegate code, string message = "", params object[] args) where TActual : Exception
        {
            return Assert.Throws<TActual>(() =>
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
     
        public delegate Task AsyncTestDelegate();


#if GLTF_VALIDATOR && UNITY_EDITOR
        void ValidateGltf(string path, params MessageCode[] expectedMessages) {
            var report = Validator.Validate(path);
            Assert.NotNull(report, $"Report null for {path}");
            // report.Log();
            if (report.issues != null) {
                foreach (var message in report.issues.messages) {
                    if (((IList)expectedMessages).Contains(message.codeEnum)) {
                        continue;
                    }
                    Assert.Less(1, message.severity, $"Error {message} (path {Path.GetFileName(path)})");
                    Assert.Less(2, message.severity, $"Warning {message} (path {Path.GetFileName(path)})");
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
