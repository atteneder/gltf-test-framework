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

using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace GLTFTest {
    
    public static class Utils {
        
        public static IEnumerator WaitForTask(Task task) {
            while(!task.IsCompleted) {
                if (task.Exception != null)
                    throw task.Exception;
                yield return null;
            }
            if (task.Exception != null)
                throw task.Exception;
        }
        
        public static void AssertNearOrEqual(float4 reference, float4 value, float epsilon = float.Epsilon) {
            var delta = math.abs(reference - value);
            var maxDelta = math.max(delta.x, math.max(delta.y, math.max(delta.z,delta.w)));
            if (maxDelta > epsilon) {
                throw new AssertionException($"float4 not equal. expected {reference} got {value} (delta {maxDelta})");
            }
        }
        
        public static void AssertNearOrEqual(float3 reference, float3 value, float epsilon = float.Epsilon) {
            var delta = math.abs(reference - value);
            var maxDelta = math.max(delta.x, math.max(delta.y, delta.z));
            if (maxDelta > epsilon) {
                throw new AssertionException($"float3 not equal. expected {reference} got {value} (delta {maxDelta})");
            }
        }
        
        public static void AssertNearOrEqual(float2 reference, float2 value, float epsilon = float.Epsilon) {
            var delta = math.abs(reference - value);
            var maxDelta = math.max(delta.x, delta.y);
            if (maxDelta > epsilon) {
                throw new AssertionException($"float2 not equal. expected {reference} got {value} (delta {maxDelta})");
            }
        }
        
        public static void AssertNearOrEqual(float reference, float value, float epsilon = float.Epsilon) {
            var delta = math.abs(reference - value);
            if (delta > epsilon) {
                throw new AssertionException($"float not equal. expected {reference} got {value} (delta {delta})");
            }
        }
        
        public static void AssertNearOrEqual(Color reference, Color value, float epsilon = float.Epsilon) {
            AssertNearOrEqual(
                new float4(reference.r,reference.g,reference.b,reference.a),
                new float4(value.r,value.g,value.b,value.a),
                epsilon
                );
        }

        public static void AssertNearOrEqual(Color reference, float4 value, float epsilon = float.Epsilon) {
            AssertNearOrEqual(
                new float4(reference.r,reference.g,reference.b,reference.a),
                value,
                epsilon
            );
        }

        public static void AssertNearOrEqual(uint4 reference, uint4 value) {
            var b = reference != value;
            if (b.x || b.y || b.z || b.w) {
                throw new AssertionException($"float4 not equal. expected {reference} got {value}");
            }
        }
    }
}
