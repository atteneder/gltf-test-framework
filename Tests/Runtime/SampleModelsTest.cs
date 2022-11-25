﻿// Copyright 2020-2022 Andreas Atteneder
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
        
        static UninterruptedDeferAgent s_UninterruptedDeferAgent;
        static TimeBudgetPerFrameDeferAgent s_TimeBudgetPerFrameDeferAgent;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            s_UninterruptedDeferAgent = new UninterruptedDeferAgent();
            var go = new GameObject("TimeBudgetPerFrameDeferAgent");
            s_TimeBudgetPerFrameDeferAgent = go.AddComponent<TimeBudgetPerFrameDeferAgent>();
        }

        [OneTimeTearDown()]
        public void OneTimeTearDown()
        {
            Object.Destroy(s_TimeBudgetPerFrameDeferAgent.gameObject);
        }
        
        [Test]
        public void CheckFiles() {
            Utils.CheckFiles(glTFSampleSetAssetPath, 211);
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
            // Debug.Log($"Testing {testCase.path}");
            var go = new GameObject();
            var task = LoadGltfSampleSetItem(testCase, go, s_UninterruptedDeferAgent);
            yield return Utils.WaitForTask(task);
            Object.Destroy(go);
        }

        [UnityTest]
        [UseGltfSampleSetTestCase(glTFSampleSetJsonPath)]
        public IEnumerator SmoothLoading(SampleSetItem testCase)
        {
            // Debug.Log($"Testing {testCase.path}");
            var go = new GameObject();
            var task = LoadGltfSampleSetItem(testCase, go, s_TimeBudgetPerFrameDeferAgent);
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

            StopWatch stopWatch = null;
            if (loadTime != null) {
                stopWatch = go.AddComponent<StopWatch>();
                stopWatch.StartTime();
            }

            gltfAsset.loadOnStartup = false;
            var success = await gltfAsset.Load(path,null,deferAgent);
            Assert.IsTrue(success);
            
            if (loadTime != null) {
                stopWatch.StopTime();
                Measure.Custom(loadTime, stopWatch.lastDuration);
            }
        }
    }
}
