using System.Net;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay;
using Tgs.Payment.DibaoPay.Client;
using Tgs.Payment.DibaoPay.MessageQueue;
using Tgs.Payment.Protos;
using Tgs.SerialNumberGenerator;

namespace DibaoPay.Core.UnitTests;

public class DepositProcessorTests
{
    [Fact]
    public async Task 儲值要求處理_Momo支付()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api.dibaopay.support");

        var fakeSystemClock = Substitute.For<ISystemClock>();

        var fakeDibaoPayClient = new DibaoPayClient(
            httpClient,
            new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance),
            fakeSystemClock,
            NullLogger<DibaoPayClient>.Instance);

        var fakePaymentClient = Substitute.For<IPaymentClient>();
        var options = new DibaoPayOptions
        {
            CallbackBaseUrl = new Uri("https://dev-payment.rpgp.cc/dibaopay"),
            PaymentApiUrl = new Uri("https://api.dibaopay.support"),
            PaymentServiceBaseUrl = new Uri("http://localhost:5541")
        };
        var formId = Guid.Parse("13b03ac8926611ee94340242ac120003");
        var merchantId = "PGTES";
        var privateKey = "hpDXGkYeB0Sof1LT";

        var sut = new DepositProcessor(
            fakeDibaoPayClient,
            fakePaymentClient,
            new OptionsWrapper<DibaoPayOptions>(options));

        var applySettings = new DibaoPayDepositSettings(
            merchantId,
            privateKey,
            formId,
            "923",
            "127.0.0.1",
            ExchangeRate: 1m);

        var request = new DepositRequest
        {
            Amount = "200000",
            ApplyInfo = JsonSerializer.Serialize(applySettings, DibaoPayUtility.SerializerOptions)
        };

        _ = fakeSystemClock.UtcNow
            .Returns(DateTimeOffset.FromUnixTimeMilliseconds(1701670024858));

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://api.dibaopay.support/pay")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Content-Type","application/x-www-form-urlencoded" }
            })
            .WithContent(await new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "uid", "PGTES" },
                { "orderid", "13b03ac8926611ee94340242ac120003" },
                { "channel", "923" },
                { "notify_url", "https://dev-payment.rpgp.cc/dibaopay/deposit/callback" },
                { "amount", "200000" },
                { "userip", "127.0.0.1" },
                { "timestamp", "1701670024858" },
                { "custom" , "" },
                { "sign", "A1600AA7349DE1F272624E43F4D19AF4" }
            }).ReadAsStringAsync())
            .Respond(HttpStatusCode.OK, new StringContent(
                """
                {
                    "status": 10000,
                    "result": {
                        "transactionid": "1701670026408496760",
                        "payurl": "https://p.dibaopay.money/payMomo?p=yavDaDv56",
                        "points": "200000"
                    },
                    "sign": "FADD3E72799DAC5DB62F3A4A4FD94259"
                }
                """));

        // Act
        var actual = await sut.HandleAsync(request.ToByteArray());
        var response = DepositResponse.Parser.ParseFrom(actual.Span);
        var applyInfo = JsonSerializer.Deserialize<DibaoPayDepositApplyInfo>(response.Result, DibaoPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .ScheduleAsync(
                Arg.Is("tgs.payment.dibaopay.deposit.schedule.reject"),
                Arg.Is(applySettings.FormId),
                Arg.Any<DateTime>());

        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
        Assert.Equal(
            new DibaoPayDepositResponse(
                200000m,
                applySettings.FormId,
                new Uri("https://p.dibaopay.money/payMomo?p=yavDaDv56"),
                "1701670026408496760"),
            applyInfo.Response);
    }

    [Fact]
    public async Task 儲值要求處理_Momo支付_匯率轉換()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api.dibaopay.support");

        var fakeSystemClock = Substitute.For<ISystemClock>();

        var fakeDibaoPayClient = new DibaoPayClient(
            httpClient,
            new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance),
            fakeSystemClock,
            NullLogger<DibaoPayClient>.Instance);

        var fakePaymentClient = Substitute.For<IPaymentClient>();
        var options = new DibaoPayOptions
        {
            CallbackBaseUrl = new Uri("https://dev-payment.rpgp.cc/dibaopay"),
            PaymentApiUrl = new Uri("https://api.dibaopay.support"),
            PaymentServiceBaseUrl = new Uri("http://localhost:5541")
        };
        var formId = Guid.Parse("13b03ac8926611ee94340242ac120003");
        var merchantId = "PGTES";
        var privateKey = "hpDXGkYeB0Sof1LT";

        var sut = new DepositProcessor(
            fakeDibaoPayClient,
            fakePaymentClient,
            new OptionsWrapper<DibaoPayOptions>(options));

        var applySettings = new DibaoPayDepositSettings(
            merchantId,
            privateKey,
            formId,
            "923",
            "127.0.0.1",
            ExchangeRate: 1000m);

        var request = new DepositRequest
        {
            Amount = "200",
            ApplyInfo = JsonSerializer.Serialize(applySettings, DibaoPayUtility.SerializerOptions)
        };

        _ = fakeSystemClock.UtcNow
            .Returns(DateTimeOffset.FromUnixTimeMilliseconds(1701670024858));

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://api.dibaopay.support/pay")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Content-Type","application/x-www-form-urlencoded" }
            })
            .WithContent(await new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "uid", "PGTES" },
                { "orderid", "13b03ac8926611ee94340242ac120003" },
                { "channel", "923" },
                { "notify_url", "https://dev-payment.rpgp.cc/dibaopay/deposit/callback" },
                { "amount", "200000" },
                { "userip", "127.0.0.1" },
                { "timestamp", "1701670024858" },
                { "custom" , "" },
                { "sign", "A1600AA7349DE1F272624E43F4D19AF4" }
            }).ReadAsStringAsync())
            .Respond(HttpStatusCode.OK, new StringContent(
                """
                {
                    "status": 10000,
                    "result": {
                        "transactionid": "1701670026408496760",
                        "payurl": "https://p.dibaopay.money/payMomo?p=yavDaDv56",
                        "points": "200000"
                    },
                    "sign": "FADD3E72799DAC5DB62F3A4A4FD94259"
                }
                """));

        // Act
        var actual = await sut.HandleAsync(request.ToByteArray());
        var response = DepositResponse.Parser.ParseFrom(actual.Span);
        var applyInfo = JsonSerializer.Deserialize<DibaoPayDepositApplyInfo>(response.Result, DibaoPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .ScheduleAsync(
                Arg.Is("tgs.payment.dibaopay.deposit.schedule.reject"),
                Arg.Is(applySettings.FormId),
                Arg.Any<DateTime>());

        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
        Assert.Equal(
            new DibaoPayDepositResponse(
                200000m,
                applySettings.FormId,
                new Uri("https://p.dibaopay.money/payMomo?p=yavDaDv56"),
                "1701670026408496760"),
            applyInfo.Response);
    }
}
