using Core.ExtensionClasses;
using NUnit.Framework;

namespace Core.Test.ExtensionClasses
{
    public class StringExtension_Test
    {
        [TestCase("#", 6, "######")]
        [TestCase("..", 3, "......")]
        public void Test_Repeat_RepeatsCorrectAmount(string shouldRepeat, int times, string resultsIn)
        {
            Assert.AreEqual(resultsIn, shouldRepeat.Repeat(times), $"The string '{shouldRepeat}' was not repeated {times} times.");
        }
    }
}