using System;
using System.Collections.Generic;

namespace SFA.DAS.EmployerIncentives.Web.Services.Users.Types
{
    public class GetUserRequest
    {
        public Guid UserRef { get; set; }
        public IEnumerable<UserRole> Roles { get; set; }        
    }
}
