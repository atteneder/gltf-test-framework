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
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using GLTFast;

namespace GLTFTest {

    using Sample;
    
    class SampleModelsTest {
        
        public const string glTFSampleSetAssetPath = "Packages/com.atteneder.gltf-tests/Runtime/SampleSets/glTF-Sample-Models.asset";
        public const string glTFSampleSetJsonPath = "glTF-Sample-Models.json";
        public const string glTFSampleSetBinaryJsonPath = "glTF-Sample-Models-glb.json";

        // const string localSampleSetJsonPath = "local.json";
        
        [Test]
        public void CheckFiles() {
            Utils.CheckFiles(glTFSampleSetAssetPath, 212);
        }

        [UnityTest]
        [UseGltfSampleSetTestCase(glTFSampleSetJsonPath,"-Uninterrupted")]
        public IEnumerator UninterruptedLoading(SampleSetItem testCase) {
            yield return UninterruptedLoadingTemplate(testCase);
        }
        
        // [UnityTest]
        // [UseGltfSampleSetTestCase(localSampleSetJsonPath)]
        // [Performance]
        // public IEnumerator UninterruptedLoadingLocal(SampleSetItem testCase) {
        //     yield return UninterruptedLoadingTemplate(testCase);
        // }

        internal static IEnumerator UninterruptedLoadingTemplate(SampleSetItem testCase) {
            Debug.Log($"Testing {testCase.path}");
            var go = new GameObject();
            var deferAgent = new UninterruptedDeferAgent();
            var task = LoadGltfSampleSetItem(testCase, go, deferAgent);
            yield return Utils.WaitForTask(task);
            Object.Destroy(go);
        }

        [UnityTest]
        [UseGltfSampleSetTestCase(glTFSampleSetJsonPath)]
        public IEnumerator SmoothLoading(SampleSetItem testCase)
        {
            Debug.Log($"Testing {testCase.path}");
            var go = new GameObject();
            var deferAgent = go.AddComponent<TimeBudgetPerFrameDeferAgent>();
            var task = LoadGltfSampleSetItem(testCase, go, deferAgent);
            yield return Utils.WaitForTask(task);
            Object.Destroy(go);
        }

        public static async Task LoadGltfSampleSetItem(SampleSetItem testCase, GameObject go, IDeferAgent deferAgent, SampleGroup loadTime = null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var path = testCase.path;
#else
            var path = $"file://{testCase.path}";
#endif

            // Debug.LogFormat("Testing {0}", path);
            
            var gltfAsset = go.AddComponent<GltfAsset>();
            var stopWatch = go.AddComponent<StopWatch>();
            stopWatch.StartTime();

            gltfAsset.loadOnStartup = false;
            var success = await gltfAsset.Load(path,null,deferAgent);
            Assert.IsTrue(success);
            
            stopWatch.StopTime();

            if (loadTime != null) {
                Measure.Custom(loadTime, stopWatch.lastDuration);
            }
        }
    }
}
