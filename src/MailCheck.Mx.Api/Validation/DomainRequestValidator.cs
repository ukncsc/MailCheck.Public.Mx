using FluentValidation;
using MailCheck.Mx.Api.Domain;

namespace MailCheck.Mx.Api.Validation
{
    public class DomainRequestValidator : AbstractValidator<DomainRequest>
    {
        public DomainRequestValidator()
        {
            CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(_ => _.Domain)
                .NotNull()
                .WithMessage("A \"domain\" field is required.")
                .NotEmpty()
                .WithMessage("The \"domain\" field should not be empty.");
        }
    }
}