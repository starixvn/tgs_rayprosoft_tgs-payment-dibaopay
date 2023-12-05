namespace Tgs.Payment.DibaoPay.Client;
public record DibaoPayDepositSettings(
    string MerchantId,
    string PrivateKey,
    Guid FormId,
    string Channel,
    string UserIp);
