using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.DibaoPay.MessageQueue;

internal class WithdrawProcessor(
    DibaoPayClient dibaoPayClient,
    IPaymentClient paymentClient,
    IOptions<DibaoPayOptions> optionsAccessor)
    : IMessageProcessor<WithdrawRequest, WithdrawResponse>
{
    private readonly DibaoPayOptions _options = optionsAccessor.Value;

    public async ValueTask<WithdrawResponse> HandleAsync(
        string subject,
        WithdrawRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<DibaoPayWithdrawSettings>(data.ApplyInfo, DibaoPayUtility.SerializerOptions)!;

        var order = new WithdrawOrder(
            settings.FormId,
            decimal.Parse(data.Amount) * settings.ExchangeRate,
            new Uri($"{_options.CallbackBaseUrl}/withdraw/callback"),
            settings.UserIp,
            settings.BankId,
            settings.BankAccount,
            settings.BankNo);

        var withdrawResponse = await dibaoPayClient.WithdrawAsync(
            settings.MerchantId,
            settings.PrivateKey,
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new DibaoPayWithdrawApplyInfo(
            settings,
            new DibaoPayWithdrawResponse(withdrawResponse.FormId, withdrawResponse.ThirdPartyOrderNo));

        await paymentClient.SaveWithdrawFormThirdPartyOrderNoAsync(
            settings.FormId,
            withdrawResponse.ThirdPartyOrderNo,
            cancellationToken);

        return new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, DibaoPayUtility.SerializerOptions)
        };
    }
}
