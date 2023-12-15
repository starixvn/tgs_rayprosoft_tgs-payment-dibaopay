using System.ComponentModel;

namespace Tgs.Payment.DibaoPay.Client;
public record DibaoPayWithdrawSettings(
    string MerchantId,
    string PrivateKey,
    Guid FormId,
    string UserIp,
    string BankId,
    string BankAccount,
    string BankNo,
    [property: Description("匯率轉換")]
    decimal ExchangeRate);
