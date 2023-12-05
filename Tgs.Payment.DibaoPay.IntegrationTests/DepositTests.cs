using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay.Client;
using Xunit.Abstractions;

namespace Tgs.Payment.DibaoPay.IntegrationTests;

public class DepositTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DepositTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [Fact]
    public async Task 代收回調_訂單狀態成功()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = DibaoPayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("f8c781d5-f7b5-4e32-9e64-a9fabdd17196");
        var brandId = Guid.NewGuid();
        var merchantId = "PGTES";
        var privateKey = "hpDXGkYeB0Sof1LT";

        var dict = new Dictionary<string, string>
        {
            ["status"] = "10000",
            ["result"] = """
            {"transactionid":"1701739417386241290","orderid":"f8c781d5-f7b5-4e32-9e64-a9fabdd17196","amount":"200000.00","real_amount":"196400.00","custom":""}
            """,
            ["sign"] = "ADDB9B208477D3638202D22D54C5FAC0",
        };

        var content = new FormUrlEncodedContent(dict);

        var applyInfo = new DibaoPayDepositApplyInfo(
            new DibaoPayDepositSettings(
                merchantId,
                privateKey,
                formId,
                "923",
                "127.0.0.1"),
             new DibaoPayDepositResponse(
                200000m,
                formId,
                new Uri("https://p.dibaopay.money/payMomo?p=yavDaDv56"),
                "1701739417386241290"));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
            .Returns(new DepositFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.WaitPayment,
                Amount = 200000m,
                ThirdPartyOrderNo = formId.ToString(),
                ApplyInfo = JsonSerializer.Serialize(applyInfo, DibaoPayUtility.SerializerOptions)
            });

        var response = await httpClient.PostAsync("/deposit/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("success", actual);

        var paymentReturn = new DibaoPayDepositReturn()
        {
            Status = DibaoPayStatus.成功执行.ToString(),
            FormId = formId,
            ThirdPartyOrderNo = "1701739417386241290",
            RealAmount = decimal.Parse("196400.00"),
            OrderAmount = decimal.Parse("200000.00"),
            Sign = dict["sign"],
        };

        _ = fakePaymentServiceClient.Received(1)
            .SubmitDepositFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Is(JsonSerializer.Serialize(paymentReturn, DibaoPayUtility.SerializerOptions)),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .ApproveDepositAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Is(paymentReturn.RealAmount),
                Arg.Any<CancellationToken>());
    }
}
