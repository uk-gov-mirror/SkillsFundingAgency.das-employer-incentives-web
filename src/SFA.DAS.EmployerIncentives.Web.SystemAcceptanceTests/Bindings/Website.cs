﻿using SFA.DAS.EmployerIncentives.Web.SystemAcceptanceTests.Services;
using SFA.DAS.HashingService;
using TechTalk.SpecFlow;

namespace SFA.DAS.EmployerIncentives.Web.SystemAcceptanceTests.Bindings
{
    [Binding]
    public class Website
    {
        private readonly TestContext _context;
        public Website(TestContext context)
        {
            _context = context;           
        }

        [BeforeScenario()]
        public void InitialiseFunctions()
        {
            _context.Website = new TestWebsite(_context.EmployerIncentivesApi);
            _context.WebsiteClient = _context.Website.CreateClient();
            _context.HashingService = _context.Website.Services.GetService(typeof(IHashingService)) as IHashingService;
        }
    }
}