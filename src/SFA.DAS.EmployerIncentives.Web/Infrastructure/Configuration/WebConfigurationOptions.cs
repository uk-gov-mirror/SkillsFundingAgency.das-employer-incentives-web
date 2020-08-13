﻿namespace SFA.DAS.EmployerIncentives.Web.Infrastructure.Configuration
{
    public class WebConfigurationOptions
    {
        public const string EmployerIncentivesWebConfiguration = "EmployerIncentivesWeb";
        public virtual string RedisCacheConnectionString { get; set; }
        public virtual string CommitmentsBaseUrl { get; set; }
        public virtual string AccountsBaseUrl { get; set; }
        public virtual string AllowedHashstringCharacters { get; set; }
        public virtual string Hashstring { get; set; }
        public virtual string CdnBaseUrl { get; set; }
        public virtual string ZenDeskHelpCentreUrl { get; set; }
        public virtual string ZenDeskSnippetKey { get; set; }
        public virtual string ZenDeskSectionId { get; set; }
        public virtual string ZenDeskCobrowsingSnippetKey { get; set; }
    }
}
