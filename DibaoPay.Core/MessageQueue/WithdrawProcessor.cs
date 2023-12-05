using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.DibaoPay.MessageQueue;

internal class WithdrawProcessor : IMessageProcessor
{
    private readonly DibaoPayClient _dibaoPayClient;
    private readonly IPaymentClient _paymentClient;
    private readonly DibaoPayOptions _options;

    public WithdrawProcessor(
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
        var request = WithdrawRequest.Parser.ParseFrom(data.Span);
        var settings = JsonSerializer.Deserialize<DibaoPayWithdrawSettings>(request.ApplyInfo, DibaoPayUtility.SerializerOptions)!;

        var order = new WithdrawOrder(
            settings.FormId,
            decimal.Parse(request.Amount),
            new Uri($"{_options.CallbackBaseUrl}/withdraw/callback"),
            settings.UserIp,
            settings.BankId,
            settings.BankAccount,
            settings.BankNo);

        var withdrawResponse = await _dibaoPayClient.WithdrawAsync(
            settings.MerchantId,
            settings.PrivateKey,
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new DibaoPayWithdrawApplyInfo(
            settings,
            new DibaoPayWithdrawResponse(withdrawResponse.FormId, withdrawResponse.ThirdPartyOrderNo));

        await _paymentClient.SaveWithdrawFormThirdPartyOrderNoAsync(
            settings.FormId,
            withdrawResponse.ThirdPartyOrderNo,
            cancellationToken);

        var msg = new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, DibaoPayUtility.SerializerOptions)
        };

        return msg.ToByteArray();
    }
}
