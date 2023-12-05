namespace Tgs.Payment.DibaoPay;
public record WithdrawOrder(
    Guid FormId,
    decimal Amount,
    Uri CallbackUrl,
    string UserIp,
    string BankId,
    string BankAccount,
    string BankNo);
