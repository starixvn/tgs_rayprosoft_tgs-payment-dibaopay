using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.DibaoPay.MessageQueue;

internal class DepositProcessor(
    DibaoPayClient dibaoPayClient,
    IPaymentClient paymentClient,
    IOptions<DibaoPayOptions> optionsAccessor)
    : IMessageProcessor<DepositRequest, DepositResponse>
{
    private readonly DibaoPayOptions _options = optionsAccessor.Value;

    public async ValueTask<DepositResponse> HandleAsync(
        string subject,
        DepositRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<DibaoPayDepositSettings>(
            data.ApplyInfo,
            DibaoPayUtility.SerializerOptions)!;

        var order = new DepositOrder(
            decimal.Parse(data.Amount) * settings.ExchangeRate,
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            settings.Channel,
            settings.UserIp);

        var depositResponse = await dibaoPayClient.DepositAsync(
            settings.MerchantId,
            settings.PrivateKey,
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new DibaoPayDepositApplyInfo(
            settings,
            new DibaoPayDepositResponse(
                depositResponse.Amount,
                depositResponse.FormId,
                depositResponse.RedirectUrl,
                depositResponse.ThirdPartyOrderNo));

        await paymentClient.ScheduleAsync(
            "tgs.payment.dibaopay.deposit.schedule.reject",
            settings.FormId,
            DateTime.UtcNow.AddMinutes(60D),
            cancellationToken).ConfigureAwait(false);

        return new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, DibaoPayUtility.SerializerOptions)
        };
    }
}
