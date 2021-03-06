using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Web.Infrastructure.Configuration;
using SFA.DAS.EmployerIncentives.Web.Models;
using SFA.DAS.EmployerIncentives.Web.Services.Applications;
using SFA.DAS.EmployerIncentives.Web.Services.Email;
using SFA.DAS.EmployerIncentives.Web.Services.LegalEntities;
using SFA.DAS.EmployerIncentives.Web.ViewModels.Apply;
using SFA.DAS.HashingService;

namespace SFA.DAS.EmployerIncentives.Web.Tests.Controllers.BankDetailsController
{
    [TestFixture]
    public class WhenBankDetailsAreRequired
    {
        private Mock<IVerificationService> _verificationService;
        private Mock<IEmailService> _emailService;
        private Mock<IApplicationService> _applicationService;
        private Mock<IHashingService> _hashingService;
        private Mock<ILegalEntitiesService> _legalEntitiesService;
        private Mock<IOptions<ExternalLinksConfiguration>> _configuration;
        private Fixture _fixture;
        private Web.Controllers.BankDetailsController _sut;
        private string _accountId;
        private Guid _applicationId;

        [SetUp]
        public void Setup()
        {
            _verificationService = new Mock<IVerificationService>();
            _emailService = new Mock<IEmailService>();
            _applicationService = new Mock<IApplicationService>();
            _hashingService = new Mock<IHashingService>();
            _legalEntitiesService = new Mock<ILegalEntitiesService>();
            _configuration = new Mock<IOptions<ExternalLinksConfiguration>>();
            _fixture = new Fixture();
            var config = _fixture.Create<ExternalLinksConfiguration>();
            _configuration.Setup(x => x.Value).Returns(config);
            _accountId = _fixture.Create<string>();
            _applicationId = Guid.NewGuid();

            _sut = new Web.Controllers.BankDetailsController(_verificationService.Object, _emailService.Object,
                _applicationService.Object, _hashingService.Object, _legalEntitiesService.Object,
                _configuration.Object);
        }

        [Test]
        public async Task Then_the_user_is_asked_to_confirm_they_can_provide_bank_details()
        {
            // Arrange
            var application = new ApplicationModel(_applicationId, _accountId, _fixture.Create<string>(),
                _fixture.CreateMany<ApplicationApprenticeshipModel>(1),
                bankDetailsRequired: true,
                newAgreementRequired: false);

            _applicationService.Setup(x => x.Get(_accountId, _applicationId, false)).ReturnsAsync(application);
            
            var legalEntity = _fixture.Create<LegalEntityModel>();
            _legalEntitiesService.Setup(x => x.Get(_accountId, application.AccountLegalEntityId)).ReturnsAsync(legalEntity);

            // Act
            var viewResult = await _sut.BankDetailsConfirmation(_accountId, _applicationId) as ViewResult;

            // Assert
            viewResult.Should().NotBeNull();
            var model = viewResult.Model as BankDetailsConfirmationViewModel;
            model.Should().NotBeNull();
            model.AccountId.Should().Be(_accountId);
            model.ApplicationId.Should().Be(_applicationId);
            model.CanProvideBankDetails.Should().BeNull();
            model.OrganisationName.Should().Be(legalEntity.Name);
        }
    }
}
