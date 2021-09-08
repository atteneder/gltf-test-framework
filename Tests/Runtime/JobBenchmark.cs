// Copyright 2020-2021 Andreas Atteneder
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
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEngine;

namespace GLTFTest.Jobs {

    static class Constants {
        public const int measureCount = 10;
        public const int iterationsPerMeasurement = 5;

        public const float epsilonUInt8 = .004f;
        public const float epsilonInt8 = .008f;
        public const float epsilonUInt16 = .000016f;
        public const float epsilonInt16 = .000031f;
    }

    static class Utils {
        
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
        
        public static void AssertNearOrEqual(uint4 reference, uint4 value) {
            var b = reference != value;
            if (b.x || b.y || b.z || b.w) {
                throw new AssertionException($"float4 not equal. expected {reference} got {value}");
            }
        }
    }
    
    [TestFixture]
    public class Vector3Jobs {

        const int k_Length = 10_000_000;
        float3 m_NormalizedReference = new float3(-.5f, .5f, 0.707107f);
        float3 m_Reference = new float3(-1, 13, 42);
        
        NativeArray<float3> m_Input;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<short> m_InputInt16;
        NativeArray<byte> m_InputUInt8;
        NativeArray<sbyte> m_InputInt8;
        NativeArray<float3> m_Output;

        [OneTimeSetUp]
        public void SetUpTest() {
            m_Input = new NativeArray<float3>(k_Length, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_Length*3, Allocator.Persistent);
            m_InputInt16 = new NativeArray<short>(k_Length*3, Allocator.Persistent);
            m_InputUInt8 = new NativeArray<byte>(k_Length*3, Allocator.Persistent);
            m_InputInt8 = new NativeArray<sbyte>(k_Length*3, Allocator.Persistent);
            m_Output = new NativeArray<float3>(k_Length, Allocator.Persistent);
            
            var i = 1;
            {
                m_Input[i] = new float3(.5f, .5f, 0.707107f);

                m_InputUInt8[i*3] = byte.MaxValue/2;
                m_InputUInt8[i*3+1] = byte.MaxValue/2;
                m_InputUInt8[i*3+2] = 180;
                
                m_InputInt8[i*3] = SByte.MaxValue/2;
                m_InputInt8[i*3+1] = SByte.MaxValue/2;
                m_InputInt8[i*3+2] = 90;
                
                m_InputUInt16[i*3] = ushort.MaxValue/2;
                m_InputUInt16[i*3+1] = ushort.MaxValue/2;
                m_InputUInt16[i*3+2] = (ushort) (0.707107f * ushort.MaxValue);
                
                m_InputInt16[i*3] = short.MaxValue/2;
                m_InputInt16[i*3+1] = short.MaxValue/2;
                m_InputInt16[i*3+2] = (short) (0.707107f * short.MaxValue);
            }

            i = 2;
            {
                m_Input[i] = new float3(1, 13, 42);;

                m_InputUInt8[i*3] = 1;
                m_InputUInt8[i*3+1] = 13;
                m_InputUInt8[i*3+2] = 42;
                
                m_InputInt8[i*3] = 1;
                m_InputInt8[i*3+1] = 13;
                m_InputInt8[i*3+2] = 42;
                
                m_InputUInt16[i*3] = 1;
                m_InputUInt16[i*3+1] = 13;
                m_InputUInt16[i*3+2] = 42;
                
                m_InputInt16[i*3] = 1;
                m_InputInt16[i*3+1] = 13;
                m_InputInt16[i*3+2] = 42;
            }
        }
        
        [OneTimeTearDown]
        public void Cleanup() {
            m_Input.Dispose();
            m_InputUInt16.Dispose();
            m_InputInt16.Dispose();
            m_InputUInt8.Dispose();
            m_InputInt8.Dispose();
            m_Output.Dispose();
        }

        void CheckNormalizedResult(float epsilon = float.Epsilon) {
            const int i = 1;
            Utils.AssertNearOrEqual(m_NormalizedReference, m_Output[i], epsilon);
        }
        
        void CheckResult(float epsilon = float.Epsilon) {
            const int i = 2;
            Utils.AssertNearOrEqual(m_Reference, m_Output[i], epsilon);
        }
        
        [Test, Performance]
        public unsafe void ConvertVector3FloatToFloatInterleavedJob() {
            var job = new GLTFast.Jobs.ConvertVector3FloatToFloatInterleavedJob {
                inputByteStride = 12,
                input = (byte*)m_Input.GetUnsafeReadOnlyPtr(),
                outputByteStride = 12,
                result = (Vector3*)m_Output.GetUnsafePtr()
            };
            Measure.Method(() => job.Run(m_Input.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();

            CheckNormalizedResult();
            CheckResult();
        }

        [Test, Performance]
        public unsafe void ConvertVector3FloatToFloatJob() {
            var job = new GLTFast.Jobs.ConvertVector3FloatToFloatJob {
                input = (float*)m_Input.GetUnsafeReadOnlyPtr(),
                result = (float*)m_Output.GetUnsafePtr()
            };
            Measure.Method(() => job.Run(m_Input.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckNormalizedResult();
            CheckResult();
        }
        
        [Test, Performance]
        public unsafe void ConvertPositionsUInt16ToFloatInterleavedJob() {
            var job = new GLTFast.Jobs.ConvertPositionsUInt16ToFloatInterleavedJob {
                input = (byte*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 6,
                result = (Vector3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.Run(m_Output.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();

            CheckResult();
        }
        
        [Test, Performance]
        public unsafe void ConvertNormalsUInt16ToFloatInterleavedNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertNormalsUInt16ToFloatInterleavedNormalizedJob {
                input = (byte*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 6,
                result = (Vector3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.Run(m_Output.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckNormalizedResult(Constants.epsilonUInt16);
        }
        
        [Test, Performance]
        public unsafe void ConvertPositionsInt16ToFloatInterleavedJob() {
            var job = new GLTFast.Jobs.ConvertPositionsInt16ToFloatInterleavedJob {
                input = (byte*)m_InputInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 6,
                result = (Vector3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.Run(m_Output.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckResult();
        }
        
        [Test, Performance]
        public unsafe void ConvertNormalsInt16ToFloatInterleavedNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertNormalsInt16ToFloatInterleavedNormalizedJob {
                input = (byte*)m_InputInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 6,
                result = (Vector3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.Run(m_Output.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckNormalizedResult(Constants.epsilonInt16);
        }
        
        [Test, Performance]
        public unsafe void ConvertPositionsInt8ToFloatInterleavedJob() {
            var job = new GLTFast.Jobs.ConvertPositionsInt8ToFloatInterleavedJob {
                input = (sbyte*)m_InputInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 3,
                result = (Vector3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.Run(m_Output.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckResult();
        }
        
        [Test, Performance]
        public unsafe void ConvertNormalsInt8ToFloatInterleavedNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertNormalsInt8ToFloatInterleavedNormalizedJob {
                input = (sbyte*)m_InputInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 3,
                result = (Vector3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.Run(m_Output.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckNormalizedResult(Constants.epsilonInt8);
        }
        
        [Test, Performance]
        public unsafe void ConvertPositionsUInt8ToFloatInterleavedJob() {
            var job = new GLTFast.Jobs.ConvertPositionsUInt8ToFloatInterleavedJob {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 3,
                result = (Vector3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.Run(m_Output.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckResult();
        }
        
        [Test, Performance]
        public unsafe void ConvertNormalsUInt8ToFloatInterleavedNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertNormalsUInt8ToFloatInterleavedNormalizedJob {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 3,
                result = (Vector3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.Run(m_Output.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckNormalizedResult(Constants.epsilonUInt8);
        }
    }

    [TestFixture]
    public class PositionSparseJobs {
        
        const int k_Length = 5_000;
        
        NativeArray<int> m_Indices;
        NativeArray<float3> m_Input;
        NativeArray<float3> m_Output;

        [OneTimeSetUp]
        public void SetUpTest() {
            m_Indices = new NativeArray<int>(k_Length, Allocator.Persistent);
            m_Input = new NativeArray<float3>(k_Length, Allocator.Persistent);
            m_Output = new NativeArray<float3>(k_Length*2, Allocator.Persistent);

            for (int i = 0; i < k_Length; i++) {
                m_Indices[i] = i*2;
                m_Input[i] = new float3(i, k_Length-1, 42);
            }
        }

        void CheckResult() {
            var endIndex = math.min(k_Length, 10);
            for (int i = 0; i < endIndex; i++) {
                Utils.AssertNearOrEqual(new float3(-i, k_Length-1, 42), m_Output[i*2]);
            }
        }

        [OneTimeTearDown]
        public void Cleanup() {
            m_Input.Dispose();
            m_Output.Dispose();
            m_Indices.Dispose();
        }
        
        [Test, Performance]
        public unsafe void ConvertPositionsSparseJob() {
            const GLTFComponentType indexType = GLTFComponentType.UnsignedInt;
            const GLTFComponentType valueType = GLTFComponentType.Float;
            const bool normalized = false;

            var job = new GLTFast.Jobs.ConvertVector3SparseJob {
                indexBuffer = m_Indices.GetUnsafeReadOnlyPtr(),
                indexConverter = GLTFast.Jobs.CachedFunction.GetIndexConverter(indexType),
                inputByteStride = 3*Accessor.GetComponentTypeSize(valueType),
                input = m_Input.GetUnsafeReadOnlyPtr(),
                valueConverter = GLTFast.Jobs.CachedFunction.GetPositionConverter(valueType,normalized),
                outputByteStride = 12,
                result = (Vector3*) m_Output.GetUnsafePtr(),
            };
            Measure.Method(() => job.Run(m_Indices.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();

            CheckResult();
        }
    }

    [TestFixture]
    public class UVJobs {
        const int k_UVLength = 100;
        float2 m_NormalizedReference = new float2(.5f, 0f);
        float2 m_Reference = new float2(13, -41);

        NativeArray<float2> m_UVInput;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<short> m_InputInt16;
        NativeArray<byte> m_InputUInt8;
        NativeArray<sbyte> m_InputInt8;
        NativeArray<float2> m_UVOutput;
        
        [OneTimeSetUp]
        public void SetUpTest() {
            m_UVInput = new NativeArray<float2>(k_UVLength, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_UVLength*2, Allocator.Persistent);
            m_InputInt16 = new NativeArray<short>(k_UVLength*2, Allocator.Persistent);
            m_InputUInt8 = new NativeArray<byte>(k_UVLength*2, Allocator.Persistent);
            m_InputInt8 = new NativeArray<sbyte>(k_UVLength*2, Allocator.Persistent);
            m_UVOutput = new NativeArray<float2>(k_UVLength, Allocator.Persistent);
            
            var i = 1;
            {
                m_UVInput[i] = new float2(.5f, 1f);

                m_InputUInt8[i*2] = byte.MaxValue/2;
                m_InputUInt8[i*2+1] = byte.MaxValue;
                
                m_InputInt8[i*2] = SByte.MaxValue/2;
                m_InputInt8[i*2+1] = SByte.MaxValue;
                
                m_InputUInt16[i*2] = ushort.MaxValue/2;
                m_InputUInt16[i*2+1] = ushort.MaxValue;
                
                m_InputInt16[i*2] = short.MaxValue/2;
                m_InputInt16[i*2+1] = short.MaxValue;
            }

            i = 2;
            {
                m_UVInput[i] = new float2(13,42);

                m_InputUInt8[i*2] = 13;
                m_InputUInt8[i*2+1] = 42;
                
                m_InputInt8[i*2] = 13;
                m_InputInt8[i*2+1] = 42;
                
                m_InputUInt16[i*2] = 13;
                m_InputUInt16[i*2+1] = 42;
                
                m_InputInt16[i*2] = 13;
                m_InputInt16[i*2+1] = 42;
            }
        }
        
        [OneTimeTearDown]
        public void Cleanup() {
            m_UVInput.Dispose();
            m_InputUInt16.Dispose();
            m_InputInt16.Dispose();
            m_InputUInt8.Dispose();
            m_InputInt8.Dispose();
            m_UVOutput.Dispose();
        }

        void CheckNormalizedResult(float epsilon = float.Epsilon) {
            const int i = 1;
            Utils.AssertNearOrEqual(m_NormalizedReference, m_UVOutput[i], epsilon);
        }
        
        void CheckResult(float epsilon = float.Epsilon) {
            const int i = 2;
            Utils.AssertNearOrEqual(m_Reference, m_UVOutput[i], epsilon);
        }
        
        // [Test, Performance]
        // public unsafe void ConvertUVsUInt8ToFloatJob() {
        //     Measure.Method(() => {
        //             var job = new GLTFast.Jobs.ConvertUVsUInt8ToFloatJob {
        //                 input = (byte*)m_UVInput.GetUnsafeReadOnlyPtr(),
        //                 result = (Vector2*)m_UVOutput.GetUnsafePtr()
        //             };
        //             job.Run(m_UVOutput.Length);
        //         })
        //         .WarmupCount(1)
        //         .MeasurementCount(Constants.measureCount)
        //         .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
        //         .Run();
        // }
        //
        // [Test, Performance]
        // public unsafe void ConvertUVsUInt8ToFloatNormalizedJob() {
        //     Measure.Method(() => {
        //             var job = new GLTFast.Jobs.ConvertUVsUInt8ToFloatNormalizedJob {
        //                 input = (byte*)m_UVInput.GetUnsafeReadOnlyPtr(),
        //                 result = (Vector2*)m_UVOutput.GetUnsafePtr()
        //             };
        //             job.Run(m_UVOutput.Length);
        //         })
        //         .WarmupCount(1)
        //         .MeasurementCount(Constants.measureCount)
        //         .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
        //         .Run();
        // }
        //
        // [Test, Performance]
        // public unsafe void ConvertUVsUInt16ToFloatNormalizedJob() {
        //     Measure.Method(() => {
        //             var job = new GLTFast.Jobs.ConvertUVsUInt16ToFloatNormalizedJob {
        //                 input = (ushort*)m_UVInput.GetUnsafeReadOnlyPtr(),
        //                 result = (Vector2*)m_UVOutput.GetUnsafePtr()
        //             };
        //             job.Run(m_UVOutput.Length);
        //         })
        //         .WarmupCount(1)
        //         .MeasurementCount(Constants.measureCount)
        //         .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
        //         .Run();
        // }
        //
        // [Test, Performance]
        // public unsafe void ConvertUVsUInt16ToFloatJob() {
        //     Measure.Method(() => {
        //             var job = new GLTFast.Jobs.ConvertUVsUInt16ToFloatJob {
        //                 input = (ushort*)m_UVInput.GetUnsafeReadOnlyPtr(),
        //                 result = (Vector2*)m_UVOutput.GetUnsafePtr()
        //             };
        //             job.Run(m_UVOutput.Length);
        //         })
        //         .WarmupCount(1)
        //         .MeasurementCount(Constants.measureCount)
        //         .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
        //         .Run();
        // }
        //
        // [Test, Performance]
        // public unsafe void ConvertUVsFloatToFloatJob() {
        //     Measure.Method(() => {
        //             var job = new GLTFast.Jobs.ConvertUVsFloatToFloatJob {
        //                 input = (float*)m_UVInput.GetUnsafeReadOnlyPtr(),
        //                 result = (Vector2*)m_UVOutput.GetUnsafePtr()
        //             };
        //             job.Run(m_UVOutput.Length);
        //         })
        //         .WarmupCount(1)
        //         .MeasurementCount(Constants.measureCount)
        //         .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
        //         .Run();
        // }
        
        [Test, Performance]
        public unsafe void ConvertUVsUInt8ToFloatInterleavedJob() {
            var job = new GLTFast.Jobs.ConvertUVsUInt8ToFloatInterleavedJob {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 2,
                result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.Run(m_UVOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();

            CheckResult(Constants.epsilonUInt8);
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsUInt8ToFloatInterleavedNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertUVsUInt8ToFloatInterleavedNormalizedJob {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 2,
                result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.Run(m_UVOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckNormalizedResult(Constants.epsilonUInt8);
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsUInt16ToFloatInterleavedJob() {
            var job = new GLTFast.Jobs.ConvertUVsUInt16ToFloatInterleavedJob {
                input = (byte*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.Run(m_UVOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckResult(Constants.epsilonUInt16);
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsUInt16ToFloatInterleavedNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertUVsUInt16ToFloatInterleavedNormalizedJob {
                input = (byte*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.Run(m_UVOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();

            CheckNormalizedResult(Constants.epsilonUInt16);
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsInt16ToFloatInterleavedJob() {
            var job = new GLTFast.Jobs.ConvertUVsInt16ToFloatInterleavedJob {
                input = (short*)m_InputInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.Run(m_UVOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckResult(Constants.epsilonInt16);
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsInt16ToFloatInterleavedNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertUVsInt16ToFloatInterleavedNormalizedJob {
                input = (short*)m_InputInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.Run(m_UVOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckNormalizedResult(Constants.epsilonInt16);
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsInt8ToFloatInterleavedJob() {
            var job = new GLTFast.Jobs.ConvertUVsInt8ToFloatInterleavedJob {
                input = (sbyte*)m_InputInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 2,
                result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.Run(m_UVOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckResult(Constants.epsilonInt8);
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsInt8ToFloatInterleavedNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertUVsInt8ToFloatInterleavedNormalizedJob {
                input = (sbyte*)m_InputInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 2,
                result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.Run(m_UVOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckNormalizedResult(Constants.epsilonInt8);
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsFloatToFloatInterleavedJob() {
            var job = new GLTFast.Jobs.ConvertUVsFloatToFloatInterleavedJob {
                input = (byte*)m_UVInput.GetUnsafeReadOnlyPtr(),
                inputByteStride = 8,
                result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.Run(m_UVOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckResult();
            CheckNormalizedResult();
        }
    }
    
    [TestFixture]
    public class Vector4Jobs {
        const int k_RotationLength = 5_000_000;
        float4 m_NormalizedReference = new float4(0.844623f, -0.191342f,-0.46194f,0.191342f);
        float4 m_NormalizedTangentReference = new float4(0.844623f, 0.191342f,-0.46194f,0.191342f);
        float4 m_RotationReference = new float4(2, -13, -42, 1);
        float4 m_TangentReference = new float4(2, 13, -42, 1);
        float4 m_Reference = new float4(2, 13, 42, 1);

        NativeArray<float4> m_RotInput;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<short> m_InputInt16;
        NativeArray<byte> m_InputUInt8;
        NativeArray<sbyte> m_InputInt8;
        NativeArray<float4> m_RotOutput;

        [OneTimeSetUp]
        public void SetUpTest() {
            m_RotInput = new NativeArray<float4>(k_RotationLength, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_RotationLength*4, Allocator.Persistent);
            m_InputInt16 = new NativeArray<short>(k_RotationLength*4, Allocator.Persistent);
            m_InputUInt8 = new NativeArray<byte>(k_RotationLength*4, Allocator.Persistent);
            m_InputInt8 = new NativeArray<sbyte>(k_RotationLength*4, Allocator.Persistent);
            m_RotOutput = new NativeArray<float4>(k_RotationLength, Allocator.Persistent);
            
            var i = 1;
            {
                var tmp = new float4(0.844623f, 0.191342f,0.46194f,0.191342f);
                m_RotInput[i] = tmp; 

                m_InputUInt8[i*4] = (byte) (byte.MaxValue*tmp.x);
                m_InputUInt8[i*4+1] = (byte) (byte.MaxValue*tmp.y);
                m_InputUInt8[i*4+2] = (byte) (byte.MaxValue*tmp.z);
                m_InputUInt8[i*4+3] = (byte) (byte.MaxValue*tmp.w);
                
                m_InputInt8[i*4] = (sbyte) (sbyte.MaxValue*tmp.x);
                m_InputInt8[i*4+1] = (sbyte) (sbyte.MaxValue*tmp.y);
                m_InputInt8[i*4+2] = (sbyte) (sbyte.MaxValue*tmp.z);
                m_InputInt8[i*4+3] = (sbyte) (sbyte.MaxValue*tmp.w);
                
                m_InputUInt16[i*4] = (ushort) (ushort.MaxValue*tmp.x);
                m_InputUInt16[i*4+1] = (ushort) (ushort.MaxValue*tmp.y);
                m_InputUInt16[i*4+2] = (ushort) (ushort.MaxValue*tmp.z);
                m_InputUInt16[i*4+3] = (ushort) (ushort.MaxValue*tmp.w);
                
                m_InputInt16[i*4] = (short) (short.MaxValue*tmp.x);
                m_InputInt16[i*4+1] = (short) (short.MaxValue*tmp.y);
                m_InputInt16[i*4+2] = (short) (short.MaxValue*tmp.z);
                m_InputInt16[i*4+3] = (short) (short.MaxValue*tmp.w);
            }

            i = 2;
            {
                m_RotInput[i] = new float4(2, 13, 42, 1);

                m_InputUInt8[i*4] = 2;
                m_InputUInt8[i*4+1] = 13;
                m_InputUInt8[i*4+2] = 42;
                m_InputUInt8[i*4+3] = 1;
                
                m_InputInt8[i*4] = 2;
                m_InputInt8[i*4+1] = 13;
                m_InputInt8[i*4+2] = 42;
                m_InputInt8[i*4+3] = 1;
                
                m_InputUInt16[i*4] = 2;
                m_InputUInt16[i*4+1] = 13;
                m_InputUInt16[i*4+2] = 42;
                m_InputUInt16[i*4+3] = 1;
                
                m_InputInt16[i*4] = 2;
                m_InputInt16[i*4+1] = 13;
                m_InputInt16[i*4+2] = 42;
                m_InputInt16[i*4+3] = 1;
            }
        }

        [OneTimeTearDown]
        public void Cleanup() {
            m_RotInput.Dispose();
            m_InputUInt16.Dispose();
            m_InputInt16.Dispose();
            m_InputUInt8.Dispose();
            m_InputInt8.Dispose();
            m_RotOutput.Dispose();
        }

        void CheckNormalizedResult(float epsilon = float.Epsilon) {
            const int i = 1;
            Utils.AssertNearOrEqual(m_NormalizedReference, m_RotOutput[i], epsilon);
        }
        
        void CheckNormalizedTangentResult(float epsilon = float.Epsilon) {
            const int i = 1;
            Utils.AssertNearOrEqual(m_NormalizedTangentReference, m_RotOutput[i], epsilon);
        }
        
        void CheckResult(float epsilon = float.Epsilon) {
            const int i = 2;
            Utils.AssertNearOrEqual(m_RotationReference, m_RotOutput[i], epsilon);
        }
        
        void CheckTangentResult(float epsilon = float.Epsilon) {
            const int i = 2;
            Utils.AssertNearOrEqual(m_TangentReference, m_RotOutput[i], epsilon);
        }
        
        void CheckBoneWeightResult(float epsilon = float.Epsilon) {
            const int i = 2;
            Utils.AssertNearOrEqual(m_Reference, m_RotOutput[i], epsilon);
        }

        [Test, Performance]
        public unsafe void ConvertRotationsFloatToFloatJob() {
            var job = new GLTFast.Jobs.ConvertRotationsFloatToFloatJob {
                input = (float*)m_RotInput.GetUnsafeReadOnlyPtr(),
                result = (float*)m_RotOutput.GetUnsafePtr()
            };
            Measure.Method(() => job.Run(m_RotOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckNormalizedResult();
            CheckResult();
        }
        
        [Test, Performance]
        public unsafe void ConvertRotationsInt16ToFloatJob() {
            var job = new GLTFast.Jobs.ConvertRotationsInt16ToFloatJob {
                input = (short*)m_InputInt16.GetUnsafeReadOnlyPtr(),
                result = (float*)m_RotOutput.GetUnsafePtr()
            };
            Measure.Method(() => job.Run(m_RotOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckNormalizedResult(Constants.epsilonInt16);
        }
        
        [Test, Performance]
        public unsafe void ConvertRotationsInt8ToFloatJob() {
            m_InputInt8[0] = sbyte.MinValue;
            m_InputInt8[1] = -64;
            m_InputInt8[2] = 64;
            m_InputInt8[3] = sbyte.MaxValue;

            var job = new GLTFast.Jobs.ConvertRotationsInt8ToFloatJob {
                input = (sbyte*)m_InputInt8.GetUnsafeReadOnlyPtr(),
                result = (float*)m_RotOutput.GetUnsafePtr()
            };
            Measure.Method(() => job.Run(m_RotOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            Utils.AssertNearOrEqual(new float4(-1,.5f,-.5f,1), m_RotOutput[0],Constants.epsilonInt8);
            CheckNormalizedResult(Constants.epsilonInt8);
        }
        
        [Test, Performance]
        public unsafe void ConvertTangentsFloatToFloatInterleavedJob() {
            var job = new GLTFast.Jobs.ConvertTangentsFloatToFloatInterleavedJob {
                input = (byte*)m_RotInput.GetUnsafeReadOnlyPtr(),
                inputByteStride = 16,
                result = (Vector4*)m_RotOutput.GetUnsafePtr(),
                outputByteStride = 16
            };
            Measure.Method(() => job.Run(m_RotOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckTangentResult();
        }
        
        [Test, Performance]
        public unsafe void ConvertBoneWeightsFloatToFloatInterleavedJob() {
            var job = new GLTFast.Jobs.ConvertBoneWeightsFloatToFloatInterleavedJob {
                input = (byte*)m_RotInput.GetUnsafeReadOnlyPtr(),
                inputByteStride = 16,
                result = (Vector4*)m_RotOutput.GetUnsafePtr(),
                outputByteStride = 16
            };
            Measure.Method(() => job.Run(m_RotOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            CheckBoneWeightResult();
        }
        
        [Test, Performance]
        public unsafe void ConvertTangentsInt16ToFloatInterleavedNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertTangentsInt16ToFloatInterleavedNormalizedJob {
                input = (short*)m_InputInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 8,
                result = (Vector4*)m_RotOutput.GetUnsafePtr(),
                outputByteStride = 16
            };
            Measure.Method(() => job.Run(m_RotOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckNormalizedTangentResult(Constants.epsilonInt16);
        }
        
        [Test, Performance]
        public unsafe void ConvertTangentsInt8ToFloatInterleavedNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertTangentsInt8ToFloatInterleavedNormalizedJob {
                input = (sbyte*)m_InputInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = (Vector4*)m_RotOutput.GetUnsafePtr(),
                outputByteStride = 16
            };
            Measure.Method(() => job.Run(m_RotOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckNormalizedTangentResult(Constants.epsilonInt8);
        }
    }
    
    [TestFixture]
    public class ColorJobs {
        const int k_ColorLength = 3_000_000;
        Color m_ReferenceRGB = new Color(.13f,.42f,.95f,1f);
        Color m_ReferenceRGBA = new Color(.42f,.95f,.5f,.24f);

        NativeArray<float> m_ColorInput;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<byte> m_InputUInt8;
        NativeArray<Color> m_ColorOutput;

        [OneTimeSetUp]
        public void SetUpTest() {
            m_ColorInput = new NativeArray<float>(k_ColorLength, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_ColorLength, Allocator.Persistent);
            m_InputUInt8 = new NativeArray<byte>(k_ColorLength, Allocator.Persistent);
            m_ColorOutput = new NativeArray<Color>(k_ColorLength, Allocator.Persistent);
            
            m_ColorInput[3] = m_ReferenceRGB.r;
            m_ColorInput[4] = m_ReferenceRGB.g;
            m_ColorInput[5] = m_ReferenceRGB.b;
            m_ColorInput[6] = m_ReferenceRGBA.b;
            m_ColorInput[7] = m_ReferenceRGBA.a;

            m_InputUInt8[3] = (byte) (byte.MaxValue*m_ReferenceRGB.r);
            m_InputUInt8[4] = (byte) (byte.MaxValue*m_ReferenceRGB.g);
            m_InputUInt8[5] = (byte) (byte.MaxValue*m_ReferenceRGB.b);
            m_InputUInt8[6] = (byte) (byte.MaxValue*m_ReferenceRGBA.b);
            m_InputUInt8[7] = (byte) (byte.MaxValue*m_ReferenceRGBA.a);
            
            m_InputUInt16[3] = (ushort) (ushort.MaxValue*m_ReferenceRGB.r);
            m_InputUInt16[4] = (ushort) (ushort.MaxValue*m_ReferenceRGB.g);
            m_InputUInt16[5] = (ushort) (ushort.MaxValue*m_ReferenceRGB.b);
            m_InputUInt16[6] = (ushort) (ushort.MaxValue*m_ReferenceRGBA.b);
            m_InputUInt16[7] = (ushort) (ushort.MaxValue*m_ReferenceRGBA.a);
        }

        [OneTimeTearDown]
        public void Cleanup() {
            m_ColorInput.Dispose();
            m_InputUInt8.Dispose();
            m_InputUInt16.Dispose();
            m_ColorOutput.Dispose();
        }

        void CheckResultRGB(float epsilon = float.Epsilon) {
            const int i = 1;
            Utils.AssertNearOrEqual(m_ReferenceRGB, m_ColorOutput[i], epsilon);
        }
        
        void CheckResultRGBA(float epsilon = float.Epsilon) {
            const int i = 1;
            Utils.AssertNearOrEqual(m_ReferenceRGBA, m_ColorOutput[i], epsilon);
        }
        
        [Test, Performance]
        public unsafe void ConvertColorsRGBFloatToRGBAFloatJob() {
            var job = new GLTFast.Jobs.ConvertColorsRGBFloatToRGBAFloatJob {
                input = (float*)m_ColorInput.GetUnsafeReadOnlyPtr(),
                inputByteStride = 12,
                result = m_ColorOutput
            };
            Measure.Method(() => job.Run(m_ColorOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();

            CheckResultRGB();
        }
        
        [Test, Performance]
        public unsafe void ConvertColorsRGBUInt8ToRGBAFloatJob() {
            var job = new GLTFast.Jobs.ConvertColorsRGBUInt8ToRGBAFloatJob {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 3,
                result = m_ColorOutput
            };
            Measure.Method(() => job.Run(m_ColorOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckResultRGB(Constants.epsilonUInt8);
        }
        
        [Test, Performance]
        public unsafe void ConvertColorsRGBUInt16ToRGBAFloatJob() {
            var job = new GLTFast.Jobs.ConvertColorsRGBUInt16ToRGBAFloatJob {
                input = (ushort*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 6,
                result = m_ColorOutput
            };
            Measure.Method(() => job.Run(m_ColorOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckResultRGB(Constants.epsilonUInt16);
        }
        
        [Test, Performance]
        public unsafe void ConvertColorsInterleavedRGBAFloatToRGBAFloatJob() {
            var job = new GLTFast.Jobs.ConvertColorsInterleavedRGBAFloatToRGBAFloatJob {
                input = (float*)m_ColorInput.GetUnsafeReadOnlyPtr(),
                inputByteStride = 16,
                result = m_ColorOutput
            };
            Measure.Method(() => job.Run(m_ColorOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckResultRGBA();
        }
        
        [Test, Performance]
        public unsafe void ConvertColorsInterleavedRGBAUInt16ToRGBAFloatJob() {
            var job = new GLTFast.Jobs.ConvertColorsInterleavedRGBAUInt16ToRGBAFloatJob {
                input = (ushort*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 8,
                result = m_ColorOutput
            };
            Measure.Method(() => job.Run(m_ColorOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckResultRGBA(Constants.epsilonUInt16);
        }
        
        [Test, Performance]
        public unsafe void ConvertColorsRGBAUInt8ToRGBAFloatJob() {
            var job = new GLTFast.Jobs.ConvertColorsRGBAUInt8ToRGBAFloatJob {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = m_ColorOutput
            };
            Measure.Method(() => job.Run(m_ColorOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            CheckResultRGBA(Constants.epsilonUInt8);
        }
    }
    
    [TestFixture]
    public class BoneIndexJobs {
        const int k_BoneIndexLength = 2_000_000;
        uint4 m_Reference = new uint4(2, 3, 4, 5);
        NativeArray<byte> m_InputUInt8;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<uint4> m_BoneIndexOutput;

        [OneTimeSetUp]
        public void SetUpTest() {
            m_InputUInt8 = new NativeArray<byte>(k_BoneIndexLength*4, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_BoneIndexLength*4, Allocator.Persistent);
            m_BoneIndexOutput = new NativeArray<uint4>(k_BoneIndexLength, Allocator.Persistent);

            m_InputUInt8[4] = (byte)m_Reference.x;
            m_InputUInt8[5] = (byte)m_Reference.y;
            m_InputUInt8[6] = (byte)m_Reference.z;
            m_InputUInt8[7] = (byte)m_Reference.w;
            
            m_InputUInt16[4] = (ushort)m_Reference.x;
            m_InputUInt16[5] = (ushort)m_Reference.y;
            m_InputUInt16[6] = (ushort)m_Reference.z;
            m_InputUInt16[7] = (ushort)m_Reference.w;
        }

        [OneTimeTearDown]
        public void Cleanup() {
            m_InputUInt16.Dispose();
            m_InputUInt8.Dispose();
            m_BoneIndexOutput.Dispose();
        }
        
        void CheckResult() {
            const int i = 1;
            Utils.AssertNearOrEqual(m_Reference, m_BoneIndexOutput[i]);
        }

        [Test, Performance]
        public unsafe void ConvertBoneJointsUInt8ToUInt32Job() {
            var job = new GLTFast.Jobs.ConvertBoneJointsUInt8ToUInt32Job {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = (uint*)m_BoneIndexOutput.GetUnsafePtr(),
                outputByteStride = 16
            };
            Measure.Method(() => job.Run(m_BoneIndexOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            CheckResult();
        }
        
        [Test, Performance]
        public unsafe void ConvertBoneJointsUInt16ToUInt32Job() {
            var job = new GLTFast.Jobs.ConvertBoneJointsUInt16ToUInt32Job {
                input = (byte*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 8,
                result = (uint*)m_BoneIndexOutput.GetUnsafePtr(),
                outputByteStride = 16
            };
            Measure.Method(() => {
                    job.Run(m_BoneIndexOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            CheckResult();
        }
    }
    
    [TestFixture]
    public class MatrixJobs {
        const int k_MatrixLength = 800_000;
        static readonly Matrix4x4 m_Reference = new Matrix4x4(
            new Vector4(1,-5,-9,13),
            new Vector4(-2,6,10,14),
            new Vector4(-3,7,11,15),
            new Vector4(-4,8,12,16)
        );
        NativeArray<float4x4> m_MatrixInput;
        NativeArray<Matrix4x4> m_MatrixOutput;

        [OneTimeSetUp]
        public void SetUpTest() {
            m_MatrixInput = new NativeArray<float4x4>(k_MatrixLength, Allocator.Persistent);
            m_MatrixOutput = new NativeArray<Matrix4x4>(k_MatrixLength, Allocator.Persistent);

            m_MatrixInput[1] = new float4x4(
                1,2,3,4,
                5,6,7,8,
                9,10,11,12,
                13,14,15,16
                );
        }

        [OneTimeTearDown]
        public void Cleanup() {
            m_MatrixInput.Dispose();
            m_MatrixOutput.Dispose();
        }
        
        [Test, Performance]
        public unsafe void ConvertMatricesJob() {
            var job = new GLTFast.Jobs.ConvertMatricesJob {
                input = (Matrix4x4*)m_MatrixInput.GetUnsafeReadOnlyPtr(),
                result = m_MatrixOutput,
            };
            Measure.Method(() => job.Run(m_MatrixOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();

            Assert.AreEqual(m_Reference, m_MatrixOutput[1]);
        }
    }
    
    [TestFixture]
    public class IndexJobs {
        const int k_IndexLength = 24_000_000; // multiple of 3!
        NativeArray<byte> m_InputUInt8;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<uint> m_InputUInt32;
        NativeArray<int> m_IndexOutput;

        [OneTimeSetUp]
        public void SetUpTest() {
            m_InputUInt8 = new NativeArray<byte>(k_IndexLength, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_IndexLength, Allocator.Persistent);
            m_InputUInt32 = new NativeArray<uint>(k_IndexLength, Allocator.Persistent);
            m_IndexOutput = new NativeArray<int>(k_IndexLength, Allocator.Persistent);

            for (int i = 0; i < 6; i++) {
                m_InputUInt8[i] = (byte)i;
                m_InputUInt16[i] = (ushort)i;
                m_InputUInt32[i] = (uint)i;
            }
        }

        [OneTimeTearDown]
        public void Cleanup() {
            m_InputUInt8.Dispose();
            m_InputUInt16.Dispose();
            m_InputUInt32.Dispose();
            m_IndexOutput.Dispose();
        }

        void CheckResult() {
            for (int i = 0; i < 6; i++) {
                Assert.AreEqual(i,m_IndexOutput[i]);
            }
        }
        
        void CheckResultFlipped() {
            Assert.AreEqual(0,m_IndexOutput[0]);
            Assert.AreEqual(2,m_IndexOutput[1]);
            Assert.AreEqual(1,m_IndexOutput[2]);
            Assert.AreEqual(3,m_IndexOutput[3]);
            Assert.AreEqual(5,m_IndexOutput[4]);
            Assert.AreEqual(4,m_IndexOutput[5]);
        }
        
        [Test, Performance]
        public unsafe void CreateIndicesInt32Job() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            var job = new GLTFast.Jobs.CreateIndicesInt32Job {
                result = (int*)m_IndexOutput.GetUnsafePtr()
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length/3))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();

            CheckResult();
        }
        
        [Test, Performance]
        public unsafe void CreateIndicesInt32FlippedJob() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            var job = new GLTFast.Jobs.CreateIndicesInt32FlippedJob {
                result = (int*)m_IndexOutput.GetUnsafePtr()
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length/3))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();

            Assert.AreEqual(2,m_IndexOutput[0]);
            Assert.AreEqual(1,m_IndexOutput[1]);
            Assert.AreEqual(0,m_IndexOutput[2]);
            Assert.AreEqual(5,m_IndexOutput[3]);
            Assert.AreEqual(4,m_IndexOutput[4]);
            Assert.AreEqual(3,m_IndexOutput[5]);
        }
        
        [Test, Performance]
        public unsafe void ConvertIndicesUInt8ToInt32Job() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            var job = new GLTFast.Jobs.ConvertIndicesUInt8ToInt32Job {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                result = (int*)m_IndexOutput.GetUnsafePtr()
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length/3))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            CheckResult();
        }
        
        [Test, Performance]
        public unsafe void ConvertIndicesUInt8ToInt32FlippedJob() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            var job = new GLTFast.Jobs.ConvertIndicesUInt8ToInt32FlippedJob {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                result = (int*)m_IndexOutput.GetUnsafePtr()
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length/3))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            CheckResultFlipped();
        }
        
        [Test, Performance]
        public unsafe void ConvertIndicesUInt16ToInt32FlippedJob() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            var job = new GLTFast.Jobs.ConvertIndicesUInt16ToInt32FlippedJob {
                input = (ushort*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                result = (int*)m_IndexOutput.GetUnsafePtr()
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length/3))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            CheckResultFlipped();
        }
        
        [Test, Performance]
        public unsafe void ConvertIndicesUInt16ToInt32Job() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            var job = new GLTFast.Jobs.ConvertIndicesUInt16ToInt32Job {
                input = (ushort*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                result = (int*)m_IndexOutput.GetUnsafePtr()
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length/3))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            CheckResult();
        }
        
        [Test, Performance]
        public unsafe void ConvertIndicesUInt32ToInt32Job() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            var job = new GLTFast.Jobs.ConvertIndicesUInt32ToInt32Job {
                input = (uint*)m_InputUInt32.GetUnsafeReadOnlyPtr(),
                result = (int*)m_IndexOutput.GetUnsafePtr()
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length/3))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            CheckResult();
        }
        
        [Test, Performance]
        public unsafe void ConvertIndicesUInt32ToInt32FlippedJob() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            var job = new GLTFast.Jobs.ConvertIndicesUInt32ToInt32FlippedJob {
                input = (uint*)m_InputUInt32.GetUnsafeReadOnlyPtr(),
                result = (int*)m_IndexOutput.GetUnsafePtr()
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length/3))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            CheckResultFlipped();
        }
    }
    
    [TestFixture]
    public class ScalarJobs {
        const int k_ScalarLength = 5_000_000;
        NativeArray<sbyte> m_InputInt8;
        NativeArray<byte> m_InputUInt8;
        NativeArray<short> m_InputInt16;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<float> m_ScalarOutput;

        [OneTimeSetUp]
        public void SetUpTest() {
            m_InputInt8 = new NativeArray<sbyte>(k_ScalarLength, Allocator.Persistent);
            m_InputUInt8 = new NativeArray<byte>(k_ScalarLength, Allocator.Persistent);
            m_InputInt16 = new NativeArray<short>(k_ScalarLength, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_ScalarLength, Allocator.Persistent);
            m_ScalarOutput = new NativeArray<float>(k_ScalarLength, Allocator.Persistent);

            m_InputInt8[0] = sbyte.MaxValue;
            m_InputUInt8[0] = byte.MaxValue;
            m_InputInt16[0] = short.MaxValue;
            m_InputUInt16[0] = ushort.MaxValue;
            
            m_InputInt8[1] = 0;
            m_InputUInt8[1] = 0;
            m_InputInt16[1] = 0;
            m_InputUInt16[1] = 0;
            
            m_InputInt8[2] = sbyte.MinValue;
            m_InputUInt8[2] = byte.MinValue;
            m_InputInt16[2] = short.MinValue;
            m_InputUInt16[2] = ushort.MinValue;
            
            m_InputInt8[3] = sbyte.MaxValue / 2;
            m_InputUInt8[3] = byte.MaxValue / 2;
            m_InputInt16[3] = short.MaxValue / 2;
            m_InputUInt16[3] = ushort.MaxValue / 2;
        }
        
        [OneTimeTearDown]
        public void Cleanup() {
            m_InputInt8.Dispose();
            m_InputUInt8.Dispose();
            m_InputInt16.Dispose();
            m_InputUInt16.Dispose();
            m_ScalarOutput.Dispose();
        }

        void CheckResult(float epsilon = float.Epsilon) {
            Utils.AssertNearOrEqual(1,m_ScalarOutput[0],epsilon);
            Utils.AssertNearOrEqual(0,m_ScalarOutput[1],epsilon);
            Utils.AssertNearOrEqual(0,m_ScalarOutput[2],epsilon);
            Utils.AssertNearOrEqual(.5f,m_ScalarOutput[3],epsilon);
        }
        
        void CheckSignedResult(float epsilon = float.Epsilon) {
            Utils.AssertNearOrEqual(1,m_ScalarOutput[0],epsilon);
            Utils.AssertNearOrEqual(0,m_ScalarOutput[1],epsilon);
            Utils.AssertNearOrEqual(-1,m_ScalarOutput[2],epsilon);
            Utils.AssertNearOrEqual(.5f,m_ScalarOutput[3],epsilon);
        }
        
        [Test, Performance]
        public unsafe void ConvertScalarInt8ToFloatNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertScalarInt8ToFloatNormalizedJob {
                input = (sbyte*)m_InputInt8.GetUnsafeReadOnlyPtr(),
                result = m_ScalarOutput,
            };
            Measure.Method(() => job.Run(m_ScalarOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            CheckSignedResult(Constants.epsilonInt8);
        }
        
        [Test, Performance]
        public unsafe void ConvertScalarUInt8ToFloatNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertScalarUInt8ToFloatNormalizedJob {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                result = m_ScalarOutput,
            };
            Measure.Method(() => job.Run(m_ScalarOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            CheckResult(Constants.epsilonUInt8);
        }
        
        [Test, Performance]
        public unsafe void ConvertScalarInt16ToFloatNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertScalarInt16ToFloatNormalizedJob {
                input = (short*)m_InputInt16.GetUnsafeReadOnlyPtr(),
                result = m_ScalarOutput,
            };
            Measure.Method(() => job.Run(m_ScalarOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            CheckSignedResult(Constants.epsilonInt16);
        }
        
        [Test, Performance]
        public unsafe void ConvertScalarUInt16ToFloatNormalizedJob() {
            var job = new GLTFast.Jobs.ConvertScalarUInt16ToFloatNormalizedJob {
                input = (ushort*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                result = m_ScalarOutput,
            };
            Measure.Method(() => job.Run(m_ScalarOutput.Length))
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            CheckResult(Constants.epsilonUInt16);
        }
    }
}