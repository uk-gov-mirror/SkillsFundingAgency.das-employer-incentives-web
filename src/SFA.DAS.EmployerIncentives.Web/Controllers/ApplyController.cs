﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SFA.DAS.EmployerIncentives.Web.Infrastructure.Configuration;
using SFA.DAS.EmployerIncentives.Web.Models;
using SFA.DAS.EmployerIncentives.Web.Services.Apprentices.Types;
using SFA.DAS.EmployerIncentives.Web.Services.LegalEntities;
using SFA.DAS.EmployerIncentives.Web.ViewModels.Apply;
using SFA.DAS.HashingService;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
namespace SFA.DAS.EmployerIncentives.Web.Controllers
{
    [Route("{hashedAccountId}/apply")]
    public class ApplyController : Controller
    {
        private readonly WebConfigurationOptions _configuration;
        private readonly ILegalEntitiesService _legalEntitiesService;
        private readonly IApprenticesService _apprenticesService;
        private readonly IHashingService _hashingService;

        public ApplyController(
            IOptions<WebConfigurationOptions> configuration,
            ILegalEntitiesService legalEntitiesService,
            IApprenticesService apprenticesService,
            IHashingService hashingService)
        {
            _configuration = configuration.Value;
            _legalEntitiesService = legalEntitiesService;
            _apprenticesService = apprenticesService;
            _hashingService = hashingService;
        }

        [Route("")]
        [HttpGet]
        public async Task<IActionResult> QualificationQuestion()
        {
            return View(new QualificationQuestionViewModel());
        }

        [Route("")]
        [HttpPost]
        public async Task<IActionResult> QualificationQuestion(string hashedAccountId, QualificationQuestionViewModel viewModel)
        {
            if (!viewModel.HasTakenOnNewApprentices.HasValue)
            {
                ModelState.AddModelError("HasTakenOnNewApprentices", QualificationQuestionViewModel.HasTakenOnNewApprenticesNotSelectedMessage);
                return View(viewModel);
            }

            if (viewModel.HasTakenOnNewApprentices.Value)
            {
                var accountId = _hashingService.DecodeValue(hashedAccountId);
                var legalEntities = await _legalEntitiesService.Get(accountId);
                if (legalEntities.Count() == 1)
                {
                    var apprentices = await _apprenticesService.Get(new ApprenticesQuery(accountId, legalEntities.First().AccountLegalEntityId));
                    if (apprentices.Any())
                    {
                        var hashedAccountLegalEntityId = _hashingService.HashValue(legalEntities.First().AccountLegalEntityId);
                        return RedirectToAction("SelectApprenticeships", new { hashedAccountId, hashedAccountLegalEntityId });
                    }
                    else
                    {
                        return RedirectToAction("CannotApply", new { hashedAccountId, hasTakenOnNewApprentices = true });
                    }
                }
                if (legalEntities.Count() > 1)
                {
                    return RedirectToAction("ChooseOrganisation", new { hashedAccountId });
                }
            }

            return RedirectToAction("CannotApply", new { hashedAccountId });
        }

        [HttpGet]
        [Route("{hashedAccountLegalEntityId}/select-new-apprentices")]
        public async Task<ViewResult> SelectApprenticeships(string hashedAccountId, string hashedAccountLegalEntityId)
        {
            var model = await GetInitialSelectApprenticeshipsViewModel(hashedAccountId, hashedAccountLegalEntityId);

            return View(model);
        }

        [HttpPost]
        [Route("{hashedAccountLegalEntityId}/select-new-apprentices")]
        public async Task<IActionResult> SelectApprenticeships(string hashedAccountId, string hashedAccountLegalEntityId, [FromBody] SelectApprenticeshipsViewModel viewModel)
        {
            if (viewModel.HasSelectedApprenticeships)
            {
                return RedirectToAction("Declaration", new { hashedAccountId });
            }

            ModelState.AddModelError(viewModel.FirstCheckboxId, SelectApprenticeshipsViewModel.SelectApprenticeshipsMessage);
            return View(viewModel);
        }

        private async Task<SelectApprenticeshipsViewModel> GetInitialSelectApprenticeshipsViewModel(string hashedAccountId, string hashedAccountLegalEntityId)
        {
            var query = new ApprenticesQuery(_hashingService.DecodeValue(hashedAccountId),
                _hashingService.DecodeValue(hashedAccountLegalEntityId));
            var apprenticeships = await _apprenticesService.Get(query);

            var model = new SelectApprenticeshipsViewModel
            {
                AccountId = hashedAccountId,
                AccountLegalEntityId = hashedAccountLegalEntityId,
                Apprenticeships = apprenticeships.ToApprenticeshipModel(_hashingService).OrderBy(a => a.LastName)
            };
            return model;
        }


        [HttpGet]
        [Route("declaration")]
        public async Task<ViewResult> Declaration(string accountId)
        {
            return View(new DeclarationViewModel(accountId));
        }

        [HttpGet]
        [Route("cannot-apply")]
        public async Task<ViewResult> CannotApply(bool hasTakenOnNewApprentices = false)
        {
            if (hasTakenOnNewApprentices)
            {
                return View(new TakenOnCannotApplyViewModel(_configuration.CommitmentsBaseUrl));
            }
            return View(new CannotApplyViewModel(_configuration.CommitmentsBaseUrl));
        }

        [HttpGet]
        [Route("choose-organisation")]
        public async Task<ViewResult> ChooseOrganisation()
        {
            return View(new ChooseOrganisationViewModel());
        }

        [HttpGet]
        [Route("need-bank-details")]
        public async Task<ViewResult> BankDetailsConfirmation()
        {
            return View(new BankDetailsConfirmationViewModel());
        }

        [HttpPost]
        [Route("need-bank-details")]
        public async Task<IActionResult> BankDetailsConfirmation(string hashedAccountId, BankDetailsConfirmationViewModel viewModel)
        {
            if (!viewModel.CanProvideBankDetails.HasValue)
            {
                ModelState.AddModelError("CanProvideBankDetails", BankDetailsConfirmationViewModel.CanProvideBankDetailsNotSelectedMessage);
                return View(viewModel);
            }

            if (viewModel.CanProvideBankDetails.Value)
            {
                // redirect to business central
                return RedirectToAction("EnterBankDetails");
            }

            // redirect to need bank details page
            return RedirectToAction("NeedBankDetails");
        }


        [HttpGet]
        [Route("enter-bank-details")]
        public async Task<ViewResult> EnterBankDetails()
        {
            // Once integration mechanism is finalised, redirect / post to external site
            return View();
        }

        [HttpGet]
        [Route("complete/need-bank-details")]
        public async Task<ViewResult> NeedBankDetails()
        {
            return View();
        }
    }
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously