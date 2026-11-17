using System;
using System.Data;
using System.Globalization;
using Dapper;

namespace Activout.DatabaseClient.Dapper;

/// <summary>
/// Dapper type handlers that read <see cref="Guid"/> and <see cref="DateTime"/> values stored as text, as SQLite and
/// libSQL store them. A <see cref="DateTime"/> stored as text is read as UTC.
/// </summary>
/// <remarks>
/// Dapper's type handlers are global, so <see cref="Register"/> affects every Dapper query in the process. Native
/// Guid and DateTime values from other databases are returned unchanged, and parameters are not affected: Dapper
/// lets the provider format Guid and DateTime parameters.
/// </remarks>
public static class SqliteTypeHandlers
{
    /// <summary>
    /// The format <see cref="DateTime"/> values are stored in, always in UTC. It keeps milliseconds, as SQLite's own
    /// date format does, so sub-millisecond ticks are dropped.
    /// </summary>
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>Registers the handlers with Dapper. Nullable Guid and DateTime are covered too.</summary>
    public static void Register()
    {
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new DateTimeHandler());
    }

    /// <summary>Converts a <see cref="Guid"/> to the text it is stored as.</summary>
    /// <param name="value">The value.</param>
    public static string ToText(Guid value) => value.ToString("D");

    /// <summary>
    /// Converts a <see cref="DateTime"/> to the UTC text it is stored as. A value with
    /// <see cref="DateTimeKind.Unspecified"/> is taken to be UTC already.
    /// </summary>
    /// <param name="value">The value.</param>
    public static string ToText(DateTime value) =>
        (value.Kind == DateTimeKind.Unspecified ? value : value.ToUniversalTime())
        .ToString(DateTimeFormat, CultureInfo.InvariantCulture);

    private class GuidHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value) => value is Guid g ? g : Guid.Parse((string)value);
        public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value;
    }

    private class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override DateTime Parse(object value) => value is DateTime d
            ? d
            : DateTime.Parse((string)value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        public override void SetValue(IDbDataParameter parameter, DateTime value) => parameter.Value = value;
    }
}
