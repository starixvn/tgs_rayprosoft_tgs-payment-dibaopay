using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Payment.DibaoPay;

namespace DibaoPay.Core.UnitTests;

public class HasherTests
{
    [Fact]
    public void 查詢餘額()
    {
        var key = "hpDXGkYeB0Sof1LT";
        var dict = new Dictionary<string, string?>
        {
            ["uid"] = "PGTES",
            ["timestamp"] = "1701670501996",
        };
        var expected = "A6EE675604B67AACB1CC8499DEC457C6";

        var sut = new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 存款()
    {
        var key = "hpDXGkYeB0Sof1LT";
        var dict = new Dictionary<string, string?>
        {
            ["uid"] = "PGTES",
            ["orderid"] = "13b03ac8926611ee94340242ac120003",
            ["channel"] = "923",
            ["notify_url"] = "https://dev-payment.rpgp.cc/dibaopay/deposit/callback",
            ["amount"] = "200000",
            ["userip"] = "127.0.0.1",
            ["timestamp"] = "1701670024858",
            ["custom"] = "",
        };
        var expected = "A1600AA7349DE1F272624E43F4D19AF4";

        var sut = new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 存款回調()
    {
        var key = "hpDXGkYeB0Sof1LT";
        var dict = new Dictionary<string, string?>
        {
            ["status"] = "10000",
            ["result"] = """
            {"transactionid":"1701739417386241290","orderid":"f8c781d5-f7b5-4e32-9e64-a9fabdd17196","amount":"200000.00","real_amount":"196400.00","custom":""}
            """,
            ["sign"] = "ADDB9B208477D3638202D22D54C5FAC0",
        };
        var expected = "ADDB9B208477D3638202D22D54C5FAC0";

        var sut = new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 查詢存款單()
    {
        var key = "hpDXGkYeB0Sof1LT";
        var dict = new Dictionary<string, string?>
        {
            ["uid"] = "PGTES",
            ["timestamp"] = "1701670664587",
            ["orderid"] = "13b03ac8926611ee94340242ac120003",
        };
        var expected = "CF2D5FFC58602BE5AA3E01FC71E6B594";

        var sut = new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 提款()
    {
        var key = "hpDXGkYeB0Sof1LT";
        var dict = new Dictionary<string, string?>
        {
            ["uid"] = "PGTES",
            ["orderid"] = "7ec9ea5e927311ee94340242ac120003",
            ["channel"] = "712",
            ["notify_url"] = "https://dev-payment.rpgp.cc/dibaopay/withdraw/callback",
            ["amount"] = "100000",
            ["userip"] = "127.0.0.1",
            ["timestamp"] = "1701673529976",
            ["custom"] = "",
            ["bank_account"] = "testBankAccount",
            ["bank_no"] = "testBankNo",
            ["bank_id"] = "2008",
        };
        var expected = "53FBED1D5ACB6ECEA9233365BA89F04E";

        var sut = new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 提款回調()
    {
        var key = "hpDXGkYeB0Sof1LT";
        var dict = new Dictionary<string, string?>
        {
            ["status"] = "10000",
            ["result"] = """
            {"transactionid":"1701736591726031228","orderid":"026140a5-fb1b-46ea-82aa-d95f6a1491d2","amount":"100000.00","real_amount":"100000.00","custom":""}
            """,
            ["sign"] = "4A3AAD48C681A37CB4D6CAF184CB7F11",
        };
        var expected = "4A3AAD48C681A37CB4D6CAF184CB7F11";

        var sut = new DibaoPayHasher(NullLogger<DibaoPayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict);

        Assert.Equal(expected, actual);
    }
}
