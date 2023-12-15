using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.DibaoPay.MessageQueue;

internal class DepositProcessor : IMessageProcessor
{
    private readonly DibaoPayClient _dibaoPayClient;
    private readonly IPaymentClient _paymentClient;
    private readonly DibaoPayOptions _options;

    public DepositProcessor(
        DibaoPayClient dibaoPayClient,
        IPaymentClient paymentClient,
        IOptions<DibaoPayOptions> optionsAccessor)
    {
        _dibaoPayClient = dibaoPayClient ?? throw new ArgumentNullException(nameof(dibaoPayClient));
        _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
        _options = optionsAccessor.Value;
    }

    public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = DepositRequest.Parser.ParseFrom(data.Span);

        var settings = JsonSerializer.Deserialize<DibaoPayDepositSettings>(
            request.ApplyInfo,
            DibaoPayUtility.SerializerOptions)!;

        var order = new DepositOrder(
            decimal.Parse(request.Amount) * settings.ExchangeRate,
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            settings.Channel,
            settings.UserIp);

        var depositResponse = await _dibaoPayClient.DepositAsync(
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

        await _paymentClient.ScheduleAsync(
            "tgs.payment.dibaopay.deposit.schedule.reject",
            settings.FormId,
            DateTime.UtcNow.AddMinutes(11D),
            cancellationToken).ConfigureAwait(false);

        var msg = new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, DibaoPayUtility.SerializerOptions)
        };

        return msg.ToByteArray();
    }
}
