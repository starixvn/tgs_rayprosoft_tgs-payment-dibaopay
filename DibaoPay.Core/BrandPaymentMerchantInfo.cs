using System.Text.Json.Serialization;

namespace Tgs.Payment.DibaoPay;
record BrandPaymentMerchantInfo(
    [property: JsonPropertyName("merchantId")] string MerchantId,
    [property: JsonPropertyName("privateKey")] string PrivateKey);
