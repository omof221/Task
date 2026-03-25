using FluentValidation;
using ProductService.Application.Commands;

namespace ProductService.Application.Validators;

/// <summary>UpdateProductCommand için FluentValidation kuralları.</summary>
public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Ürün ID boş olamaz.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün adı boş olamaz.")
            .MaximumLength(200).WithMessage("Ürün adı 200 karakteri geçemez.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Açıklama 1000 karakteri geçemez.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Fiyat negatif olamaz.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stok negatif olamaz.");
    }
}
