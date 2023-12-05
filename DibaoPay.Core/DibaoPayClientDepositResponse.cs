namespace Tgs.Payment.DibaoPay;
public record DibaoPayClientDepositResponse(
    decimal Amount,
    Guid FormId,
    string ThirdPartyOrderNo,
    Uri RedirectUrl);
