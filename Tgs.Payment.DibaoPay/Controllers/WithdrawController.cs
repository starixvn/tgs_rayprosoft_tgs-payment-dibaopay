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
public class WithdrawController : ControllerBase
{
    private readonly ILogger<WithdrawController> _logger;

    public WithdrawController(ILogger<WithdrawController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("callback", Name = "NotifyWithdraw")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<string> WithdrawCallbackAsync(
    [FromServices] DibaoPayHasher hasher,
        [FromServices] IGetService<GetWithdrawFormQuery, WithdrawForm?> getService,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromForm] CallbackWithdrawViewModel source)
    {
        var result = JsonSerializer.Deserialize<WithdrawCallbackResult>(source.Result);

        if (result == null)
        {
            _logger.LogWarning("Can't deserialize withdraw callback result. ({result})", source.Result);
            return "fail";
        }

        var formId = Guid.Parse(result.OrderId);

        var withdrawForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (withdrawForm == null)
        {
            _logger.LogWarning("Can't found withdraw form({id}).", formId);
            return "fail";
        }

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(source))!;

        var sign = source.Sign;
        var hash = hasher.ComputeHash(
            withdrawForm.ApplyInfo!.Settings.PrivateKey,
            dict.Where(kv => kv.Value is not null)
                .Select(kv => new KeyValuePair<string, string?>(
                kv.Key,
                kv.Value!.ToString())));

        _logger.LogInformation("Compute hash: {hash}, sign: {sign}", hash, sign);

        if (sign != hash)
        {
            _logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", sign, hash);
            return "fail";
        }

        var status = (DibaoPayStatus)source.Status;

        var paymentReturn = new DibaoPayWithdrawReturn()
        {
            FormId = formId,
            Amount = decimal.Parse(result.Amount),
            Sign = sign,
            ThirdPartyOrderNo = result.TransactionId,
            Status = status.ToString(),
        };

        var fsm = PaymentStateMachineFactory.CreateWithdrawStateMachine(withdrawForm.Status);

        switch (status)
        {
            case DibaoPayStatus.成功执行:
                if (fsm.CanFire(PaymentTrigger.PaymentWithdrawCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentWithdrawCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            DibaoPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.PaidWithdrawAsync(
                        withdrawForm.Id,
                        withdrawForm.BrandId).ConfigureAwait(false);
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

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            DibaoPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectWithdrawAsync(
                        withdrawForm.Id,
                        withdrawForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                _logger.LogWarning("FormId:{formId}, ThirdPartyOrderNo:{ThirdPartyOrderNo}",
                    formId,
                    paymentReturn.ThirdPartyOrderNo);
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
