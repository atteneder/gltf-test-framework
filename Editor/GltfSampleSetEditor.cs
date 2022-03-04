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
                _sampleSet.CreateRenderTestScenes();
            }
            
            if (GUILayout.Button("Create single test scene")) {
                _sampleSet.CreateRenderSingleTestScene();
            }
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Copy to StreamingAssets")) {
                _sampleSet.CopyToStreamingAssets();
            }
            
            base.OnInspectorGUI();
            
            if (GUI.changed) {
                EditorUtility.SetDirty(_sampleSet);
            }
        }
        
        static void CreateJSON(SampleSet sampleSet, Object target) {
            sampleSet.CreateJSON();
        }

        static void CreateListFile(SampleSet sampleSet, Object target) {
            sampleSet.CreateListFile();
        }
    }
}
