using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Activout.DatabaseClient.LibSql;

/// <summary>
/// A reader over rows that were already read, so that Dapper can map them without the schema calls the libSQL
/// reader does not support. Values and column names are kept exactly as the database returned them.
/// </summary>
internal sealed class BufferedDataReader(string[] names, List<object[]> rows) : DbDataReader
{
    private int _row = -1;

    public override int FieldCount => names.Length;
    public override bool HasRows => rows.Count > 0;
    public override bool IsClosed => false;
    public override int RecordsAffected => -1;
    public override int Depth => 0;
    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));

    public override bool Read() => ++_row < rows.Count;
    public override bool NextResult() => false;
    public override string GetName(int ordinal) => names[ordinal];
    public override int GetOrdinal(string name) => Array.FindIndex(names, n => n.Equals(name, StringComparison.OrdinalIgnoreCase)) is var i and >= 0
        ? i
        : throw new IndexOutOfRangeException(name);

    // The column's type when all its values share one, otherwise object: SQLite allows mixed types in a column.
    public override Type GetFieldType(int ordinal) =>
        rows.Select(r => r[ordinal]).Where(v => v is not DBNull).Select(v => v.GetType()).Distinct().ToList() is [var t]
            ? t
            : typeof(object);

    public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
    public override object GetValue(int ordinal) => rows[_row][ordinal];
    public override bool IsDBNull(int ordinal) => GetValue(ordinal) is DBNull;

    public override int GetValues(object[] values)
    {
        var n = Math.Min(values.Length, names.Length);
        Array.Copy(rows[_row], values, n);
        return n;
    }

    public override bool GetBoolean(int ordinal) => Convert.ToBoolean(GetValue(ordinal));
    public override byte GetByte(int ordinal) => Convert.ToByte(GetValue(ordinal));
    public override char GetChar(int ordinal) => Convert.ToChar(GetValue(ordinal));
    public override DateTime GetDateTime(int ordinal) => Convert.ToDateTime(GetValue(ordinal));
    public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(GetValue(ordinal));
    public override double GetDouble(int ordinal) => Convert.ToDouble(GetValue(ordinal));
    public override float GetFloat(int ordinal) => Convert.ToSingle(GetValue(ordinal));
    public override Guid GetGuid(int ordinal) => GetValue(ordinal) is Guid g ? g : Guid.Parse(GetString(ordinal));
    public override short GetInt16(int ordinal) => Convert.ToInt16(GetValue(ordinal));
    public override int GetInt32(int ordinal) => Convert.ToInt32(GetValue(ordinal));
    public override long GetInt64(int ordinal) => Convert.ToInt64(GetValue(ordinal));
    public override string GetString(int ordinal) => Convert.ToString(GetValue(ordinal))!;
    public override IEnumerator GetEnumerator() => new DbEnumerator(this);

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
        Copy((byte[])GetValue(ordinal), dataOffset, buffer, bufferOffset, length);

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
        Copy(GetString(ordinal).ToCharArray(), dataOffset, buffer, bufferOffset, length);

    private static long Copy<T>(T[] source, long dataOffset, T[]? buffer, int bufferOffset, int length)
    {
        if (buffer is null) return source.Length;
        var n = (int)Math.Max(0, Math.Min(length, source.Length - dataOffset));
        Array.Copy(source, dataOffset, buffer, bufferOffset, n);
        return n;
    }
}
