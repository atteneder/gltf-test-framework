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
using System.Threading;
using System.Threading.Tasks;
using GLTFast;
using GLTFTest.Sample;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace GLTFTest.Import {

    class CancellationTest {
        
        [UnityTest]
        // [UseGltfSampleSetTestCase(TestModelsTest.k_AssetPath)]
        public IEnumerator CancelImmediately() {

            var sampleSet = SampleSet.FromStreamingAssets(SampleModelsTest.glTFSampleSetJsonPath);
            const string itemName = "Avocado.glb";
            if (sampleSet.TryGetTestItem( itemName, out var item)) {
                var tokenSrc = new CancellationTokenSource();
#if UNITY_ANDROID && !UNITY_EDITOR
                var path = item.path;
#else
                var path = $"file://{item.path}";
#endif
                var task = LoadGltf(item.path, tokenSrc.Token);
                tokenSrc.Cancel();
                yield return Utils.WaitForTask(task);
                var gltf = task.Result;
                Assert.IsFalse(gltf.LoadingError, "Cancellation should not cause an error");
                Assert.IsFalse(gltf.LoadingDone, "Loading finished despite being cancelled");
            }
            else {
                throw new AssertionException($"Sample set item {itemName} was not found");
            }
        }

        static async Task<GltfImport> LoadGltf(string path, CancellationToken token) {
            var gltf = new GltfImport();
            await gltf.LoadFile(path, new Uri(path), cancellationToken: token);
            return gltf;
        }
    }
}
