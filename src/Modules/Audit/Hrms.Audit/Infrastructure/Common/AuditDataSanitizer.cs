using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hrms.Audit.Infrastructure.Common;

public static class AuditDataSanitizer
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "PasswordHash",
        "PasswordSalt",
        "SecurityStamp",
        "ConcurrencyStamp",
        "Token",
        "RefreshToken",
        "Secret",
        "ApiKey",
        "Nid",
        "NidNumber",
        "NidNumberEncrypted",
        "NationalId"
    };

    public static string? SanitizeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var node = JsonNode.Parse(json);
            if (node == null) return null;

            SanitizeNode(node);
            return node.ToJsonString();
        }
        catch (JsonException)
        {
            // If invalid JSON, return null to avoid violating database ISJSON check constraints
            return null;
        }
    }

    public static string? SerializeAndSanitize<T>(T? data) where T : class
    {
        if (data == null) return null;
        var json = JsonSerializer.Serialize(data);
        return SanitizeJson(json);
    }

    private static void SanitizeNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            var propertyNames = obj.Select(kv => kv.Key).ToList();
            foreach (var prop in propertyNames)
            {
                if (IsSensitiveKey(prop))
                {
                    obj[prop] = "[REDACTED]";
                }
                else if (obj[prop] is JsonNode child)
                {
                    SanitizeNode(child);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item != null)
                {
                    SanitizeNode(item);
                }
            }
        }
    }

    public static bool IsSensitiveKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (SensitiveKeys.Contains(key)) return true;

        foreach (var sensitive in SensitiveKeys)
        {
            if (key.Contains(sensitive, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
