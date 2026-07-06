using System.Globalization;
using System.Text.Json;

namespace BRaVe_Portal.Helpers;

public static class FileBasedDeduplicationUploadValidator
{
    private static readonly HashSet<string> AllowedHouseholdTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Family", "Single", "Group" };

    private static readonly HashSet<string> AllowedGenders =
        new(StringComparer.OrdinalIgnoreCase) { "Male", "Female", "Other", "PreferNotToSay" };

    private static readonly HashSet<string> AllowedRelationships =
        new(StringComparer.OrdinalIgnoreCase) { "Self", "Spouse", "Child", "Parent", "Sibling", "Other" };

    private static readonly HashSet<string> AllowedBiometricTypes =
        new(StringComparer.OrdinalIgnoreCase) { "finger", "voice", "face", "iris", "none" };

    private static readonly HashSet<string> AllowedBiometricFormats =
        new(StringComparer.OrdinalIgnoreCase) { "Neurotechnology", "iso" };

    private static readonly string[] RequiredFingerPositions =
    [
        "RightThumb",
        "RightIndex",
        "RightMiddle",
        "RightRing",
        "RightLittle",
        "LeftThumb",
        "LeftIndex",
        "LeftMiddle",
        "LeftRing",
        "LeftLittle"
    ];

    private static readonly string[] HouseholdProperties =
    [
        "householdId",
        "uuid",
        "householdType",
        "householdLocation",
        "RegisteredOn",
        "RegisteredBy",
        "source",
        "token",
        "additionalInfo",
        "householdMembers"
    ];

    private static readonly string[] MemberProperties =
    [
        "memberId",
        "uuid",
        "firstname",
        "middlename",
        "lastname",
        "ageInYears",
        "ageInMonts",
        "ageInDays",
        "dob",
        "gender",
        "IsHeadOfHousehold",
        "Relationship",
        "additionalInfo",
        "photoBase64",
        "biometricType",
        "biometricFormat",
        "isBundled",
        "biometricRawTemplateBase64",
        "biometricUnbundledTemplate"
    ];

    public static ValidationResult Validate(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return ValidationResult.Fail($"Invalid JSON file: {ex.Message}");
        }

        using (document)
        {
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                return ValidationResult.Fail("Root element must be a JSON array of households.");
            }

            if (root.GetArrayLength() == 0)
            {
                return ValidationResult.Fail("Upload must contain at least one household.");
            }

            var householdIndex = 0;
            foreach (var household in root.EnumerateArray())
            {
                var error = ValidateHousehold(household, $"household[{householdIndex}]");
                if (error is not null)
                {
                    return ValidationResult.Fail(error);
                }

                householdIndex++;
            }
        }

        return ValidationResult.Ok();
    }

    private static string? ValidateHousehold(JsonElement household, string path)
    {
        if (household.ValueKind != JsonValueKind.Object)
        {
            return $"{path} must be a JSON object.";
        }

        var propertyError = RequireExactProperties(household, HouseholdProperties, path);
        if (propertyError is not null)
        {
            return propertyError;
        }

        var householdId = RequireNonEmptyString(household, "householdId", path);
        if (householdId is null)
        {
            return $"{path}.householdId must be a non-empty string.";
        }

        if (!TryGetGuid(household, "uuid", out _))
        {
            return $"{path}.uuid must be a valid GUID string.";
        }

        var householdType = RequireNonEmptyString(household, "householdType", path);
        if (householdType is null)
        {
            return $"{path}.householdType must be a non-empty string.";
        }

        if (!AllowedHouseholdTypes.Contains(householdType))
        {
            return $"{path}.householdType must be one of: Family, Single, Group.";
        }

        if (RequireNonEmptyString(household, "householdLocation", path) is null)
        {
            return $"{path}.householdLocation must be a non-empty string.";
        }

        var registeredOn = RequireNonEmptyString(household, "RegisteredOn", path);
        if (registeredOn is null || !DateTime.TryParse(registeredOn, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
        {
            return $"{path}.RegisteredOn must be a valid ISO 8601 date/time string.";
        }

        if (RequireNonEmptyString(household, "RegisteredBy", path) is null)
        {
            return $"{path}.RegisteredBy must be a non-empty string.";
        }

        if (RequireNonEmptyString(household, "source", path) is null)
        {
            return $"{path}.source must be a non-empty string.";
        }

        if (!household.TryGetProperty("additionalInfo", out var additionalInfo) ||
            additionalInfo.ValueKind != JsonValueKind.Object)
        {
            return $"{path}.additionalInfo must be a JSON object.";
        }

        if (!household.TryGetProperty("householdMembers", out var members) ||
            members.ValueKind != JsonValueKind.Array)
        {
            return $"{path}.householdMembers must be a JSON array.";
        }

        if (members.GetArrayLength() == 0)
        {
            return $"{path}.householdMembers must contain at least one member.";
        }

        var memberIndex = 0;
        foreach (var member in members.EnumerateArray())
        {
            var memberError = ValidateMember(member, $"{path}.householdMembers[{memberIndex}]");
            if (memberError is not null)
            {
                return memberError;
            }

            memberIndex++;
        }

        var headCount = members.EnumerateArray().Count(member =>
            member.TryGetProperty("IsHeadOfHousehold", out var isHead) &&
            isHead.ValueKind == JsonValueKind.True);

        if (headCount > 1)
        {
            return $"{path}.householdMembers can have at most one member with IsHeadOfHousehold set to true. Zero heads is allowed.";
        }

        return null;
    }

    private static string? ValidateMember(JsonElement member, string path)
    {
        if (member.ValueKind != JsonValueKind.Object)
        {
            return $"{path} must be a JSON object.";
        }

        var propertyError = RequireExactProperties(member, MemberProperties, path);
        if (propertyError is not null)
        {
            return propertyError;
        }

        if (!TryGetInt(member, "memberId", out _))
        {
            return $"{path}.memberId must be a numeric value.";
        }

        if (!TryGetGuid(member, "uuid", out _))
        {
            return $"{path}.uuid must be a valid GUID string.";
        }

        if (RequireNonEmptyString(member, "firstname", path) is null)
        {
            return $"{path}.firstname must be a non-empty string.";
        }

        if (!member.TryGetProperty("middlename", out var middlename) || middlename.ValueKind != JsonValueKind.String)
        {
            return $"{path}.middlename must be a string.";
        }

        if (RequireNonEmptyString(member, "lastname", path) is null)
        {
            return $"{path}.lastname must be a non-empty string.";
        }

        var ageOrDobError = ValidateAgeOrDob(member, path);
        if (ageOrDobError is not null)
        {
            return ageOrDobError;
        }

        var gender = RequireNonEmptyString(member, "gender", path);
        if (gender is null)
        {
            return $"{path}.gender must be a non-empty string.";
        }

        if (!AllowedGenders.Contains(gender))
        {
            return $"{path}.gender must be one of: Male, Female, Other, PreferNotToSay.";
        }

        if (!member.TryGetProperty("IsHeadOfHousehold", out var isHead) ||
            isHead.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return $"{path}.IsHeadOfHousehold must be a boolean value.";
        }

        var relationship = RequireNonEmptyString(member, "Relationship", path);
        if (relationship is null)
        {
            return $"{path}.Relationship must be a non-empty string.";
        }

        if (!AllowedRelationships.Contains(relationship))
        {
            return $"{path}.Relationship must be one of: Self, Spouse, Child, Parent, Sibling, Other.";
        }

        if (!member.TryGetProperty("additionalInfo", out var additionalInfo) ||
            additionalInfo.ValueKind != JsonValueKind.Object)
        {
            return $"{path}.additionalInfo must be a JSON object.";
        }

        if (RequireNonEmptyString(member, "photoBase64", path) is null)
        {
            return $"{path}.photoBase64 must be a non-empty string.";
        }

        return ValidateBiometrics(member, path);
    }

    private static string? ValidateAgeOrDob(JsonElement member, string path)
    {
        if (TryGetNullableInt(member, "ageInYears", path, out var ageInYears) is { } ageYearsError)
        {
            return ageYearsError;
        }

        if (TryGetNullableInt(member, "ageInMonts", path, out var ageInMonts) is { } ageMonthsError)
        {
            return ageMonthsError;
        }

        if (TryGetNullableInt(member, "ageInDays", path, out var ageInDays) is { } ageDaysError)
        {
            return ageDaysError;
        }

        if (TryGetNullableDob(member, path, out var dob) is { } dobError)
        {
            return dobError;
        }

        var allAgesNull = !ageInYears.HasValue && !ageInMonts.HasValue && !ageInDays.HasValue;
        var hasAnyAge = ageInYears.HasValue || ageInMonts.HasValue || ageInDays.HasValue;

        if (allAgesNull && !dob.HasValue)
        {
            return $"{path}: provide dob when all age fields are null, or provide at least one age field when dob is null.";
        }

        if (hasAnyAge && dob.HasValue)
        {
            return $"{path}: provide either dob (with all age fields null) or at least one age field (with dob null); age fields and dob cannot both be set.";
        }

        return null;
    }

    private static string? TryGetNullableInt(
        JsonElement member,
        string propertyName,
        string path,
        out int? value)
    {
        value = null;

        if (!member.TryGetProperty(propertyName, out var property))
        {
            return $"{path}.{propertyName} is required.";
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var number))
        {
            return $"{path}.{propertyName} must be null or a numeric value.";
        }

        value = number;
        return null;
    }

    private static string? TryGetNullableDob(JsonElement member, string path, out DateOnly? dob)
    {
        dob = null;

        if (!member.TryGetProperty("dob", out var property))
        {
            return $"{path}.dob is required.";
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            return $"{path}.dob must be null or a date string in YYYY-MM-DD format.";
        }

        var dobValue = property.GetString();
        if (string.IsNullOrWhiteSpace(dobValue))
        {
            return null;
        }

        if (!DateOnly.TryParseExact(dobValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDob))
        {
            return $"{path}.dob must be a date string in YYYY-MM-DD format.";
        }

        dob = parsedDob;
        return null;
    }

    private static string? ValidateBiometrics(JsonElement member, string path)
    {
        var biometricType = RequireNonEmptyString(member, "biometricType", path);
        if (biometricType is null)
        {
            return $"{path}.biometricType must be a non-empty string.";
        }

        if (!AllowedBiometricTypes.Contains(biometricType))
        {
            return $"{path}.biometricType must be one of: finger, voice, face, iris, none.";
        }

        if (!member.TryGetProperty("isBundled", out var isBundled) ||
            isBundled.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return $"{path}.isBundled must be a boolean value.";
        }

        if (!member.TryGetProperty("biometricRawTemplateBase64", out var biometricRawTemplateBase64) ||
            biometricRawTemplateBase64.ValueKind != JsonValueKind.String)
        {
            return $"{path}.biometricRawTemplateBase64 must be a string.";
        }

        if (!member.TryGetProperty("biometricUnbundledTemplate", out var biometricUnbundledTemplate) ||
            biometricUnbundledTemplate.ValueKind != JsonValueKind.Array)
        {
            return $"{path}.biometricUnbundledTemplate must be a JSON array.";
        }

        if (string.Equals(biometricType, "none", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateNoBiometricCollected(
                member,
                path,
                isBundled,
                biometricRawTemplateBase64,
                biometricUnbundledTemplate);
        }

        var biometricFormat = RequireNonEmptyString(member, "biometricFormat", path);
        if (biometricFormat is null)
        {
            return $"{path}.biometricFormat must be a non-empty string.";
        }

        if (!AllowedBiometricFormats.Contains(biometricFormat))
        {
            return $"{path}.biometricFormat must be one of: Neurotechnology, iso.";
        }

        var isFinger = string.Equals(biometricType, "finger", StringComparison.OrdinalIgnoreCase);
        var bundled = isBundled.GetBoolean();
        var base64Value = biometricRawTemplateBase64.GetString() ?? string.Empty;

        if (isFinger)
        {
            if (bundled)
            {
                if (string.IsNullOrWhiteSpace(base64Value))
                {
                    return $"{path}.biometricRawTemplateBase64 must be a non-empty string when biometricType is finger and isBundled is true.";
                }

                if (biometricUnbundledTemplate.GetArrayLength() > 0)
                {
                    return $"{path}.biometricUnbundledTemplate must be an empty array when biometricType is finger and isBundled is true.";
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(base64Value))
                {
                    return $"{path}.biometricRawTemplateBase64 must be an empty string when biometricType is finger and isBundled is false.";
                }

                return ValidateUnbundledFingers(biometricUnbundledTemplate, path);
            }
        }
        else
        {
            if (!bundled)
            {
                return $"{path}.isBundled must be true when biometricType is not finger.";
            }

            if (string.IsNullOrWhiteSpace(base64Value))
            {
                return $"{path}.biometricRawTemplateBase64 must be a non-empty string when biometricType is {biometricType}.";
            }

            if (biometricUnbundledTemplate.GetArrayLength() > 0)
            {
                return $"{path}.biometricUnbundledTemplate must be an empty array when biometricType is not finger.";
            }
        }

        return null;
    }

    private static string? ValidateNoBiometricCollected(
        JsonElement member,
        string path,
        JsonElement isBundled,
        JsonElement biometricRawTemplateBase64,
        JsonElement biometricUnbundledTemplate)
    {
        if (!member.TryGetProperty("biometricFormat", out var biometricFormat) ||
            biometricFormat.ValueKind != JsonValueKind.String)
        {
            return $"{path}.biometricFormat must be a string.";
        }

        if (!string.IsNullOrEmpty(biometricFormat.GetString()))
        {
            return $"{path}.biometricFormat must be an empty string when biometricType is none.";
        }

        if (!isBundled.GetBoolean())
        {
            return $"{path}.isBundled must be true when biometricType is none.";
        }

        if (!string.IsNullOrEmpty(biometricRawTemplateBase64.GetString()))
        {
            return $"{path}.biometricRawTemplateBase64 must be an empty string when biometricType is none.";
        }

        if (biometricUnbundledTemplate.GetArrayLength() > 0)
        {
            return $"{path}.biometricUnbundledTemplate must be an empty array when biometricType is none.";
        }

        return null;
    }

    private static string? ValidateUnbundledFingers(JsonElement biometricUnbundledTemplate, string path)
    {
        if (biometricUnbundledTemplate.GetArrayLength() != RequiredFingerPositions.Length)
        {
            return $"{path}.biometricUnbundledTemplate must contain exactly {RequiredFingerPositions.Length} finger entries when biometricType is finger and isBundled is false.";
        }

        var fingers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entryIndex = 0;

        foreach (var entry in biometricUnbundledTemplate.EnumerateArray())
        {
            var entryPath = $"{path}.biometricUnbundledTemplate[{entryIndex}]";

            if (entry.ValueKind != JsonValueKind.Object)
            {
                return $"{entryPath} must be a JSON object.";
            }

            var entryProperties = entry.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
            if (!entryProperties.SetEquals(new HashSet<string>(StringComparer.Ordinal) { "finger", "base64" }))
            {
                return $"{entryPath} must contain exactly the properties: finger, base64.";
            }

            var finger = RequireNonEmptyString(entry, "finger", entryPath);
            if (finger is null)
            {
                return $"{entryPath}.finger must be a non-empty string.";
            }

            if (!RequiredFingerPositions.Contains(finger, StringComparer.OrdinalIgnoreCase))
            {
                return $"{entryPath}.finger must be one of: {string.Join(", ", RequiredFingerPositions)}.";
            }

            if (!fingers.Add(finger))
            {
                return $"{entryPath}.finger '{finger}' is duplicated.";
            }

            if (RequireNonEmptyString(entry, "base64", entryPath) is null)
            {
                return $"{entryPath}.base64 must be a non-empty string.";
            }

            entryIndex++;
        }

        var missingFingers = RequiredFingerPositions
            .Where(finger => !fingers.Contains(finger))
            .ToList();

        if (missingFingers.Count > 0)
        {
            return $"{path}.biometricUnbundledTemplate is missing finger entries: {string.Join(", ", missingFingers)}.";
        }

        return null;
    }

    private static string? RequireExactProperties(JsonElement element, IReadOnlyList<string> expectedProperties, string path)
    {
        var actualProperties = element.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);

        var missing = expectedProperties.Where(property => !actualProperties.Contains(property)).ToList();
        if (missing.Count > 0)
        {
            return $"{path} is missing required properties: {string.Join(", ", missing)}.";
        }

        var unexpected = actualProperties.Where(property => !expectedProperties.Contains(property)).ToList();
        if (unexpected.Count > 0)
        {
            return $"{path} contains unexpected properties: {string.Join(", ", unexpected)}.";
        }

        return null;
    }

    private static string? RequireNonEmptyString(JsonElement element, string propertyName, string path)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return $"{path}.{propertyName} must be a string.";
        }

        return string.IsNullOrWhiteSpace(value.GetString()) ? null : value.GetString();
    }

    private static bool TryGetGuid(JsonElement element, string propertyName, out Guid guid)
    {
        guid = default;
        return element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String &&
               Guid.TryParse(value.GetString(), out guid);
    }

    private static bool TryGetInt(JsonElement element, string propertyName, out int number)
    {
        number = default;
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        return value.TryGetInt32(out number);
    }
}

public readonly record struct ValidationResult(bool IsValid, string? ErrorMessage)
{
    public static ValidationResult Ok() => new(true, null);

    public static ValidationResult Fail(string errorMessage) => new(false, errorMessage);
}
