using System.ComponentModel;

namespace Tgs.Payment.DibaoPay.Client;
public record DibaoPayDepositSettings(
    string MerchantId,
    string PrivateKey,
    Guid FormId,
    string Channel,
    string UserIp,
    [property: Description("匯率轉換")]
    decimal ExchangeRate);
