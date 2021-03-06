using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SFA.DAS.EmployerIncentives.Web.SystemAcceptanceTests.Hooks;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Web.SystemAcceptanceTests.Services
{
    public class TestActionResultFilter : IAsyncAlwaysRunResultFilter
    {
        private readonly IHook<IActionResult> _actionResultHook;

        public TestActionResultFilter(IHook<IActionResult> actionResultHook)
        {
            _actionResultHook = actionResultHook;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (_actionResultHook != null)
            {
                try
                {
                    if (_actionResultHook?.OnReceived != null)
                    {
                        _actionResultHook.OnReceived(context.Result);
                    }
                    await next();
                    if (_actionResultHook?.OnProcessed != null)
                    {
                        _actionResultHook.OnProcessed(context.Result);
                    }
                }
                catch (Exception ex)
                {
                    if (_actionResultHook?.OnErrored != null)
                    {
                        _actionResultHook.OnErrored(ex, context.Result);
                    }
                    throw;
                }
            }
            else
            {
                await next();
            }
        }
    }
}
