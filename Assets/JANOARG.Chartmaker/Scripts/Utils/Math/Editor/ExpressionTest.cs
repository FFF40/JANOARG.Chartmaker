using System;
using UnityEngine.Assertions;

namespace JANOARG.Chartmaker.Utils.Math
{
    internal static class ExpressionTest
    {
        static void Test(string expression, float expected)
        {
            Assert.AreApproximatelyEqual(ExpressionUtils.Evaluate<float>(expression), expected);
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void TestAll()
        {
            Test("1000", 1000);
            Test("1-2-3-4", -8);
        }
    }
}