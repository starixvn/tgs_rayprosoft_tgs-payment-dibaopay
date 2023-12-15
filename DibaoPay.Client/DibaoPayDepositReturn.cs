namespace Tgs.Payment.DibaoPay.Client;
public record DibaoPayDepositReturn
{
    public string ThirdPartyOrderNo { get; set; } = default!;

    public decimal OrderAmount { get; set; }

    public decimal RealAmount { get; set; }

    public Guid FormId { get; set; }

    public string Status { get; set; } = default!;

    public string Sign { get; set; } = default!;

    public decimal AfterExchangeOrderAmount { get; set; }

    public decimal AfterExchangeRealAmount { get; set; }
}
