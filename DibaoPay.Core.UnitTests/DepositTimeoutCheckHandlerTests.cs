using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay.Client;
using Tgs.Payment.DibaoPay.MessageQueue;
using Tgs.Payment.Protos;

namespace DibaoPay.Core.UnitTests;

public class DepositTimeoutCheckHandlerTests
{
    [Fact]
    public async Task 申請六十分鐘的單子會自動拒絕()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

        var fakePaymentClient = Substitute.For<IPaymentClient>();

        var sut = new DepositTimeoutCheckHandler(fakePaymentServiceClient, fakePaymentClient, NullLogger<DepositTimeoutCheckHandler>.Instance);

        var formId = Guid.Parse("13b03ac8926611ee94340242ac120003");
        var brandId = Guid.Parse("4e86719f-243f-46f6-b010-5c758b7285af");
        var request = new ScheduleRequest
        {
            FormId = formId,
            Subject = "tgs.payment.dibaopay.deposit.schedule.reject",
            ScheduleTime = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        var applyInfo = new DibaoPayDepositApplyInfo(
            new DibaoPayDepositSettings(
                "PGTES",
                "hpDXGkYeB0Sof1LT",
                formId,
                "923",
                "127.0.0.1",
                ExchangeRate: 1m),
            new DibaoPayDepositResponse(
                200000m,
                formId,
                new Uri("https://p.dibaopay.money/payMomo?p=yavDaDv56"),
                "1701670026408496760"));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new DepositFormInfo
            {
                Id = formId,
                Amount = 25000m,
                BrandId = brandId,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, DibaoPayUtility.SerializerOptions)
            });

        // Act
        await sut.HandleAsync(request.ToByteArray());

        // Assert
        _ = fakePaymentClient
            .Received(1)
            .RejectDepositAsync(Arg.Is(formId), Arg.Is(brandId));
    }
}
