﻿using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Web.Models;
using SFA.DAS.EmployerIncentives.Web.Services.Applications;
using SFA.DAS.EmployerIncentives.Web.ViewModels.Applications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Web.Tests.Controllers.PaymentsController
{
    [TestFixture]
    public class WhenPaymentApplicationsAccessed
    {
        private Web.Controllers.PaymentsController _sut;
        private Mock<IApplicationService> _service;
        private Fixture _fixture;
        private string _accountId;
        private string _sortOrder;
        private string _sortField;

        [SetUp]
        public void Arrange()
        {
            _service = new Mock<IApplicationService>();
            _sut = new Web.Controllers.PaymentsController(_service.Object);
            _fixture = new Fixture();
            _accountId = _fixture.Create<string>();
            _sortOrder = ApplicationsSortOrder.Ascending;
            _sortField = ApplicationsSortField.ApprenticeName;
        }

        [Test]
        public async Task Then_the_view_contains_summary_for_submitted_applications()
        {
            // Arrange
            var applications = new List<ApprenticeApplicationModel>();
            applications.AddRange(_fixture.CreateMany<ApprenticeApplicationModel>(5));
            foreach(var application in applications)
            {
                application.Status = "Submitted";
            }
            _service.Setup(x => x.GetList(_accountId)).ReturnsAsync(applications);

            // Act
            var result = await _sut.ListPayments(_accountId, _sortOrder, _sortField) as ViewResult;

            // Assert
            var viewModel = result.Model as ViewApplicationsViewModel;
            viewModel.Should().NotBeNull();
            viewModel.Applications.Count().Should().Be(applications.Count());
        }

        [Test]
        public async Task Then_the_view_contains_summary_for_only_submitted_applications()
        {
            // Arrange
            var applications = new List<ApprenticeApplicationModel>();
            applications.AddRange(_fixture.CreateMany<ApprenticeApplicationModel>(5));
            applications[2].Status = "Submitted";
            applications[4].Status = "Submitted";

            _service.Setup(x => x.GetList(_accountId)).ReturnsAsync(applications);

            // Act
            var result = await _sut.ListPayments(_accountId, _sortOrder, _sortField) as ViewResult;

            // Assert
            var viewModel = result.Model as ViewApplicationsViewModel;
            viewModel.Should().NotBeNull();
            viewModel.Applications.Count().Should().Be(applications.Count(x => x.Status == "Submitted"));
        }

        [Test]
        public async Task Then_a_shutter_page_is_shown_if_no_applcations()
        {
            // Arrange
            var applications = new List<ApprenticeApplicationModel>();

            _service.Setup(x => x.GetList(_accountId)).ReturnsAsync(applications);

            // Act
            var result = await _sut.ListPayments(_accountId, _sortOrder, _sortField) as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            result.ActionName.Should().Be("NoApplications");
        }

        [Test]
        public async Task Then_a_shutter_page_is_shown_if_only_applcations_are_in_progress()
        {
            // Arrange
            var applications = new List<ApprenticeApplicationModel>();
            applications.AddRange(_fixture.CreateMany<ApprenticeApplicationModel>(2));
            applications[0].Status = "InProgress";
            applications[1].Status = "InProgress";

            _service.Setup(x => x.GetList(_accountId)).ReturnsAsync(applications);

            // Act
            var result = await _sut.ListPayments(_accountId, _sortOrder, _sortField) as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            result.ActionName.Should().Be("NoApplications");
        }

        [TestCase(null)]
        [TestCase("")]
        public async Task Then_the_default_sort_order_is_ascending_by_apprentice_name(string orderByText)
        {
            // Arrange
            var applications = new List<ApprenticeApplicationModel>();
            applications.AddRange(_fixture.CreateMany<ApprenticeApplicationModel>(2));
            applications[0].Status = "Submitted";
            applications[0].FirstName = "Steve";
            applications[0].LastName = "Jones";
            applications[1].Status = "Submitted";
            applications[1].FirstName = "Freda";
            applications[1].LastName = "Johnson";

            _service.Setup(x => x.GetList(_accountId)).ReturnsAsync(applications);

            // Act
            var result = await _sut.ListPayments(_accountId, orderByText, orderByText) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            var viewModel = result.Model as ViewApplicationsViewModel;
            viewModel.Should().NotBeNull();
            var modelApplications = viewModel.Applications.ToArray();
            modelApplications[0].ApprenticeName.Should().Be(applications[1].ApprenticeName);
            modelApplications[1].ApprenticeName.Should().Be(applications[0].ApprenticeName);
        }

        [Test]
        public async Task Then_applications_are_sorted_by_application_date_descending()
        {
            // Arrange
            var applications = new List<ApprenticeApplicationModel>();
            applications.AddRange(_fixture.CreateMany<ApprenticeApplicationModel>(2));
            applications[0].Status = "Submitted";
            applications[0].ApplicationDate = new DateTime(2020, 09, 01);
            applications[1].Status = "Submitted";
            applications[1].ApplicationDate = new DateTime(2020, 08, 20);

            _service.Setup(x => x.GetList(_accountId)).ReturnsAsync(applications);

            // Act
            var result = await _sut.ListPayments(_accountId, ApplicationsSortOrder.Descending, ApplicationsSortField.ApplicationDate) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            var viewModel = result.Model as ViewApplicationsViewModel;
            viewModel.Should().NotBeNull();
            var modelApplications = viewModel.Applications.ToArray();
            modelApplications[1].ApplicationDate.Should().Be(applications[1].ApplicationDate);
            modelApplications[0].ApplicationDate.Should().Be(applications[0].ApplicationDate);
        }

        [Test]
        public async Task Then_applications_are_sorted_by_application_date_ascending()
        {
            // Arrange
            var applications = new List<ApprenticeApplicationModel>();
            applications.AddRange(_fixture.CreateMany<ApprenticeApplicationModel>(2));
            applications[0].Status = "Submitted";
            applications[0].ApplicationDate = new DateTime(2020, 09, 01);
            applications[1].Status = "Submitted";
            applications[1].ApplicationDate = new DateTime(2020, 08, 20);

            _service.Setup(x => x.GetList(_accountId)).ReturnsAsync(applications);

            // Act
            var result = await _sut.ListPayments(_accountId, ApplicationsSortOrder.Ascending, ApplicationsSortField.ApplicationDate) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            var viewModel = result.Model as ViewApplicationsViewModel;
            viewModel.Should().NotBeNull();
            var modelApplications = viewModel.Applications.ToArray();
            modelApplications[0].ApplicationDate.Should().Be(applications[1].ApplicationDate);
            modelApplications[1].ApplicationDate.Should().Be(applications[0].ApplicationDate);
        }
    }
}