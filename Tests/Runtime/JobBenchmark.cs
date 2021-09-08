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
    }
    
    [TestFixture]
    public class Vector3Jobs {

        const int k_Length = 10_000_000;
        float3 m_Reference = new float3(-.5f, .5f, 0.707107f);
        float3 m_Reference2 = new float3(-1, 13, 42);
        
        NativeArray<float3> m_Input;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<short> m_InputInt16;
        NativeArray<byte> m_InputUInt8;
        NativeArray<sbyte> m_InputInt8;
        NativeArray<float3> m_Output;

        [SetUp]
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
        
        [TearDown]
        public void Cleanup() {
            m_Input.Dispose();
            m_InputUInt16.Dispose();
            m_InputInt16.Dispose();
            m_InputUInt8.Dispose();
            m_InputInt8.Dispose();
            m_Output.Dispose();
        }

        void AssertFloat3NearOrEqual(float3 reference, float3 value, float epsilon) {
            const int i = 1;
            var delta = math.abs(reference - value);
            var maxDelta = math.max(delta.x, math.max(delta.y, delta.z));
            if (maxDelta > epsilon) {
                throw new AssertionException($"float3 not equal. expected {reference} got {value} (delta {maxDelta})");
            }
        }
        
        void CheckResult1(float epsilon = float.Epsilon) {
            const int i = 1;
            AssertFloat3NearOrEqual(m_Reference, m_Output[i], epsilon);
        }
        
        void CheckResult2(float epsilon = .001f) {
            const int i = 2;
            AssertFloat3NearOrEqual(m_Reference2, m_Output[i], epsilon);
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

            CheckResult1();
            CheckResult2();
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
            
            CheckResult1();
            CheckResult2();
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

            CheckResult2();
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
            
            CheckResult1(.00001f);
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
            
            CheckResult2();
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
            
            CheckResult1(.000002f);
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
            
            CheckResult2();
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
            
            CheckResult1(0.005f);
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
            
            CheckResult2();
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
            
            CheckResult1(.001f);
        }
    }

    [TestFixture]
    public class PositionSparseJobs {
        
        const int k_Length = 5_000;
        
        NativeArray<int> m_Indices;
        NativeArray<float3> m_Input;
        NativeArray<float3> m_Output;

        [SetUp]
        public void SetUpTest() {
            m_Indices = new NativeArray<int>(k_Length, Allocator.Persistent);
            m_Input = new NativeArray<float3>(k_Length, Allocator.Persistent);
            m_Output = new NativeArray<float3>(k_Length*2, Allocator.Persistent);

            for (int i = 0; i < k_Length; i++) {
                m_Indices[i] = i*2;
                m_Input[i] = new float3(i, k_Length-1, 42);
            }
        }

        [TearDown]
        public void Cleanup() {
            m_Input.Dispose();
            m_Output.Dispose();
            m_Indices.Dispose();
        }
        
        [Test, Performance]
        public unsafe void ConvertPositionsSparseJob() {
            Measure.Method(() => {
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
                    job.Run(m_Indices.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
    }

    [TestFixture]
    public class UVJobs {
        const int k_UVLength = 10_000_000;
        NativeArray<float2> m_UVInput;
        NativeArray<float2> m_UVOutput;
        
        [SetUp]
        public void SetUpTest() {
            m_UVInput = new NativeArray<float2>(k_UVLength, Allocator.Persistent);
            m_UVOutput = new NativeArray<float2>(k_UVLength, Allocator.Persistent);
        }
        
        [TearDown]
        public void Cleanup() {
            m_UVInput.Dispose();
            m_UVOutput.Dispose();
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
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertUVsUInt8ToFloatInterleavedJob {
                        input = (byte*)m_UVInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 8,
                        result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                        outputByteStride = 8
                    };
                    job.Run(m_UVOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsUInt8ToFloatInterleavedNormalizedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertUVsUInt8ToFloatInterleavedNormalizedJob {
                        input = (byte*)m_UVInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 8,
                        result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                        outputByteStride = 8
                    };
                    job.Run(m_UVOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsUInt16ToFloatInterleavedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertUVsUInt16ToFloatInterleavedJob {
                        input = (byte*)m_UVInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 8,
                        result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                        outputByteStride = 8
                    };
                    job.Run(m_UVOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsUInt16ToFloatInterleavedNormalizedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertUVsUInt16ToFloatInterleavedNormalizedJob {
                        input = (byte*)m_UVInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 8,
                        result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                        outputByteStride = 8
                    };
                    job.Run(m_UVOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsInt16ToFloatInterleavedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertUVsInt16ToFloatInterleavedJob {
                        input = (short*)m_UVInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 8,
                        result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                        outputByteStride = 8
                    };
                    job.Run(m_UVOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsInt16ToFloatInterleavedNormalizedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertUVsInt16ToFloatInterleavedNormalizedJob {
                        input = (short*)m_UVInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 8,
                        result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                        outputByteStride = 8
                    };
                    job.Run(m_UVOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsInt8ToFloatInterleavedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertUVsInt8ToFloatInterleavedJob {
                        input = (sbyte*)m_UVInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 8,
                        result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                        outputByteStride = 8
                    };
                    job.Run(m_UVOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsInt8ToFloatInterleavedNormalizedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertUVsInt8ToFloatInterleavedNormalizedJob {
                        input = (sbyte*)m_UVInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 8,
                        result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                        outputByteStride = 8
                    };
                    job.Run(m_UVOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertUVsFloatToFloatInterleavedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertUVsFloatToFloatInterleavedJob {
                        input = (byte*)m_UVInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 2,
                        result = (Vector2*)m_UVOutput.GetUnsafePtr(),
                        outputByteStride = 8
                    };
                    job.Run(m_UVOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
    }
    
    [TestFixture]
    public class Vector4Jobs {
        const int k_RotationLength = 5_000_000;
        NativeArray<quaternion> m_RotInput;
        NativeArray<quaternion> m_RotOutput;

        [SetUp]
        public void SetUpTest() {
            m_RotInput = new NativeArray<quaternion>(k_RotationLength, Allocator.Persistent);
            m_RotOutput = new NativeArray<quaternion>(k_RotationLength, Allocator.Persistent);
        }

        [TearDown]
        public void Cleanup() {
            m_RotInput.Dispose();
            m_RotOutput.Dispose();
        }
        
        [Test, Performance]
        public unsafe void ConvertRotationsFloatToFloatJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertRotationsFloatToFloatJob {
                        input = (float*)m_RotInput.GetUnsafeReadOnlyPtr(),
                        result = (float*)m_RotOutput.GetUnsafePtr()
                    };
                    job.Run(m_RotOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertRotationsInt16ToFloatJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertRotationsInt16ToFloatJob {
                        input = (short*)m_RotInput.GetUnsafeReadOnlyPtr(),
                        result = (float*)m_RotOutput.GetUnsafePtr()
                    };
                    job.Run(m_RotOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertRotationsInt8ToFloatJob() {
            var ptr = (sbyte*)m_RotInput.GetUnsafeReadOnlyPtr();
            ptr[0] = sbyte.MinValue;
            ptr[1] = -64;
            ptr[2] = 64;
            ptr[3] = sbyte.MaxValue;
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertRotationsInt8ToFloatJob {
                        input = (sbyte*)m_RotInput.GetUnsafeReadOnlyPtr(),
                        result = (float*)m_RotOutput.GetUnsafePtr()
                    };
                    job.Run(m_RotOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
            
            Assert.AreEqual( new quaternion(-1,.503937f,-.503937f,1), m_RotOutput[0] );
        }
        
        [Test, Performance]
        public unsafe void ConvertTangentsFloatToFloatInterleavedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertTangentsFloatToFloatInterleavedJob {
                        input = (byte*)m_RotInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 16,
                        result = (Vector4*)m_RotOutput.GetUnsafePtr(),
                        outputByteStride = 16
                    };
                    job.Run(m_RotOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertBoneWeightsFloatToFloatInterleavedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertBoneWeightsFloatToFloatInterleavedJob {
                        input = (byte*)m_RotInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 16,
                        result = (Vector4*)m_RotOutput.GetUnsafePtr(),
                        outputByteStride = 16
                    };
                    job.Run(m_RotOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertTangentsInt16ToFloatInterleavedNormalizedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertTangentsInt16ToFloatInterleavedNormalizedJob {
                        input = (short*)m_RotInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 16,
                        result = (Vector4*)m_RotOutput.GetUnsafePtr(),
                        outputByteStride = 16
                    };
                    job.Run(m_RotOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertTangentsInt8ToFloatInterleavedNormalizedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertTangentsInt8ToFloatInterleavedNormalizedJob {
                        input = (sbyte*)m_RotInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 16,
                        result = (Vector4*)m_RotOutput.GetUnsafePtr(),
                        outputByteStride = 16
                    };
                    job.Run(m_RotOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
    }
    
    [TestFixture]
    public class ColorJobs {
        const int k_ColorLength = 3_000_000;
        NativeArray<Color> m_ColorInput;
        NativeArray<Color> m_ColorOutput;

        [SetUp]
        public void SetUpTest() {
            m_ColorInput = new NativeArray<Color>(k_ColorLength, Allocator.Persistent);
            m_ColorOutput = new NativeArray<Color>(k_ColorLength, Allocator.Persistent);
        }

        [TearDown]
        public void Cleanup() {
            m_ColorInput.Dispose();
            m_ColorOutput.Dispose();
        }
        
        [Test, Performance]
        public unsafe void ConvertColorsRGBFloatToRGBAFloatJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertColorsRGBFloatToRGBAFloatJob {
                        input = (float*)m_ColorInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 8,
                        result = m_ColorOutput
                    };
                    job.Run(m_ColorOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertColorsRGBUInt8ToRGBAFloatJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertColorsRGBUInt8ToRGBAFloatJob {
                        input = (byte*)m_ColorInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 8,
                        result = m_ColorOutput
                    };
                    job.Run(m_ColorOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertColorsRGBUInt16ToRGBAFloatJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertColorsRGBUInt16ToRGBAFloatJob {
                        input = (ushort*)m_ColorInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 8,
                        result = m_ColorOutput
                    };
                    job.Run(m_ColorOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertColorsInterleavedRGBAFloatToRGBAFloatJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertColorsInterleavedRGBAFloatToRGBAFloatJob {
                        input = (float*)m_ColorInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 16,
                        result = m_ColorOutput
                    };
                    job.Run(m_ColorOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertColorsInterleavedRGBAUInt16ToRGBAFloatJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertColorsInterleavedRGBAUInt16ToRGBAFloatJob {
                        input = (ushort*)m_ColorInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 8,
                        result = m_ColorOutput
                    };
                    job.Run(m_ColorOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertColorsRGBAUInt8ToRGBAFloatJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertColorsRGBAUInt8ToRGBAFloatJob {
                        input = (byte*)m_ColorInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 8,
                        result = m_ColorOutput
                    };
                    job.Run(m_ColorOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
    }
    
    [TestFixture]
    public class BoneIndexJobs {
        const int k_BoneIndexLength = 2_000_000;
        NativeArray<uint4> m_BoneIndexInput;
        NativeArray<uint4> m_BoneIndexOutput;

        [SetUp]
        public void SetUpTest() {
            m_BoneIndexInput = new NativeArray<uint4>(k_BoneIndexLength, Allocator.Persistent);
            m_BoneIndexOutput = new NativeArray<uint4>(k_BoneIndexLength, Allocator.Persistent);
        }

        [TearDown]
        public void Cleanup() {
            m_BoneIndexInput.Dispose();
            m_BoneIndexOutput.Dispose();
        }
        
        [Test, Performance]
        public unsafe void ConvertBoneJointsUInt8ToUInt32Job() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertBoneJointsUInt8ToUInt32Job {
                        input = (byte*)m_BoneIndexInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 16,
                        result = (uint*)m_BoneIndexOutput.GetUnsafePtr(),
                        outputByteStride = 16
                    };
                    job.Run(m_BoneIndexOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertBoneJointsUInt16ToUInt32Job() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertBoneJointsUInt16ToUInt32Job {
                        input = (byte*)m_BoneIndexInput.GetUnsafeReadOnlyPtr(),
                        inputByteStride = 16,
                        result = (uint*)m_BoneIndexOutput.GetUnsafePtr(),
                        outputByteStride = 16
                    };
                    job.Run(m_BoneIndexOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
    }
    
    [TestFixture]
    public class MatrixJobs {
        const int k_MatrixLength = 800_000;
        NativeArray<float4x4> m_MatrixInput;
        NativeArray<Matrix4x4> m_MatrixOutput;

        [SetUp]
        public void SetUpTest() {
            m_MatrixInput = new NativeArray<float4x4>(k_MatrixLength, Allocator.Persistent);
            m_MatrixOutput = new NativeArray<Matrix4x4>(k_MatrixLength, Allocator.Persistent);
        }

        [TearDown]
        public void Cleanup() {
            m_MatrixInput.Dispose();
            m_MatrixOutput.Dispose();
        }
        
        [Test, Performance]
        public unsafe void ConvertMatricesJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertMatricesJob {
                        input = (Matrix4x4*)m_MatrixInput.GetUnsafeReadOnlyPtr(),
                        result = m_MatrixOutput,
                    };
                    job.Run(m_MatrixOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
    }
    
    [TestFixture]
    public class IndexJobs {
        const int k_IndexLength = 24_000_000; // multiple of 3!
        NativeArray<int> m_IndexInput;
        NativeArray<int> m_IndexOutput;

        [SetUp]
        public void SetUpTest() {
            m_IndexInput = new NativeArray<int>(k_IndexLength, Allocator.Persistent);
            m_IndexOutput = new NativeArray<int>(k_IndexLength, Allocator.Persistent);
        }

        [TearDown]
        public void Cleanup() {
            m_IndexInput.Dispose();
            m_IndexOutput.Dispose();
        }
        
        [Test, Performance]
        public unsafe void CreateIndicesInt32Job() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.CreateIndicesInt32Job {
                        result = (int*)m_IndexOutput.GetUnsafePtr()
                    };
                    job.Run(m_IndexOutput.Length/3);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void CreateIndicesInt32FlippedJob() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.CreateIndicesInt32FlippedJob {
                        result = (int*)m_IndexOutput.GetUnsafePtr()
                    };
                    job.Run(m_IndexOutput.Length/3);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertIndicesUInt8ToInt32Job() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertIndicesUInt8ToInt32Job {
                        input = (byte*)m_IndexInput.GetUnsafeReadOnlyPtr(),
                        result = (int*)m_IndexOutput.GetUnsafePtr()
                    };
                    job.Run(m_IndexOutput.Length/3);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertIndicesUInt8ToInt32FlippedJob() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertIndicesUInt8ToInt32FlippedJob {
                        input = (byte*)m_IndexInput.GetUnsafeReadOnlyPtr(),
                        result = (int*)m_IndexOutput.GetUnsafePtr()
                    };
                    job.Run(m_IndexOutput.Length/3);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertIndicesUInt16ToInt32FlippedJob() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertIndicesUInt16ToInt32FlippedJob {
                        input = (ushort*)m_IndexInput.GetUnsafeReadOnlyPtr(),
                        result = (int*)m_IndexOutput.GetUnsafePtr()
                    };
                    job.Run(m_IndexOutput.Length/3);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertIndicesUInt16ToInt32Job() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertIndicesUInt16ToInt32Job {
                        input = (ushort*)m_IndexInput.GetUnsafeReadOnlyPtr(),
                        result = (int*)m_IndexOutput.GetUnsafePtr()
                    };
                    job.Run(m_IndexOutput.Length/3);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertIndicesUInt32ToInt32Job() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertIndicesUInt32ToInt32Job {
                        input = (uint*)m_IndexInput.GetUnsafeReadOnlyPtr(),
                        result = (int*)m_IndexOutput.GetUnsafePtr()
                    };
                    job.Run(m_IndexOutput.Length/3);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertIndicesUInt32ToInt32FlippedJob() {
            Assert.IsTrue( m_IndexOutput.Length % 3 == 0 );
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertIndicesUInt32ToInt32FlippedJob {
                        input = (uint*)m_IndexInput.GetUnsafeReadOnlyPtr(),
                        result = (int*)m_IndexOutput.GetUnsafePtr()
                    };
                    job.Run(m_IndexOutput.Length/3);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
    }
    
    [TestFixture]
    public class ScalarJobs {
        const int k_ScalarLength = 5_000_000;
        NativeArray<float> m_ScalarInput;
        NativeArray<float> m_ScalarOutput;

        [SetUp]
        public void SetUpTest() {
            m_ScalarInput = new NativeArray<float>(k_ScalarLength, Allocator.Persistent);
            m_ScalarOutput = new NativeArray<float>(k_ScalarLength, Allocator.Persistent);
        }
        
        [TearDown]
        public void Cleanup() {
            m_ScalarInput.Dispose();
            m_ScalarOutput.Dispose();
        }
        
        [Test, Performance]
        public unsafe void ConvertScalarInt8ToFloatNormalizedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertScalarInt8ToFloatNormalizedJob {
                        input = (sbyte*)m_ScalarInput.GetUnsafeReadOnlyPtr(),
                        result = m_ScalarOutput,
                    };
                    job.Run(m_ScalarOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertScalarUInt8ToFloatNormalizedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertScalarUInt8ToFloatNormalizedJob {
                        input = (byte*)m_ScalarInput.GetUnsafeReadOnlyPtr(),
                        result = m_ScalarOutput,
                    };
                    job.Run(m_ScalarOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertScalarInt16ToFloatNormalizedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertScalarInt16ToFloatNormalizedJob {
                        input = (short*)m_ScalarInput.GetUnsafeReadOnlyPtr(),
                        result = m_ScalarOutput,
                    };
                    job.Run(m_ScalarOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
        
        [Test, Performance]
        public unsafe void ConvertScalarUInt16ToFloatNormalizedJob() {
            Measure.Method(() => {
                    var job = new GLTFast.Jobs.ConvertScalarUInt16ToFloatNormalizedJob {
                        input = (ushort*)m_ScalarInput.GetUnsafeReadOnlyPtr(),
                        result = m_ScalarOutput,
                    };
                    job.Run(m_ScalarOutput.Length);
                })
                .WarmupCount(1)
                .MeasurementCount(Constants.measureCount)
                .IterationsPerMeasurement(Constants.iterationsPerMeasurement)
                .Run();
        }
    }
}