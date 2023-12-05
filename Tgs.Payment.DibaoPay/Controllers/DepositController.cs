using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay.ApplicationServices;
using Tgs.Payment.DibaoPay.Client;
using Tgs.Payment.DibaoPay.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Payment.DibaoPay.Controllers;

[Route("[controller]")]
[ApiController]
public class DepositController : ControllerBase
{
    private readonly ILogger<DepositController> _logger;

    public DepositController(ILogger<DepositController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("callback", Name = "NotifyDeposit")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<string> DepositCallbackAsync(
    [FromServices] DibaoPayHasher hasher,
        [FromServices] IGetService<GetDepositFormQuery, DepositForm?> getService,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromForm] CallbackDepositViewModel source)
    {
        var result = JsonSerializer.Deserialize<DepositCallbackResult>(source.Result);

        if (result == null)
        {
            _logger.LogWarning("Can't deserialize deposit callback result. ({result})", source.Result);
            return "fail";
        }

        var formId = Guid.Parse(result.OrderId);

        var depositForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (depositForm == null)
        {
            _logger.LogWarning("Can't found deposit form({id}).", formId);
            return "fail";
        }

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(source))!;

        var sign = source.Sign;
        var hash = hasher.ComputeHash(
            depositForm.ApplyInfo!.Settings.PrivateKey,
            dict.Select(kv => new KeyValuePair<string, string?>(
                kv.Key,
                kv.Value!.ToString())));

        _logger.LogInformation("Compute hash: {hash}, sign: {sign}", hash, sign);

        if (sign != hash)
        {
            _logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", sign, hash);
            return "fail";
        }

        var status = (DibaoPayStatus)source.Status;

        var paymentReturn = new DibaoPayDepositReturn()
        {
            Status = status.ToString(),
            FormId = formId,
            ThirdPartyOrderNo = result.TransactionId,
            RealAmount = decimal.Parse(result.RealAmount),
            OrderAmount = decimal.Parse(result.Amount),
            Sign = sign,
        };

        var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

        switch (status)
        {
            case DibaoPayStatus.成功执行:
                if (fsm.CanFire(PaymentTrigger.PaymentDepositCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentDepositCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            DibaoPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.ApproveDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        paymentReturn.RealAmount,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;

            case DibaoPayStatus.订单失效:
            case DibaoPayStatus.登入失败:
            case DibaoPayStatus.餘額不足:
            case DibaoPayStatus.验证失败:
            case DibaoPayStatus.實名失敗:
            case DibaoPayStatus.交易失败:
            case DibaoPayStatus.交易超时:
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            DibaoPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;

            default:
                _logger.LogWarning("FormId:{formId}, ThirdPartyOrderNo:{ThirdPartyOrderNo} , payment Status:{status}",
                    formId,
                    result.TransactionId,
                    source.Status);
                return "fail";
        }

        return "success";
    }
}
