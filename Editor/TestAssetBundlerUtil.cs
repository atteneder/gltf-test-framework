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

using System.Collections.Generic;
using GLTFTest.Sample;
using UnityEditor;
using UnityEngine;

public class TestAssetBundlerUtil {

    public static void SyncAssets(IEnumerable<string> samplePaths, bool forceStreamingAssets = false) {

        foreach (var samplePath in samplePaths) {
            var sampleSet = AssetDatabase.LoadAssetAtPath<SampleSet>(samplePath);
            if (!sampleSet) {
                Debug.LogWarning($"Expected SampleSet at {samplePath} doesn't exist.");
                continue;
            }

            if (forceStreamingAssets) {
                sampleSet.CopyToStreamingAssets();
            }
            sampleSet.CreateJSON();
        }
        AssetDatabase.Refresh();
    }
}
