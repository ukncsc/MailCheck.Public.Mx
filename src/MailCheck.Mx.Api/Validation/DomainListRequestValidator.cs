using System.Collections.Generic;
using FluentValidation;
using MailCheck.Common.Util;
using MailCheck.Mx.Api.Domain;

namespace MailCheck.Mx.Api.Validation
{
    public class DomainListRequestValidator : AbstractValidator<DomainListRequest>
    {
        private readonly DomainValidator _domainValidator = new DomainValidator();

        public DomainListRequestValidator()
        {
            CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(_ => _.Domains)
                .NotNull()
                .WithMessage("A \"domains\" field is required with domains as a list of strings.")
                .NotEmpty()
                .WithMessage("The \"domains\" field should not be empty.")
                .Must(_ => _?.Count <= 100)
                .WithMessage("The \"domainNames\" should contain less than 100 domains.")
                .Must(AreAllDomainsValid)
                .WithMessage("All domains must be be a valid domains");
        }

        public bool AreAllDomainsValid(List<string> domains)
        {
            return domains.TrueForAll(_domainValidator.IsValidDomain);
        }
    }
}
