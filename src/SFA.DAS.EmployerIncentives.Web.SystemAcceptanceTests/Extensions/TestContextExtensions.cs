using SFA.DAS.EmployerIncentives.Web.SystemAcceptanceTests.Services;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Web.SystemAcceptanceTests.Extensions
{
    public static class TestContextExtensions
    {
        public static Task<WaitForResult> WaitFor<T>(
                    this TestContext context,
                    Func<Task> func,
                    bool assertOnTimeout = true,
                    bool assertOnError = false,
                    int timeoutInMs = 15000)
        {
            return new TestHelper(context)
                .WaitFor<T>(func, assertOnTimeout: assertOnTimeout, assertOnError: assertOnError, timeoutInMs: timeoutInMs);
        }     
    }
}
