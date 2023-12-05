namespace Tgs.Payment.DibaoPay.Client;
public record DibaoPayWithdrawResponse(
    Guid FormId,
    string ThirdPartyOrderNo);
