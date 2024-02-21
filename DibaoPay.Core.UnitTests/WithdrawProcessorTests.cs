using System.Net;
using System.Text.Json;
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

public class WithdrawProcessorTests
{
    [Fact]
    public async Task 處理提款要求()
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

        var sut = new WithdrawProcessor(
            fakeDibaoPayClient,
            fakePaymentClient,
            new OptionsWrapper<DibaoPayOptions>(options));

        var formId = Guid.Parse("3c8ede8e926d11ee94340242ac120003");
        var merchantId = "PGTES";
        var privateKey = "hpDXGkYeB0Sof1LT";

        var applySettings = new DibaoPayWithdrawSettings(
            merchantId,
            privateKey,
            formId,
            "127.0.0.1",
            BankId: "2008",
            BankAccount: "testBankAccount",
            BankNo: "testBankNo",
            ExchangeRate: 1m);

        var request = new WithdrawRequest
        {
            Amount = "100000",
            ApplyInfo = JsonSerializer.Serialize(applySettings, DibaoPayUtility.SerializerOptions)
        };

        _ = fakeSystemClock.UtcNow
            .Returns(DateTimeOffset.FromUnixTimeMilliseconds(1701671020850));

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://api.dibaopay.support/applyfor")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Content-Type","application/x-www-form-urlencoded" }
            })
            .WithContent(await new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "uid", "PGTES" },
                { "orderid", "3c8ede8e926d11ee94340242ac120003" },
                { "channel", "712" },
                { "notify_url", "https://dev-payment.rpgp.cc/dibaopay/withdraw/callback" },
                { "amount", "100000" },
                { "userip", "127.0.0.1" },
                { "timestamp", "1701671020850" },
                { "custom" , "" },
                { "bank_account" , "testBankAccount" },
                { "bank_no" , "testBankNo" },
                { "bank_id" , "2008" },
                { "sign", "D0EE056B5B6CB1B58651FE4C4ADD6E4C" }
            }).ReadAsStringAsync())
            .Respond(HttpStatusCode.OK, new StringContent(
                """
                {
                    "status": 10000,
                    "result": {
                        "transactionid": "1701671022943602649"
                    },
                    "sign": "FED53CE0D812A7F0602A6E9C5C41AA14"
                }
                """));

        // Act
        var actual = await sut.HandleAsync("test", request, null);
        var applyInfo = JsonSerializer.Deserialize<DibaoPayWithdrawApplyInfo>(actual.Result, DibaoPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                Arg.Is(applySettings.FormId),
                Arg.Is("1701671022943602649"));
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
    }

    [Fact]
    public async Task 處理提款要求_匯率轉換()
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

        var sut = new WithdrawProcessor(
            fakeDibaoPayClient,
            fakePaymentClient,
            new OptionsWrapper<DibaoPayOptions>(options));

        var formId = Guid.Parse("3c8ede8e926d11ee94340242ac120003");
        var merchantId = "PGTES";
        var privateKey = "hpDXGkYeB0Sof1LT";

        var applySettings = new DibaoPayWithdrawSettings(
            merchantId,
            privateKey,
            formId,
            "127.0.0.1",
            BankId: "2008",
            BankAccount: "testBankAccount",
            BankNo: "testBankNo",
            ExchangeRate: 1000m);

        var request = new WithdrawRequest
        {
            Amount = "100",
            ApplyInfo = JsonSerializer.Serialize(applySettings, DibaoPayUtility.SerializerOptions)
        };

        _ = fakeSystemClock.UtcNow
            .Returns(DateTimeOffset.FromUnixTimeMilliseconds(1701671020850));

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://api.dibaopay.support/applyfor")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Content-Type","application/x-www-form-urlencoded" }
            })
            .WithContent(await new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "uid", "PGTES" },
                { "orderid", "3c8ede8e926d11ee94340242ac120003" },
                { "channel", "712" },
                { "notify_url", "https://dev-payment.rpgp.cc/dibaopay/withdraw/callback" },
                { "amount", "100000" },
                { "userip", "127.0.0.1" },
                { "timestamp", "1701671020850" },
                { "custom" , "" },
                { "bank_account" , "testBankAccount" },
                { "bank_no" , "testBankNo" },
                { "bank_id" , "2008" },
                { "sign", "D0EE056B5B6CB1B58651FE4C4ADD6E4C" }
            }).ReadAsStringAsync())
            .Respond(HttpStatusCode.OK, new StringContent(
                """
                {
                    "status": 10000,
                    "result": {
                        "transactionid": "1701671022943602649"
                    },
                    "sign": "FED53CE0D812A7F0602A6E9C5C41AA14"
                }
                """));

        // Act
        var actual = await sut.HandleAsync("test", request, null);
        var applyInfo = JsonSerializer.Deserialize<DibaoPayWithdrawApplyInfo>(actual.Result, DibaoPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                Arg.Is(applySettings.FormId),
                Arg.Is("1701671022943602649"));
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
    }
}
