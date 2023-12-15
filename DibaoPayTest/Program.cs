using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay;
using Tgs.Payment.DibaoPay.Client;
using Tgs.SerialNumberGenerator;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddPaymentClient()
    .AddPhoenixToolkitsMessageQueue()
    .AddNatsMessageQueue(natsConfig => natsConfig
        .ConfigQueueOptions((opt, _) => opt.Url = "nats://localhost:4222"))
    .AddNatsGlobPatternExchange("*");

var app = builder.Build();

await app.StartAsync().ConfigureAwait(false);

var logger = app.Services.GetRequiredService<ILogger<Program>>();

var paymentClient = app.Services.GetRequiredService<IPaymentClient>();

var merchantId = "PGTES";
var privateKey = "hpDXGkYeB0Sof1LT";

//Deposit
//var depositSettings = new DibaoPayDepositSettings(merchantId, privateKey, Guid.NewGuid(), "923", "127.0.0.1");

//var depositReturn = await paymentClient.DepositAsync(
//    "tgs.payment.dibaopay.deposit",
//    200000m,
//    JsonSerializer.Serialize(depositSettings, DibaoPayUtility.SerializerOptions)).ConfigureAwait(false);

//logger.LogInformation("DepositReturn: {depositReturn}", depositReturn);


//Withdraw
var withdrawSettings = new DibaoPayWithdrawSettings(
    merchantId, 
    privateKey, 
    Guid.NewGuid(), 
    "127.0.0.1", 
    "2008", 
    "testBankAccount", 
    "testBankNo",
    ExchangeRate: 1m);

var withdrawReturn = await paymentClient.WithdrawAsync(
    "tgs.payment.dibaopay.withdraw",
    100000m,
    JsonSerializer.Serialize(withdrawSettings, DibaoPayUtility.SerializerOptions)).ConfigureAwait(false);

logger.LogInformation("WithdrawReturn: {withdrawReturn}", withdrawReturn);


//訂單查詢
//var httpClient = new HttpClient();
//httpClient.BaseAddress = new Uri("https://api.dibaopay.support");
//var fakeSystemClock = Substitute.For<ISystemClock>();
//var client = new DibaoPayClient(httpClient, new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance), fakeSystemClock, NullLogger<DibaoPayClient>.Instance);

//var queryReturn = await client.QueryOrderStatusAsync(merchantId, privateKey, Guid.Parse("13b03ac8926611ee94340242ac120003"));
//logger.LogInformation("{return}", queryReturn);


//商戶餘額查詢
//var httpClient = new HttpClient();
//httpClient.BaseAddress = new Uri("https://api.dibaopay.support");
//var fakeSystemClock = Substitute.For<ISystemClock>();
//var client = new DibaoPayClient(httpClient, new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance), fakeSystemClock, NullLogger<DibaoPayClient>.Instance);

//var queryReturn = await client.QueryBalanceAsync(merchantId, privateKey);
//logger.LogInformation("{return}", queryReturn);
