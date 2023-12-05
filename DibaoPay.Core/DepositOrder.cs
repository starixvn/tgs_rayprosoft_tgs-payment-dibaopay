namespace Tgs.Payment.DibaoPay;
public record DepositOrder(
    decimal Amount,
    Guid FormId,
    Uri CallbackUrl,
    string Channel,
    string UserIp);
