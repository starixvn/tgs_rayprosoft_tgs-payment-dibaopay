using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Tgs.Payment.DibaoPay.ViewModels;

public class CallbackDepositViewModel
{
    [FromForm(Name = "status")]
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [FromForm(Name = "result")]
    [JsonPropertyName("result")]
    public string Result { get; set; } = default!;

    [FromForm(Name = "sign")]
    [JsonPropertyName("sign")]
    public string Sign { get; set; } = default!;
}
