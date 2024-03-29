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

#if UNITY_PERFORMANCE_TESTS

using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using GLTFast;

namespace GLTFTest.Performance {

    using Sample;
    
    public class SampleModelsPerformanceTest {
        const int k_Repetitions = 10;

        [UnityTest]
        [UseGltfSampleSetTestCase(ImportSampleModelsTest.glTFSampleSetJsonPath, "-Uninterrupted")]
        [Performance]
        public IEnumerator UninterruptedLoading(SampleSetItem testCase) {
            yield return UninterruptedLoadingTemplate(testCase);
        }
        
        // [UnityTest]
        // [UseGltfSampleSetTestCase(localSampleSetJsonPath)]
        // [Performance]
        // public IEnumerator UninterruptedLoadingLocal(SampleSetItem testCase) {
        //     yield return UninterruptedLoadingTemplate(testCase);
        // }

        static IEnumerator UninterruptedLoadingTemplate(SampleSetItem testCase) {
            Debug.Log($"Testing {testCase.path}");
            var go = new GameObject();
            var deferAgent = new UninterruptedDeferAgent();
            var loadTime = new SampleGroup("LoadTime");
            var instantiationSettings = new InstantiationSettings {
                // Make sure go is never re-used as scene root
                SceneObjectCreation = SceneObjectCreation.Always
            };
            // First time without measuring
            var task = ImportSampleModelsTest.LoadGltfSampleSetItem(testCase, go, deferAgent, instantiationSettings);
            yield return Utils.WaitForTask(task);
            using (Measure.Frames().Scope()) {
                for (int i = 0; i < k_Repetitions; i++) {
                    task = LoadGltfSampleSetItem(testCase, go, deferAgent, loadTime, instantiationSettings);
                    yield return Utils.WaitForTask(task);
                }
            }
            
            Object.Destroy(go);
        }

        [UnityTest]
        [UseGltfSampleSetTestCase(ImportSampleModelsTest.glTFSampleSetJsonPath)]
        [Performance]
        public IEnumerator SmoothLoading(SampleSetItem testCase)
        {
            // Debug.Log($"Testing {testCase.path}");
            var go = new GameObject();
            var deferAgent = go.AddComponent<TimeBudgetPerFrameDeferAgent>();
            SampleGroup loadTime = new SampleGroup("LoadTime");
            var instantiationSettings = new InstantiationSettings {
                // Make sure go is never re-used as scene root
                SceneObjectCreation = SceneObjectCreation.Always
            };
            // First time without measuring
            var task = ImportSampleModelsTest.LoadGltfSampleSetItem(testCase, go, deferAgent, instantiationSettings);
            yield return Utils.WaitForTask(task);
            using (Measure.Frames().Scope()) {
                for (int i = 0; i < k_Repetitions; i++) {
                    task = LoadGltfSampleSetItem(testCase, go, deferAgent, loadTime, instantiationSettings);
                    yield return Utils.WaitForTask(task);
                    // Wait one more frame. Usually some more action happens in this one.
                    yield return null;
                }
            }
            Object.Destroy(go);
        }

        static async Task LoadGltfSampleSetItem(
            SampleSetItem testCase,
            GameObject go,
            IDeferAgent deferAgent,
            SampleGroup loadTime,
            InstantiationSettings instantiationSettings = null
            )
        {
            var stopWatch = go.AddComponent<StopWatch>();
            stopWatch.StartTime();

            await ImportSampleModelsTest.LoadGltfSampleSetItem(testCase, go, deferAgent, instantiationSettings);

            stopWatch.StopTime();
            Measure.Custom(loadTime, stopWatch.lastDuration);
        }
    }
}

#endif // UNITY_PERFORMANCE_TESTS
