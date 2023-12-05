namespace Tgs.Payment.DibaoPay.Client;
public record DibaoPayWithdrawSettings(
    string MerchantId,
    string PrivateKey,
    Guid FormId,
    string UserIp,
    string BankId,
    string BankAccount,
    string BankNo);
