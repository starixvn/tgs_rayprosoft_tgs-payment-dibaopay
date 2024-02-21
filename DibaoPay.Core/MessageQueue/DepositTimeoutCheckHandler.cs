using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.DibaoPay.MessageQueue;

internal class DepositTimeoutCheckHandler(
    IPaymentServiceClient paymentServiceClient,
    IPaymentClient paymentClient,
    ILogger<DepositTimeoutCheckHandler> logger)
    : IMessageHandler<ScheduleRequest>
{
    public async ValueTask HandleAsync(
        string subject,
        ScheduleRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Deposit Schedule Handler Receive request Form Id:{FormId}, "
                + "Schedule Time:{ScheduleTime:yyyy/MM/dd HH:mm:ss}",
            data.FormId.ToGuid(),
            data.ScheduleTime.ToDateTime());

        var formId = data.FormId.ToGuid();

        try
        {
            var depositForm = await paymentServiceClient
                .GetDepositFormAsync(formId, cancellationToken)
                .ConfigureAwait(false);

            if (depositForm is null)
            {
                logger.LogWarning("Deposit Form {FormId} not found.", data.FormId.ToGuid());
                return;
            }

            var brandId = depositForm.BrandId;

            logger.LogInformation(
                "GetDepositFormAsync Form Id:{FormId}, Brand Id:{BrandId}, Status:{Status}",
                depositForm.Id,
                brandId,
                depositForm.Status);

            if (depositForm.Status >= PaymentFlowState.Reject)
                return;

            var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

            if (fsm.CanFire(PaymentTrigger.Reject))
            {
                await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                await paymentClient
                    .RejectDepositAsync(formId, brandId, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Process deposit timeout error!");

            await paymentClient.ScheduleAsync(
                "tgs.payment.dibaopay.deposit.schedule.reject",
                formId,
                DateTime.UtcNow.AddMinutes(10D),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
