public static class TodoSchedule
{
    public static (DateTime? StartAtUtc, DateTime? EndAtUtc) Normalize(DateTime? startAtUtc, DateTime? endAtUtc)
    {
        return (NormalizeUtc(startAtUtc), NormalizeUtc(endAtUtc));
    }

    public static bool TryValidate(DateTime? startAtUtc, DateTime? endAtUtc, out string error)
    {
        if (startAtUtc is null && endAtUtc is null)
        {
            error = string.Empty;
            return true;
        }

        if (startAtUtc is null || endAtUtc is null)
        {
            error = "Start and end must both be provided.";
            return false;
        }

        if (endAtUtc < startAtUtc)
        {
            error = "End must be on or after start.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (value is null)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }
}
