using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Tgs.SerialNumberGenerator;

namespace Tgs.Payment.DibaoPay;

public class DibaoPayClient(
    HttpClient httpClient,
    DibaoPayHasher hasher,
    ISystemClock systemClock,
    ILogger<DibaoPayClient> logger)
{
    public async ValueTask<DibaoPayClientDepositResponse> DepositAsync(string merchantId, string privateKey, DepositOrder order, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["uid"] = merchantId,
            ["orderid"] = order.FormId.ToString("N"),
            ["channel"] = order.Channel,
            ["notify_url"] = order.CallbackUrl.AbsoluteUri,
            ["amount"] = order.Amount.ToString("#.##"),
            ["userip"] = order.UserIp,
            ["timestamp"] = systemClock.UtcNow.ToUnixTimeMilliseconds().ToString(),
            ["custom"] = "",
        };

        var sign = hasher.ComputeHash(privateKey, dict);
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, "/pay")
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("DibaoPayClient Deposit Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Deposit response can't read.");

        var status = node["status"]!.GetValue<int>();

        if (status != (int)DibaoPayStatus.成功执行)
        {
            var errorMessage = ((DibaoPayStatus)status).ToString();
            logger.LogError("DibaoPayClient Deposit Error:{errorMessage}", errorMessage);
            throw new ApiFailException(errorMessage);
        }

        var result = node["result"]!;

        return new DibaoPayClientDepositResponse(
            Amount: decimal.Parse(result["points"]!.GetValue<string>()),
            FormId: order.FormId,
            ThirdPartyOrderNo: result["transactionid"]!.GetValue<string>(),
            RedirectUrl: new Uri(result["payurl"]!.GetValue<string>()));
    }

    public async ValueTask<DibaoPayClientOrderQueryStatus?> QueryOrderStatusAsync(string merchantId, string privateKey, Guid formId, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["uid"] = merchantId,
            ["timestamp"] = systemClock.UtcNow.ToUnixTimeMilliseconds().ToString(),
            ["orderid"] = formId.ToString("N"),
        };

        var sign = hasher.ComputeHash(privateKey, dict);
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, "/orderquery")
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("DibaoPayClient Query Deposit Status Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Deposit Status response can't read.");

        var status = node["status"]!.GetValue<int>();

        if (status != (int)DibaoPayStatus.成功执行)
        {
            var errorMessage = ((DibaoPayStatus)status).ToString();
            logger.LogError("DibaoPayClient Query Deposit Status Error:{errorMessage}", errorMessage);
            throw new ApiFailException(errorMessage);
        }

        var result = node["result"]!;

        var orderStatus = result["data"]?["0"]?["status"]?.GetValue<int>();

        var enumTryParse = Enum.TryParse<DibaoPayClientOrderQueryStatus>(orderStatus.ToString(), out var orderQueryStatus);

        return enumTryParse ? orderQueryStatus : null;
    }

    public async ValueTask<DibaoPayClientWithdrawResponse> WithdrawAsync(string merchantId, string privateKey, WithdrawOrder order, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["uid"] = merchantId,
            ["orderid"] = order.FormId.ToString("N"),
            ["channel"] = "712",
            ["notify_url"] = order.CallbackUrl.AbsoluteUri,
            ["amount"] = order.Amount.ToString("#.##"),
            ["userip"] = order.UserIp,
            ["timestamp"] = systemClock.UtcNow.ToUnixTimeMilliseconds().ToString(),
            ["custom"] = "",
            ["bank_account"] = order.BankAccount,
            ["bank_no"] = order.BankNo,
            ["bank_id"] = order.BankId,
        };

        var sign = hasher.ComputeHash(privateKey, dict);
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, "/applyfor")
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("DibaoPayClient Withdraw Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Withdraw response can't read.");

        var status = node["status"]!.GetValue<int>();

        if (status != (int)DibaoPayStatus.成功执行)
        {
            var errorMessage = ((DibaoPayStatus)status).ToString();
            logger.LogError("DibaoPayClient Withdraw Error:{errorMessage}", errorMessage);
            throw new ApiFailException(errorMessage);
        }

        var result = node["result"]!;

        return new DibaoPayClientWithdrawResponse(
            Amount: order.Amount,
            FormId: order.FormId,
            ThirdPartyOrderNo: result["transactionid"]!.GetValue<string>());
    }

    public async ValueTask<decimal> QueryBalanceAsync(string merchantId, string privateKey, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["uid"] = merchantId,
            ["timestamp"] = systemClock.UtcNow.ToUnixTimeMilliseconds().ToString(),
        };

        var sign = hasher.ComputeHash(privateKey, dict);
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, "/getpoints")
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("DibaoPayClient Query Merchant Balance Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Merchant Balance response can't read.");

        var status = node["status"]!.GetValue<int>();

        if (status != (int)DibaoPayStatus.成功执行)
        {
            var errorMessage = ((DibaoPayStatus)status).ToString();
            logger.LogError("DibaoPayClient Query Merchant Balance Error:{errorMessage}", errorMessage);
            throw new ApiFailException(errorMessage);
        }

        var result = node["result"]!;
        var points = result["points"]!.GetValue<string>();

        return decimal.Parse(points);
    }
}
