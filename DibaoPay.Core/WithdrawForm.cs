using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay.Client;

namespace Tgs.Payment.DibaoPay;
public record WithdrawForm(
    Guid Id,
    string? UserId,
    UserType UserType,
    Guid BrandId,
    Guid? BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    Guid BankInfoId,
    DibaoPayWithdrawApplyInfo? ApplyInfo,
    string? PaymentReturn = null);
