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

#if !(UNITY_ANDROID || UNITY_WEBGL) || UNITY_EDITOR
#define LOCAL_LOADING
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[assembly:InternalsVisibleTo("glTFTests")]

namespace GLTFTest.Sample {

    [CreateAssetMenu(fileName = "glTF-SampleSet", menuName = "ScriptableObjects/glTFast SampleSet", order = 1)]
    public class SampleSet : ScriptableObject {

        /// <summary>
        /// Local file path
        /// </summary>
        public string baseLocalPath = "";
        
        /// <summary>
        /// Path relative to "assets", a folder at root level of the glTF Test repository.
        /// Can be overriden.
        /// </summary>
        public string assetRelativePath;
        
        /// <summary>
        /// Path relative to "Assets/StreamingAssets" folder
        /// </summary>
        public string streamingAssetsPath = "glTF-Sample-Models";
        
        /// <summary>
        /// Base URI for loading via HTTP
        /// </summary>
        public string baseUrlWeb = "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/";
        
        /// <summary>
        /// Base URI for loading via HTTP from local server
        /// </summary>
        public string baseUrlLocal = "http://localhost:8080/glTF-Sample-Models/2.0/";

        [SerializeField]
        private SampleSetItemEntry[] items;

        public int itemCount => items.Length;

        /// <summary>
        /// Resolves to the absolute path in this order
        /// 1. StreamingAssets
        /// 2. Assets
        /// 3. Absolute Path
        /// </summary>
        public string localFilePath {
            get {
                string path;
                
                if (!string.IsNullOrEmpty(streamingAssetsPath)) {
                    path = Path.Combine(Application.streamingAssetsPath, streamingAssetsPath);
                    if (Directory.Exists(path)) {
                        return path;
                    }
                }

                return GetSourcePath();
            }
        }

        /// <summary>
        /// Gives the absolute path to "assets", null otherwise
        /// </summary>
        string assetAbsolutePath {
            get {
                if(!string.IsNullOrEmpty(assetRelativePath)) {
                    var assetsPath = GetAssetsPath();
                    if (assetsPath == null) {
                        return null;
                    }
                    var path = Path.Combine(assetsPath, assetRelativePath);
                    if (Directory.Exists(path)) {
                        return path;
                    }
                }

                return null;
            }
        }
        
        string remoteUri {
            get {
                string uriPrefix = string.IsNullOrEmpty(baseUrlWeb) ? "<baseUrlWeb not set!>" : baseUrlWeb;
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(baseUrlLocal)) {
                    uriPrefix = baseUrlLocal;
                }
#endif
                return uriPrefix;
            }
        }

        /// <summary>
        /// Deserializes a sample set from a JSON file stored in the
        /// StreamingAssets path.
        /// </summary>
        /// <param name="path">JSON file path relative to
        /// the StreamingAssets folder</param>
        /// <returns>Deserialized sample set</returns>
        public static SampleSet FromStreamingAssets(string path) {
            var json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, path));
            var sampleSet = CreateInstance<SampleSet>();
            JsonUtility.FromJsonOverwrite(json,sampleSet);
            return sampleSet;
        }
        
        /// <summary>
        /// Source directory
        /// </summary>
        /// <returns>Absolute path to source directory, either within "assets" or absolute baseLocalPath</returns>
        string GetSourcePath() {
            var path = assetAbsolutePath;
            if(!string.IsNullOrEmpty(path) && Directory.Exists(path)) {
                return path;
            }

            return baseLocalPath;
        }

        public IEnumerable<SampleSetItem> GetItems() {
            foreach (var x in items) {
                if (!x.active) continue;
                yield return new SampleSetItem(
                    string.Format("{0}-{1:X}", x.item.name, x.item.path.GetHashCode()),
                    x.item.path
                );
            }
        }

        public IEnumerable<SampleSetItem> GetItemsPrefixed(bool local = true) {
            var prefix = local ? localFilePath : remoteUri;
            foreach (var entry in items) {
                if(!entry.active) continue;
                if (!string.IsNullOrEmpty(prefix)) {
                    var p = string.Format(
                        "{0}/{1}"
                        , prefix
                        , entry.item.path
                    );
                    yield return new SampleSetItem(entry.item.name, p);
                }
            }
        }

        public IEnumerable<SampleSetItem> GetTestItems(bool local = true) {
            var prefix = local ? localFilePath : remoteUri;
            foreach (var entry in items) {
                if(!entry.active) continue;
                if (!string.IsNullOrEmpty(prefix)) {
                    var p = string.Format(
                        "{0}/{1}"
                        , prefix
                        , entry.item.path
                    );
                    yield return new SampleSetItem(entry.item.name, p);
                }
            }
        }

        public bool TryGetTestItem(string itemName, out SampleSetItem item, bool local = true) {
            foreach (var x in items) {
                if (!x.active) continue;
                if (x.item.name == itemName) {
                    var prefix = local ? localFilePath : remoteUri;
                    if (!string.IsNullOrEmpty(prefix)) {
                        item = new SampleSetItem {
                            name = x.item.name,
                            path = $"{prefix}/{x.item.path}" 
                        };
                        return true;
                    }
                    break;
                }
            }

            item = new SampleSetItem();
            return false;
        }

        public void LoadItemsFromPath(string searchPattern) {
            var dir = new DirectoryInfo(localFilePath);
            var dirLength = dir.FullName.Length + 1;

            var newItems = new List<SampleSetItemEntry>();

            foreach (var file in dir.GetFiles(searchPattern, SearchOption.AllDirectories)) {
                var ext = file.Extension;
                if (ext != ".gltf" && ext != ".glb") continue;
                var i = new SampleSetItemEntry();
                i.active = true;
                i.item.name = file.Name;
                i.item.path = file.FullName.Substring(dirLength);
                newItems.Add(i);
            }

            items = newItems.ToArray();
        }

        public void SetAllActive(bool active = true) {
            for (int i = 0; i < items.Length; i++) {
                items[i].active = active;
            }
        }

        /// <summary>
        /// Get the project's path
        /// Can be overriden by the GLTF_TEST_PROJECT_DIR environment variable
        /// </summary>
        /// <returns>Project file path</returns>
        static string GetProjectPath() {
            var projectPath = Environment.GetEnvironmentVariable("GLTF_TEST_PROJECT_DIR");
            if (!string.IsNullOrEmpty(projectPath) && Directory.Exists(projectPath)) {
                Debug.LogWarning($"GLTF_TEST_PROJECT_DIR {projectPath}");
                if (!string.IsNullOrEmpty(projectPath) && Directory.Exists(projectPath)) {
                    return projectPath;
                }
            }
            var parent =  new DirectoryInfo(Application.dataPath); // Assets
            parent = parent.Parent; // Project dir
            return parent?.FullName;
        }
        
        /// <summary>
        /// Get assets path from the root level of the glTF Test repository
        /// Can be overriden via GLTF_TEST_ASSET_DIR environment variable
        /// </summary>
        /// <returns>Path to glTFastTest project specific assets folder</returns>
        internal static string GetAssetsPath() {
            var assetDir = Environment.GetEnvironmentVariable("GLTF_TEST_ASSET_DIR");
            if (!string.IsNullOrEmpty(assetDir) && Directory.Exists(assetDir)) {
                return assetDir;
            }
            var dir =  new DirectoryInfo(Application.dataPath); // Assets
            dir = dir.Parent; // Project dir
            dir = dir?.Parent; // projects dir
            dir = dir?.Parent; // root dir
            if (dir != null) {
                var path = Path.Combine(dir.FullName,"assets");
                if (Directory.Exists(path)) {
                    return path;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        public void CopyToStreamingAssets(bool force = false) {
            var srcPath = GetSourcePath();
            if (string.IsNullOrEmpty(srcPath) || !Directory.Exists(srcPath)) {
                Debug.LogError($"Invalid source path: \"{srcPath}\"");
                return;
            }

            if (string.IsNullOrEmpty(streamingAssetsPath)) {
                Debug.LogError($"Invalid streamingAssetsPath: \"{streamingAssetsPath}\"");
                return;
            }
            var dstPath = Path.Combine(Application.streamingAssetsPath, streamingAssetsPath);

            if (Directory.Exists(dstPath)) {
                if (force) {
                    Directory.Delete(dstPath);
                }
                else {
                    return;
                }
            }
            else {
                var parent = Directory.GetParent(dstPath).FullName;
                if (!Directory.Exists(parent)) {
                    Directory.CreateDirectory(parent);
                }
            }
            
            FileUtil.CopyFileOrDirectory(srcPath,dstPath);
            
            AssetDatabase.Refresh();
        }
        
        public void CreateJSON() {
            var jsonPathAbsolute = Path.Combine( Application.streamingAssetsPath, $"{name}.json");
            Debug.Log(jsonPathAbsolute);
            var json = JsonUtility.ToJson(this);
            File.WriteAllText(jsonPathAbsolute,json);
        }
        
        public void CreateListFile() {
            var newName = name.Replace("-Sample-Models-", "_");
            newName = newName.Replace("-Sample-Models", "");
            var jsonPathAbsolute = Path.Combine( Application.streamingAssetsPath, $"{newName}.txt");
            Debug.Log(jsonPathAbsolute);
            using( var fs = new StreamWriter(jsonPathAbsolute) )
            {
                fs.Write($"# All files from set {newName}\n\n");
                foreach (var setItem in GetItems()) {
                    fs.WriteLine(setItem.path);
                }
            }
        }
#endif
    }
}
