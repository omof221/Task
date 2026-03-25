using FluentValidation;
using ProductService.Application.Commands;

namespace ProductService.Application.Validators;

/// <summary>
/// AddProductCommand için FluentValidation kuralları.
/// SRP: Validation mantığı tek sınıfta toplandı.
/// ValidationBehavior pipeline'ı bu validator'ı handler çalışmadan önce tetikler.
/// </summary>
public sealed class AddProductCommandValidator : AbstractValidator<AddProductCommand>
{
    public AddProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün adı boş olamaz.")
            .MaximumLength(200).WithMessage("Ürün adı 200 karakteri geçemez.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Açıklama 1000 karakteri geçemez.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Fiyat negatif olamaz.")
            .LessThanOrEqualTo(999_999.99m).WithMessage("Fiyat 999.999,99 değerini geçemez.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stok negatif olamaz.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Stok 1.000.000 adedi geçemez.");
    }
}
