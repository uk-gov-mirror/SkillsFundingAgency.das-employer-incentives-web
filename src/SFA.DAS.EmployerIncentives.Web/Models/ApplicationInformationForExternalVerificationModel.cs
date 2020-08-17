﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SFA.DAS.EmployerIncentives.Web.Models
{
    public class ApplicationInformationForExternalVerificationModel
    {
        public bool IsNew { get; set; } = true;
        public string HashedLegalEntityId { get; set; }
        public string VendorId { get; set; } = "00000000";
        public string SubmittedByFullName { get; set; } = "";
        public string SubmittedByEmailAddress { get; set; } = "";
        public decimal IncentiveAmount { get; set; }
        public string HashedAccountId { get; set; }
        public Guid ApplicationId { get; set; }
        public IEnumerable<SignedAgreementModel> SignedAgreements { get; set; } = new List<SignedAgreementModel>();

        public string ToPsvString()
        {
            return string.Join("|", HashedLegalEntityId, VendorId, SubmittedByFullName, SubmittedByEmailAddress, IncentiveAmount.ToString(CultureInfo.InvariantCulture),
                string.Join("|", SignedAgreements.Select(x => x.ToPsvString())), $"apps={SignedAgreements.Count()}");
        }
    }
}