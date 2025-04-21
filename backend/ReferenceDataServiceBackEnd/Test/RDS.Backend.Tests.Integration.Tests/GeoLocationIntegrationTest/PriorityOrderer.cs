using Xunit.Abstractions;
using Xunit.Sdk;

namespace RDS.Backend.Tests.IntegrationTests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TestPriorityAttribute : Attribute
    {
        public int Priority { get; }

        public TestPriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }

    public class PriorityOrderer : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(
            IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
        {
            var sorted = new SortedDictionary<int, List<TTestCase>>();

            foreach (var testCase in testCases)
            {
                int priority = 0;

                foreach (var attr in testCase.TestMethod.Method
                             .GetCustomAttributes(typeof(TestPriorityAttribute).AssemblyQualifiedName))
                {
                    priority = attr.GetNamedArgument<int>("Priority");
                }

                if (!sorted.TryGetValue(priority, out var list))
                {
                    list = new List<TTestCase>();
                    sorted.Add(priority, list);
                }

                list.Add(testCase);
            }

            return sorted.SelectMany(x => x.Value);
        }
    }
}
