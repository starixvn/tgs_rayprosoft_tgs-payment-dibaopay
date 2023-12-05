using System.Text.Json.Serialization;

namespace Tgs.Payment.DibaoPay.ViewModels;

public class WithdrawCallbackResult
{
    [JsonPropertyName("transactionid")]
    public string TransactionId { get; set; } = default!;

    [JsonPropertyName("orderid")]
    public string OrderId { get; set; } = default!;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = default!;

    [JsonPropertyName("real_amount")]
    [JsonConverter(typeof(DecimalStringJsonConverter))]
    public string RealAmount { get; set; } = default!;

    [JsonPropertyName("custom")]
    public string Custom { get; set; } = default!;
}
