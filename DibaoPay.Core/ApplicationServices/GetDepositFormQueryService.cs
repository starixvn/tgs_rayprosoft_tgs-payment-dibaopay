using System.Text.Json;
using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay.Client;
using Tgs.ServiceUtility;

namespace Tgs.Payment.DibaoPay.ApplicationServices;

internal class GetDepositFormQueryService(IPaymentServiceClient paymentServiceClient)
    : IGetService<GetDepositFormQuery, DepositForm?>
{
    public async ValueTask<DepositForm?> GetAsync(GetDepositFormQuery query, CancellationToken cancellationToken = default)
    {
        var data = await paymentServiceClient
            .GetDepositFormAsync(query.FormId, cancellationToken)
            .ConfigureAwait(false);

        return data == null
            ? null
            : new DepositForm(
                data.Id,
                data.PlayerCode,
                data.BrandId,
                data.BrandPaymentId,
                data.Amount,
                data.Status,
                string.IsNullOrEmpty(data.ApplyInfo)
        ? null
        : JsonSerializer.Deserialize<DibaoPayDepositApplyInfo>(data.ApplyInfo, DibaoPayUtility.SerializerOptions),
                data.PaymentReturn);
    }
}
