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
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using GLTFast;
using GLTFast.Logging;

namespace GLTFTest {

    using Sample;
    
    /// <summary>
    /// Tests all of <see cref="GltfImport"/>'s load methods. 
    /// </summary>
    class ImportTest {
        
        const string k_GltfAssetPath = "Packages/com.atteneder.gltf-tests/Runtime/SampleSets/ImportTest.asset";
        const string k_GltfJsonPath = "ImportTest.json";

        [Test]
        public void CheckFiles()
        {
            Utils.CheckFiles(k_GltfAssetPath, 2);
        }
        
        [UnityTest]
        [UseGltfSampleSetTestCase(k_GltfJsonPath,"-LoadUri")]
        public IEnumerator LoadUri(SampleSetItem testCase)
        {
            var go = new GameObject();
            var deferAgent = new UninterruptedDeferAgent();
            var logger = new ConsoleLogger();
            using (var gltf = new GltfImport(deferAgent: deferAgent, logger: logger)) { 
                var task = gltf.Load($"file://{testCase.path}");
                yield return Utils.WaitForTask(task);
                var success = task.Result;
                Assert.IsTrue(success);
                var instantiator = new GameObjectInstantiator(gltf,go.transform,logger);
                task = gltf.InstantiateMainScene(instantiator);
                yield return Utils.WaitForTask(task);
                success = task.Result;
                Assert.IsTrue(success);
                Object.Destroy(go);
            }
        }
        
        [UnityTest]
        [UseGltfSampleSetTestCase(k_GltfJsonPath,"-Load")]
        public IEnumerator Load(SampleSetItem testCase)
        {
            Debug.Log($"Testing {testCase.path}");
            var data = File.ReadAllBytes(testCase.path);
            var go = new GameObject();
            var deferAgent = new UninterruptedDeferAgent();
            var logger = new ConsoleLogger();
            using (var gltf = new GltfImport(deferAgent: deferAgent, logger: logger)) { 
                var task = gltf.Load(data, new Uri(testCase.path));
                yield return Utils.WaitForTask(task);
                var success = task.Result;
                Assert.IsTrue(success);
                var instantiator = new GameObjectInstantiator(gltf,go.transform,logger);
                task = gltf.InstantiateMainScene(instantiator);
                yield return Utils.WaitForTask(task);
                success = task.Result;
                Assert.IsTrue(success);
                Object.Destroy(go);
            }
        }
        
        [UnityTest]
        [UseGltfSampleSetTestCase(k_GltfJsonPath,"-LoadFile")]
        public IEnumerator LoadFile(SampleSetItem testCase)
        {
            Debug.Log($"Testing {testCase.path}");
            var go = new GameObject();
            var deferAgent = new UninterruptedDeferAgent();
            var logger = new ConsoleLogger();
            using (var gltf = new GltfImport(deferAgent: deferAgent, logger: logger)) { 
                var task = gltf.LoadFile(testCase.path, new Uri(testCase.path));
                yield return Utils.WaitForTask(task);
                var success = task.Result;
                Assert.IsTrue(success);
                var instantiator = new GameObjectInstantiator(gltf,go.transform,logger);
                task = gltf.InstantiateMainScene(instantiator);
                yield return Utils.WaitForTask(task);
                success = task.Result;
                Assert.IsTrue(success);
                Object.Destroy(go);
            }
        }
        
        [UnityTest]
        [UseGltfSampleSetTestCase(k_GltfJsonPath,"-LoadGltfBinary")]
        public IEnumerator LoadGltfBinary(SampleSetItem testCase)
        {
            if (!testCase.path.EndsWith(".glb")) {
                Assert.Ignore( "Wrong glTF type. Skipping" );
            }
            Debug.Log($"Testing {testCase.path}");
            var data = File.ReadAllBytes(testCase.path);
            var go = new GameObject();
            var deferAgent = new UninterruptedDeferAgent();
            var logger = new ConsoleLogger();
            using (var gltf = new GltfImport(deferAgent: deferAgent, logger: logger)) { 
                var task = gltf.LoadGltfBinary(data, new Uri(testCase.path));
                yield return Utils.WaitForTask(task);
                var success = task.Result;
                Assert.IsTrue(success);
                var instantiator = new GameObjectInstantiator(gltf,go.transform,logger);
                task = gltf.InstantiateMainScene(instantiator);
                yield return Utils.WaitForTask(task);
                success = task.Result;
                Assert.IsTrue(success);
                Object.Destroy(go);
            }
        }
        
        [UnityTest]
        [UseGltfSampleSetTestCase(k_GltfJsonPath,"-LoadGltfJson")]
        public IEnumerator LoadGltfJson(SampleSetItem testCase)
        {
            if (!testCase.path.EndsWith(".gltf")) {
                Assert.Ignore( "Wrong glTF type. Skipping" );
            }
            Debug.Log($"Testing {testCase.path}");
            var json = File.ReadAllText(testCase.path);
            var go = new GameObject();
            var deferAgent = new UninterruptedDeferAgent();
            var logger = new ConsoleLogger();
            using (var gltf = new GltfImport(deferAgent: deferAgent, logger: logger)) { 
                var task = gltf.LoadGltfJson(json, new Uri(testCase.path));
                yield return Utils.WaitForTask(task);
                var success = task.Result;
                Assert.IsTrue(success);
                var instantiator = new GameObjectInstantiator(gltf,go.transform,logger);
                task = gltf.InstantiateMainScene(instantiator);
                yield return Utils.WaitForTask(task);
                success = task.Result;
                Assert.IsTrue(success);
                Object.Destroy(go);
            }
        }
    }
}
