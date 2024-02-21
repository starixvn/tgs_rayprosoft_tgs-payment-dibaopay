using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.DibaoPay;
using Tgs.SerialNumberGenerator;

namespace DibaoPay.Core.UnitTests;

public class DibaoPayClientTests
{
    [Fact]
    public async Task 查詢商戶餘額()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api.dibaopay.support");

        var signer = new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance);
        var fakeSystemClock = Substitute.For<ISystemClock>();

        var sut = new DibaoPayClient(httpClient, signer, fakeSystemClock, NullLogger<DibaoPayClient>.Instance);

        var merchantId = "PGTES";
        var privateKey = "hpDXGkYeB0Sof1LT";

        _ = fakeSystemClock.UtcNow
            .Returns(DateTimeOffset.FromUnixTimeMilliseconds(1701670501996));

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://api.dibaopay.support/getpoints")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Content-Type","application/x-www-form-urlencoded" }
            })
            .WithContent(await new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "uid", "PGTES" },
                { "timestamp", "1701670501996" },
                { "sign", "A6EE675604B67AACB1CC8499DEC457C6" }
            }).ReadAsStringAsync())
            .Respond(HttpStatusCode.OK, new StringContent(
                """
                {
                    "status": 10000,
                    "result": {
                        "points": "0.00"
                    },
                    "sign": "CA21FE7E656B8211A7A2F2588A550AA8"
                }
                """));

        var actual = await sut.QueryBalanceAsync(merchantId, privateKey);

        Assert.Equal(0, actual);
    }

    [Fact]
    public async Task 存款()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api.dibaopay.support");

        var signer = new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance);
        var fakeSystemClock = Substitute.For<ISystemClock>();

        var sut = new DibaoPayClient(httpClient, signer, fakeSystemClock, NullLogger<DibaoPayClient>.Instance);

        var merchantId = "PGTES";
        var privateKey = "hpDXGkYeB0Sof1LT";
        var formId = Guid.Parse("13b03ac8926611ee94340242ac120003");

        var order = new DepositOrder(
            Amount: 200000m,
            FormId: formId,
            CallbackUrl: new Uri("https://dev-payment.rpgp.cc/dibaopay/deposit/callback"),
            Channel: "923",
            UserIp: "127.0.0.1");

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

        var actual = await sut.DepositAsync(merchantId, privateKey, order);

        Assert.Equal(200000, actual.Amount);
        Assert.Equal(formId, actual.FormId);
        Assert.Equal("1701670026408496760", actual.ThirdPartyOrderNo);
        Assert.Equal(new Uri("https://p.dibaopay.money/payMomo?p=yavDaDv56"), actual.RedirectUrl);
    }

    [Fact]
    public async Task 訂單查詢()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api.dibaopay.support");

        var signer = new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance);
        var fakeSystemClock = Substitute.For<ISystemClock>();

        var sut = new DibaoPayClient(httpClient, signer, fakeSystemClock, NullLogger<DibaoPayClient>.Instance);

        var merchantId = "PGTES";
        var privateKey = "hpDXGkYeB0Sof1LT";
        var formId = Guid.Parse("13b03ac8926611ee94340242ac120003");

        _ = fakeSystemClock.UtcNow
            .Returns(DateTimeOffset.FromUnixTimeMilliseconds(1701670664587));

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://api.dibaopay.support/orderquery")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Content-Type","application/x-www-form-urlencoded" }
            })
            .WithContent(await new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "uid", "PGTES" },
                { "timestamp", "1701670664587" },
                { "orderid", "13b03ac8926611ee94340242ac120003" },
                { "sign", "CF2D5FFC58602BE5AA3E01FC71E6B594" }
            }).ReadAsStringAsync())
            .Respond(HttpStatusCode.OK, new StringContent(
                """
                {
                    "status": 10000,
                    "result": {
                        "totalCount": 1,
                        "page": 1,
                        "row": 20,
                        "count": 1,
                        "data": {
                            "0": {
                                "transactionid": 1701670026408496760,
                                "orderid": "13b03ac8926611ee94340242ac120003",
                                "channel": 923,
                                "amount": "200000.0000",
                                "real_amount": "196400.0000",
                                "status": 0,
                                "bdate": "2023-12-04 14:07:07",
                                "cdate": "2023-12-04 14:07:09"
                            }
                        }
                    },
                    "sign": "CD646C490AF3916FEB7746C6BB3461CA"
                }
                """));

        var actual = await sut.QueryOrderStatusAsync(merchantId, privateKey, formId);

        Assert.Equal(DibaoPayClientOrderQueryStatus.未处理, actual);
    }

    [Fact]
    public async Task 提款()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api.dibaopay.support");

        var signer = new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance);
        var fakeSystemClock = Substitute.For<ISystemClock>();

        var sut = new DibaoPayClient(httpClient, signer, fakeSystemClock, NullLogger<DibaoPayClient>.Instance);

        var merchantId = "PGTES";
        var privateKey = "hpDXGkYeB0Sof1LT";
        var formId = Guid.Parse("3c8ede8e926d11ee94340242ac120003");

        var order = new WithdrawOrder(
            FormId: formId,
            Amount: 100000m,
            CallbackUrl: new Uri("https://dev-payment.rpgp.cc/dibaopay/withdraw/callback"),
            UserIp: "127.0.0.1",
            BankAccount: "testBankAccount",
            BankNo: "testBankNo",
            BankId: "2008");

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

        var actual = await sut.WithdrawAsync(merchantId, privateKey, order);

        Assert.Equal(100000m, actual.Amount);
        Assert.Equal(formId, actual.FormId);
        Assert.Equal("1701671022943602649", actual.ThirdPartyOrderNo);
    }
}
