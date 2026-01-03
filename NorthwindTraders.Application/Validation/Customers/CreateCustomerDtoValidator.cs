using FluentValidation;
using NorthwindTraders.Application.Dtos.Customers;

namespace NorthwindTraders.Application.Validation.Customers;

public sealed class CreateCustomerDtoValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(40);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(40);

    }
}
