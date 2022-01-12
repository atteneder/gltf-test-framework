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
using GLTFast;
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEngine;

namespace GLTFTest.Performance {

    static class MatrixExtensionTest {
        
        const int measureCount = 10;
        const int iterationsPerMeasurement = 100_000;
        
        [Test, Performance]
        public static void MatrixDecomposeTestLegacy() {
            // Corner case matrix (90°/0°/45° rotation with -1/-1/-1 scale)
            var m = new Matrix4x4(
                new Vector4(
                    -0.7071067811865474f,
                    0f,
                    -0.7071067811865477f,
                    0f
                ),
                new Vector4(
                    0.7071067811865477f,
                    0f,
                    -0.7071067811865474f,
                    0f
                ),
                new Vector4(
                    0f,
                    1f,
                    0f,
                    0f
                ),
                new Vector4(
                    0f,
                    0f,
                    0f,
                    1f
                )
            );
            
            Measure.Method(() => {
                    m.Decompose(out var t, out var r, out var s);
                })
                .WarmupCount(1)
                .MeasurementCount(measureCount)
                .IterationsPerMeasurement(iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public static void MatrixDecomposeTest() {
            // Corner case matrix (90°/0°/45° rotation with -1/-1/-1 scale)
            var m2 = new float4x4(
                -0.7071067811865474f,0.7071067811865477f,0,0,
                0,0,1,0,
                -0.7071067811865477f,-0.7071067811865474f,0,0,
                0,0,0,1
            );

            Measure.Method(() => {
                    m2.Decompose(out var t3, out var r3, out var s3);
                })
                .WarmupCount(1)
                .MeasurementCount(measureCount)
                .IterationsPerMeasurement(iterationsPerMeasurement)
                .Run();
        }
    }
}
