using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using SFA.DAS.EmployerIncentives.Web.ViewModels;

namespace SFA.DAS.EmployerIncentives.Web.SystemAcceptanceTests.Extensions
{
    public class ViewModelAssertions : ReferenceTypeAssertions<IViewModel, ViewModelAssertions>
    {
        public ViewModelAssertions(IViewModel instance)
        {
            Subject = instance;
        }

        protected override string Identifier => "ViewModel";

        public AndConstraint<ViewModelAssertions> HaveTitle(string title, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!string.IsNullOrEmpty(title))
            .FailWith("Title to assert on not provided")
            .Then
            .Given(() => Subject.Title)
            .ForCondition(t => t.Equals(title))
            .FailWith("Expected {context:Title} to contain {0} but found {1}",
                _ => title, item => item);

            return new AndConstraint<ViewModelAssertions>(this);
        }
    }
}
