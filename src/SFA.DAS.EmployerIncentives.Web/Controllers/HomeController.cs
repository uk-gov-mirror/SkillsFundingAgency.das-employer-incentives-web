﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.EmployerIncentives.Web.Infrastructure;
using SFA.DAS.EmployerIncentives.Web.ViewModels.Home;

namespace SFA.DAS.EmployerIncentives.Web.Controllers
{
    [Authorize(Policy = nameof(PolicyNames.HasEmployerAccount))]
    [Route("/")]
    public class HomeController : Controller
    {
        [Route("")]
        public IActionResult Home()
        {
            var accountId = "";
            return View(new HomeViewModel(accountId));
        }
    }
}