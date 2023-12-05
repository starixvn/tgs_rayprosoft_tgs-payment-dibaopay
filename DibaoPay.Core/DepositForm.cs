using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay.Client;

namespace Tgs.Payment.DibaoPay;
public record DepositForm(
    Guid Id,
    string PlayerCode,
    Guid BrandId,
    Guid BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    DibaoPayDepositApplyInfo? ApplyInfo,
    string? PaymentReturn = null);
