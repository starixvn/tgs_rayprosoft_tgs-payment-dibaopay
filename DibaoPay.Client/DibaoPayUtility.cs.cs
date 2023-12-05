using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Payment.DibaoPay.Client;

public static class DibaoPayUtility
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = {
        }
    };
}
