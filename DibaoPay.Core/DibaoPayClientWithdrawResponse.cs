namespace Tgs.Payment.DibaoPay;
public record DibaoPayClientWithdrawResponse(
    Guid FormId,
    string ThirdPartyOrderNo,
    decimal Amount);
