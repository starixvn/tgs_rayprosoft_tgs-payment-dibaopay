using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.DibaoPay;

public class DibaoPayHasher(ILogger<DibaoPayHasher> logger)
{
    public string ComputeHash(string privateKey, IEnumerable<KeyValuePair<string, string?>> dict)
    {
        var content = string.Join(
            "&",
            dict.Where(kvp => kvp.Key is not "sign")
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(p => $"{p.Key}={p.Value}"));

        var fullText = $"{content}&key={privateKey}";

        logger.LogTrace("FullText: {fullText}", fullText);

        var hash = MD5.HashData(Encoding.UTF8.GetBytes(fullText));

        return Convert.ToHexString(hash).ToUpper();
    }
}
