﻿using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SFA.DAS.EmployerIncentives.Web.Infrastructure.Configuration;
using SFA.DAS.EmployerIncentives.Web.Services.Apprentices.Types;
using SFA.DAS.EmployerIncentives.Web.Services.LegalEntities;
using SFA.DAS.EmployerIncentives.Web.ViewModels;
using SFA.DAS.EmployerIncentives.Web.ViewModels.Apply;
using SFA.DAS.EmployerIncentives.Web.ViewModels.Apply.SelectApprenticeships;
using System.Linq;
using System.Threading.Tasks;
using SFA.DAS.EmployerIncentives.Web.Services.Applications;
using SFA.DAS.EmployerIncentives.Web.Services.Apprentices;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
namespace SFA.DAS.EmployerIncentives.Web.Controllers
{
    [Route("{accountId}/apply")]
    public class ApplyController : Controller
    {
        private readonly WebConfigurationOptions _configuration;
        private readonly ILegalEntitiesService _legalEntitiesService;
        private readonly IApprenticesService _apprenticesService;
        private readonly IApplicationService _applicationService;

        public ApplyController(
            IOptions<WebConfigurationOptions> configuration,
            ILegalEntitiesService legalEntitiesService,
            IApprenticesService apprenticesService,
            IApplicationService applicationService)
        {
            _configuration = configuration.Value;
            _legalEntitiesService = legalEntitiesService;
            _apprenticesService = apprenticesService;
            _applicationService = applicationService;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Default()
        {
            return RedirectToAction("GetChooseOrganisation");
        }

        [HttpGet]
        [Route("declaration")]
        public async Task<ViewResult> Declaration(string accountId)
        {
            return View(new DeclarationViewModel(accountId));
        }

        [Route("{accountLegalEntityId}/taken-on-new-apprentices")]
        [HttpGet]
        public async Task<IActionResult> GetQualificationQuestion(QualificationQuestionViewModel viewModel)
        {
            return View("QualificationQuestion", viewModel);
        }

        [Route("{accountLegalEntityId}/taken-on-new-apprentices")]
        [HttpPost]
        public async Task<IActionResult> QualificationQuestion(QualificationQuestionViewModel viewModel)
        {
            if (!viewModel.HasTakenOnNewApprentices.HasValue)
            {
                ModelState.AddModelError("HasTakenOnNewApprentices", QualificationQuestionViewModel.HasTakenOnNewApprenticesNotSelectedMessage);
                return View(viewModel);
            }

            if (viewModel.HasTakenOnNewApprentices.Value)
            {
                return RedirectToAction("SelectApprenticeships", new { viewModel.AccountId, accountLegalEntityId = viewModel.AccountLegalEntityId });
            }

            return RedirectToAction("CannotApply", new { viewModel.AccountId });
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
        public async Task<IActionResult> GetChooseOrganisation(ChooseOrganisationViewModel viewModel)
        {
            var legalEntities = await _legalEntitiesService.Get(viewModel.AccountId);
            if (legalEntities.Count() == 1)
            {
                var accountLegalEntityId = legalEntities.First().AccountLegalEntityId;
                var apprentices = await _apprenticesService.Get(new ApprenticesQuery(viewModel.AccountId, accountLegalEntityId));
                if (apprentices.Any())
                {
                    return RedirectToAction("GetQualificationQuestion", new { viewModel.AccountId, accountLegalEntityId });
                }
                else
                {
                    return RedirectToAction("CannotApply", new { viewModel.AccountId, hasTakenOnNewApprentices = true });
                }
            }
            if (legalEntities.Count() > 1)
            {
                viewModel.AddOrganisations(legalEntities);
                return View("ChooseOrganisation", viewModel);
            }

            return RedirectToAction("CannotApply", new { viewModel.AccountId });
        }

        [HttpPost]
        [Route("choose-organisation")]
        public async Task<IActionResult> ChooseOrganisation(ChooseOrganisationViewModel viewModel)
        {
            if (!string.IsNullOrEmpty(viewModel.Selected))
            {
                return RedirectToAction("GetQualificationQuestion", new { viewModel.AccountId, accountLegalEntityId = viewModel.Selected });
            }

            if (string.IsNullOrEmpty(viewModel.Selected))
            {
                ModelState.AddModelError("OrganisationNotSelected", viewModel.OrganisationNotSelectedMessage);
            }

            viewModel.AddOrganisations(await _legalEntitiesService.Get(viewModel.AccountId));

            return View(viewModel);
        }

        [HttpGet]
        [Route("{accountLegalEntityId}/select-new-apprentices")]
        public async Task<ViewResult> SelectApprenticeships(string accountId, string accountLegalEntityId)
        {
            var model = await GetInitialSelectApprenticeshipsViewModel(accountId, accountLegalEntityId);

            return View(model);
        }

        [HttpPost]
        [Route("{accountLegalEntityId}/select-new-apprentices")]
        public async Task<IActionResult> SelectApprenticeships(SelectApprenticeshipsRequest form)
        {
            if (form.HasSelectedApprenticeships)
            {
                var applicationId = await _applicationService.Post(form.AccountId, form.AccountLegalEntityId, form.SelectedApprenticeships);
                return RedirectToAction("ConfirmApprenticeships", new { form.AccountId, applicationId  });
            }

            var viewModel = await GetInitialSelectApprenticeshipsViewModel(form.AccountId, form.AccountLegalEntityId);
            ModelState.AddModelError(viewModel.FirstCheckboxId, SelectApprenticeshipsViewModel.SelectApprenticeshipsMessage);

            return View(viewModel);
        }

        [HttpGet]
        [Route("confirm-apprentices/{applicationId}")]
        public async Task<ViewResult> ConfirmApprenticeships(string accountId, Guid applicationId)
        {
            var model = await _applicationService.Get(accountId, applicationId);
            return View(model);
        }

        private async Task<SelectApprenticeshipsViewModel> GetInitialSelectApprenticeshipsViewModel(string accountId, string accountLegalEntityId)
        {
            var apprenticeships = await _apprenticesService.Get(new ApprenticesQuery(accountId, accountLegalEntityId));

            return new SelectApprenticeshipsViewModel
            {
                AccountId = accountId,
                AccountLegalEntityId = accountLegalEntityId,
                Apprenticeships = apprenticeships.OrderBy(a => a.LastName)
            };
        }
    }
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously