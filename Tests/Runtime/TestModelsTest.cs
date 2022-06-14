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
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GLTFTest {

    using Sample;
    
    class TestModelsTest {
        
        const string k_AssetPath = "Packages/com.atteneder.gltf-tests/Runtime/SampleSets/glTF-test-models.asset";
        const string k_JsonPath = "glTF-test-models.json";

        [Test]
        public void CheckFiles()
        {
#if UNITY_EDITOR
            var sampleSet = AssetDatabase.LoadAssetAtPath<SampleSet>(k_AssetPath);
            Assert.IsNotNull(sampleSet,"SampleSet not found");
            Assert.AreEqual(1, sampleSet.itemCount);

            foreach (var item in sampleSet.GetItemsPrefixed()) {
                SampleModelsTest.CheckFileExists(item.path);
            }
#else
            Debug.Log("Editor only test");
#endif
        }

        [UnityTest]
        [UseGltfSampleSetTestCase(k_JsonPath)]
        public IEnumerator TestModels(SampleSetItem testCase) {
            yield return SampleModelsTest.UninterruptedLoadingTemplate(testCase);
        }
    }
}
