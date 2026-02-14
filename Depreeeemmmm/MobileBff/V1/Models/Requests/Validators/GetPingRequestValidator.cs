using FluentValidation;

namespace Depreeeemmmm.MobileBff.V1.Models.Requests.Validators;

public class GetPingRequestValidator : AbstractValidator<GetPingRequest>
{
    public GetPingRequestValidator()
    {
        RuleFor(x => x.Pinger)
            .NotEmpty();
    }
}