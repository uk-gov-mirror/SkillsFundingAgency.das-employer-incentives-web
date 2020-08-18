﻿using System.Collections.Generic;

namespace SFA.DAS.EmployerIncentives.Web.Services.Applications.Types
{
    public class IncentiveApplicationDto
    {
        public long AccountLegalEntityId { get; set; }
        public IEnumerable<IncentiveApplicationApprenticeshipDto> Apprenticeships { get; set; }
    }
}