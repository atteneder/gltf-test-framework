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
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using UnityEngine;

namespace GLTFTest {

    public class SceneRootObjectTestCaseAttribute : UnityEngine.TestTools.UnityTestAttribute, ITestBuilder {

        string[] m_ObjectNames;
        
        NUnitTestCaseBuilder m_Builder = new NUnitTestCaseBuilder();

        public SceneRootObjectTestCaseAttribute(string namesFile) {
            var path = Path.Combine(Application.streamingAssetsPath, namesFile);
            m_ObjectNames = File.ReadAllLines(path);
        }

        IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test suite) {
            var results = new List<TestMethod>();

            try {
                for (var i = 0; i < m_ObjectNames.Length; i++) {
                    var objectName = m_ObjectNames[i];
                    var data = new TestCaseData(new object[] { i, objectName });

                    data.SetName(objectName);
                    data.ExpectedResult = new UnityEngine.Object();
                    data.HasExpectedResult = true;

                    var test = this.m_Builder.BuildTestMethod(method, suite, data);
                    if (test.parms != null)
                        test.parms.HasExpectedResult = false;

                    test.Name = objectName;

                    results.Add(test);
                }
            }
            catch (Exception ex) {
                Console.WriteLine("Failed to generate glTF testcases!");
                Debug.LogException(ex);
                throw;
            }

            Console.WriteLine("Generated {0} glTF test cases.", results.Count);
            return results;
        }
    }
}
