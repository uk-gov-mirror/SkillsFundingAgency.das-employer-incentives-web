﻿using AngleSharp.Html.Parser;
using FluentAssertions;
using Newtonsoft.Json;
using SFA.DAS.EmployerIncentives.Web.Models;
using SFA.DAS.EmployerIncentives.Web.Services.LegalEntities.Types;
using SFA.DAS.EmployerIncentives.Web.SystemAcceptanceTests.Extensions;
using SFA.DAS.EmployerIncentives.Web.ViewModels.Apply;
using SFA.DAS.HashingService;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using SFA.DAS.EmployerIncentives.Web.ViewModels.Apply.SelectApprenticeships;
using TechTalk.SpecFlow;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace SFA.DAS.EmployerIncentives.Web.SystemAcceptanceTests.Steps.Application
{
    [Binding]
    [Scope(Feature = "ApprenticeSelection")]
    public class ApprenticeSelectionSteps : StepsBase
    {
        private readonly TestContext _testContext;
        private readonly TestDataStore _testData;
        private readonly IHashingService _hashingService;
        private HttpResponseMessage _continueNavigationResponse;
        private List<ApprenticeDto> _apprenticeshipData;

        public ApprenticeSelectionSteps(TestContext testContext) : base(testContext)
        {
            _testContext = testContext;
            _testData = _testContext.TestDataStore;
            _hashingService = _testContext.HashingService;
        }

        [Given(@"an employer applying for a grant has apprentices matching the eligibility requirement")]
        public void GivenAnEmployerApplyingForAGrantHasApprenticesMatchingTheEligibilityRequirement()
        {
            var data = new TestData.Account.WithSingleLegalEntityWithEligibleApprenticeships();
            _apprenticeshipData = data.Apprentices;

            var accountId = _testData.GetOrCreate("AccountId", onCreate: () => data.AccountId);
            _testData.Add("HashedAccountId", _hashingService.HashValue(accountId));
            var accountLegalEntityId = _testData.GetOrCreate("AccountLegalEntityId", onCreate: () => data.AccountLegalEntityId);
            _testData.Add("HashedAccountLegalEntityId", _hashingService.HashValue(accountLegalEntityId));

            _testContext.EmployerIncentivesApi.MockServer
                .Given(
                    Request
                        .Create()
                        .WithPath($"/apprenticeships")
                        .WithParam("accountid", accountId.ToString())
                        .WithParam("accountlegalentityid", accountLegalEntityId.ToString())
                        .UsingGet()
                )
                .RespondWith(
                    Response.Create()
                        .WithBody(JsonConvert.SerializeObject(_apprenticeshipData))
                        .WithStatusCode(HttpStatusCode.OK));

            _testContext.EmployerIncentivesApi.MockServer
                .Given(
                    Request
                        .Create()
                        .WithPath($"/accounts/{accountId}/applications")
                        .UsingPost()
                )
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(HttpStatusCode.Created));
        }

        [When(@"the employer selects the apprentice the grant applies to")]
        public async Task WhenTheEmployerSelectsTheApprenticeTheGrantAppliesTo()
        {
            var apprenticeships = _apprenticeshipData.ToApprenticeshipModel(_hashingService).ToArray();
            var hashedAccountId = _testData.Get<string>("HashedAccountId");
            var hashedLegalEntityId = _testData.Get<string>("HashedAccountLegalEntityId");

            var url = $"{hashedAccountId}/apply/{hashedLegalEntityId}/select-new-apprentices";
            var form = new KeyValuePair<string, string>("SelectedApprenticeships", apprenticeships.First().Id);

            _continueNavigationResponse = await _testContext.WebsiteClient.PostFormAsync(url, form);
            _continueNavigationResponse.EnsureSuccessStatusCode();
        }

        [When(@"the employer doesn't select any apprentice the grant applies to")]
        public async Task WhenTheEmployerDoesnTSelectAnyApprenticeTheGrantAppliesTo()
        {
            var hashedAccountId = _testData.Get<string>("HashedAccountId");
            var hashedLegalEntityId = _testData.Get<string>("HashedAccountLegalEntityId");

            var url = $"{hashedAccountId}/apply/{hashedLegalEntityId}/select-new-apprentices";

            _continueNavigationResponse = await _testContext.WebsiteClient.PostFormAsync(url);
            _continueNavigationResponse.EnsureSuccessStatusCode();
        }

        [Then(@"the employer is asked to confirm the selected apprentices")]
        public async Task ThenTheEmployerIsAskedToConfirmTheSelectedApprentices()
        {
            var hashedAccountId = _testData.Get<string>("HashedAccountId");
            _continueNavigationResponse.RequestMessage.RequestUri.PathAndQuery.Should().StartWith($"/{hashedAccountId}/apply/confirm-apprentices/");
        }

        [Then(@"the employer is informed that they haven't selected an apprentice")]
        public async Task ThenTheEmployerIsInformedThatTheyHavenTSelectedAnApprentice()
        {
            var parser = new HtmlParser();
            var document = parser.ParseDocument(await _continueNavigationResponse.Content.ReadAsStreamAsync());

            document.Title.Should().Be(SelectApprenticeshipsViewModel.SelectApprenticeshipsMessage);

            var hashedAccountId = _testData.Get<string>("HashedAccountId");
            var hashedLegalEntityId = _testData.Get<string>("HashedAccountLegalEntityId");

            var url = $"/{hashedAccountId}/apply/{hashedLegalEntityId}/select-new-apprentices";

            _continueNavigationResponse.RequestMessage.RequestUri.PathAndQuery.Should().Be(url);

            var viewResult = _testContext.ActionResult.LastViewResult;
            viewResult.Should().NotBeNull();
            var model = viewResult.Model as SelectApprenticeshipsViewModel;
            model.Should().NotBeNull();
            model.Should().HaveTitle(SelectApprenticeshipsViewModel.SelectApprenticeshipsMessage);
            model.Apprenticeships.Count().Should().Be(_apprenticeshipData.Count);
            model.AccountId.Should().Be(hashedAccountId);
            viewResult.Should().ContainError(model.FirstCheckboxId, model.Title);
        }
    }
}
