using UnityEngine;
using UnityEngine.TestTools;

namespace GLTFTest.Editor {
    
    // Work around case #1033694, unable to use PrebuildSetup types directly from assemblies that don't have special names.
    // Once that's fixed, this class can be deleted and the SetupGraphicsTestCases class in Unity.TestFramework.Graphics.Editor
    // can be used directly instead.
    public class SetupGraphicsTestCases : IPrebuildSetup
    {
        public void Setup() {
#if GRAPHICS_TESTS
            UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup(RenderTests.universalPackagePath);
#else
            Debug.LogWarning("Graphics Tests Framework not installed!");
#endif
        }
    }
}