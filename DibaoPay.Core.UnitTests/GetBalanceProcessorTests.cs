using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Tgs.Payment.DibaoPay;
using Tgs.Payment.DibaoPay.MessageQueue;
using Tgs.Payment.Protos;

namespace DibaoPay.Core.UnitTests;

public class GetBalanceProcessorTests
{
    [Fact]
    public async Task GetBalanceProcessor_取得商戶餘額()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api.dibaopay.support");

        var fakeTimeProvider = Substitute.For<TimeProvider>();

        var fakeDibaoPayClient = new DibaoPayClient(
            httpClient,
            new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance),
            fakeTimeProvider,
            NullLogger<DibaoPayClient>.Instance);

        var sut = new GetBalanceProcessor(fakeDibaoPayClient);

        var request = new PaymentBalanceRequest
        {
            BrandPaymentSettings = """
                {
                    "merchantId": "PGTES",
                    "privateKey": "hpDXGkYeB0Sof1LT"
                }
                """
        };

        _ = fakeTimeProvider.GetUtcNow()
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

        // Act
        var actual = await sut.HandleAsync("test", request, null);

        // Assert
        Assert.Equal(0M, actual.Balance.ToDecimal());
    }
}
