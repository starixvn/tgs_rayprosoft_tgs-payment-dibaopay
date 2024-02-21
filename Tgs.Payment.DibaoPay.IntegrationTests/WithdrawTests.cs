using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay.Client;
using Xunit.Abstractions;

namespace Tgs.Payment.DibaoPay.IntegrationTests;

public class WithdrawTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task 代付回調_訂單狀態成功()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = DibaoPayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("026140a5-fb1b-46ea-82aa-d95f6a1491d2");
        var brandId = Guid.NewGuid();
        var merchantId = "PGTES";
        var privateKey = "hpDXGkYeB0Sof1LT";

        var settings = new DibaoPayWithdrawSettings(
            merchantId,
            privateKey,
            formId,
            "127.0.0.1",
            "2008",
            "testBankAccount",
            "testBankNo",
            ExchangeRate: 1m);
        var withdrawResponse = new DibaoPayWithdrawResponse(formId, "1701736591726031228");

        var applyInfo = new DibaoPayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, DibaoPayUtility.SerializerOptions)
            });

        var dict = new Dictionary<string, string>
        {
            ["status"] = "10000",
            ["result"] = """
            {"transactionid":"1701736591726031228","orderid":"026140a5-fb1b-46ea-82aa-d95f6a1491d2","amount":"100000.00","real_amount":"100000.00","custom":""}
            """,
            ["sign"] = "4A3AAD48C681A37CB4D6CAF184CB7F11",
        };

        var content = new FormUrlEncodedContent(dict);

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        var paymentReturn = new DibaoPayWithdrawReturn()
        {
            FormId = formId,
            Amount = 100000.00m,
            Sign = "4A3AAD48C681A37CB4D6CAF184CB7F11",
            ThirdPartyOrderNo = "1701736591726031228",
            Status = DibaoPayStatus.成功执行.ToString(),
            AfterExchangeAmount = 100000.00m
        };

        Assert.Equal("success", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Is(JsonSerializer.Serialize(paymentReturn, DibaoPayUtility.SerializerOptions)),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .PaidWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 代付回調_訂單狀態交易失败()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = DibaoPayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("a1fa3c034082428e8485e7ff997ae109");
        var brandId = Guid.NewGuid();
        var merchantId = "PGTES";
        var privateKey = "hpDXGkYeB0Sof1LT";

        var settings = new DibaoPayWithdrawSettings(
            merchantId,
            privateKey,
            formId,
            "127.0.0.1",
            BankId: "2008",
            BankAccount: "testBankAccount",
            BankNo: "testBankNo",
            ExchangeRate: 1m);
        var withdrawResponse = new DibaoPayWithdrawResponse(formId, "1701763085030837997");

        var applyInfo = new DibaoPayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, DibaoPayUtility.SerializerOptions)
            });

        var dict = new Dictionary<string, string>
        {
            ["status"] = "30916",
            ["result"] = """
            {"transactionid":"1701763085030837997","orderid":"a1fa3c034082428e8485e7ff997ae109","amount":"100000.00","real_amount":0,"custom":""}
            """,
            ["sign"] = "7BD31668315912D231CF9A9CC15510FF",
        };

        var content = new FormUrlEncodedContent(dict);

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        var paymentReturn = new DibaoPayWithdrawReturn()
        {
            FormId = formId,
            Amount = 100000.00m,
            Sign = "7BD31668315912D231CF9A9CC15510FF",
            ThirdPartyOrderNo = "1701763085030837997",
            Status = DibaoPayStatus.交易失败.ToString(),
            AfterExchangeAmount = 100000.00m
        };

        Assert.Equal("success", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Is(JsonSerializer.Serialize(paymentReturn, DibaoPayUtility.SerializerOptions)),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .RejectWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }
}
