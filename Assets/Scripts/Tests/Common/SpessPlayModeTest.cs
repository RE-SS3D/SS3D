using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{

    public abstract class SpessPlayModeTest : SpessTest
    {
        // When overriding, use:
        //     yield return base.UnitySetUp();
        [UnitySetUp]
        public virtual IEnumerator UnitySetUp()
        {
            base.SetUp();
            yield return null;
        }

        // When overriding, use:
        //     yield return base.UnityTearDown();
        [UnityTearDown]
        public virtual IEnumerator UnityTearDown()
        {
            base.TearDown();
            yield return null;
        }

    }
}