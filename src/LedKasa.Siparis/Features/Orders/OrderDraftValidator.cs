using FluentValidation;

namespace LedKasa.Siparis.Features.Orders;

public sealed class OrderDraftValidator : AbstractValidator<OrderDraft>
{
    public OrderDraftValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Sipariş veren kişi zorunludur.")
            .MaximumLength(160);

        RuleFor(x => x.DeliveryPlace)
            .IsInEnum()
            .WithMessage("Teslim yeri geçersiz.");

        RuleFor(x => x.DeliveryDate)
            .GreaterThanOrEqualTo(x => x.OrderDate)
            .WithMessage("Teslim tarihi sipariş tarihinden önce olamaz.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Siparişte en az bir kalem olmalıdır.");

        RuleForEach(x => x.Items).SetValidator(new OrderItemInputValidator());
    }
}

public sealed class OrderItemInputValidator : AbstractValidator<OrderItemInput>
{
    public OrderItemInputValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Ürün seçimi zorunludur.");
        RuleFor(x => x.WidthCm).GreaterThan(0).WithMessage("Yatay ölçü sıfırdan büyük olmalıdır.");
        RuleFor(x => x.HeightCm).GreaterThan(0).WithMessage("Dikey ölçü sıfırdan büyük olmalıdır.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Adet sıfırdan büyük olmalıdır.");
        RuleFor(x => x.Note).MaximumLength(400);
    }
}
