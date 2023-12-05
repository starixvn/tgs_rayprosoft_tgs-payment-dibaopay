namespace Tgs.Payment.DibaoPay;

public enum DibaoPayClientOrderQueryStatus
{
    未处理 = 0,
    交易成功 = 1,
    处理中 = 2,
    交易失败 = 3,
    操作失败 = 4,
    提单失败 = 5,
}
