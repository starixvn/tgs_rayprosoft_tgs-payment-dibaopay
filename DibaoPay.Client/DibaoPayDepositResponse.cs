namespace Tgs.Payment.DibaoPay.Client;
public record DibaoPayDepositResponse(
    decimal Amount,
    Guid FormId,
    Uri RedirectUrl,
    string ThirdPartyOrderNo);
