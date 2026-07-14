using Google.Protobuf.WellKnownTypes;
using MerlinORM.Client;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Testing
{
    public enum YesNoEnum
    {
        NA = 0,
        Yes = 1,
        No = 2,
    }

    public class TestModelA : MerlinModelBase
    {        
        public int a { get; set; }
        public sbyte b  { get; set; }
        public byte c  { get; set; }
        public bool d { get; set; }
        public short e { get; set; }
        public ushort f { get; set; }
        public long g { get; set; }
        public ulong h { get; set; }
        public float i { get; set; }
        public float j { get; set; }
        public double k { get; set; }
        public double l { get; set; }
        public decimal m { get; set; }
        public decimal n { get; set; }
        public string o { get; set; }
        public string p { get; set; }
        public YesNoEnum q { get; set; }
        public DateOnly r { get; set; }
        public TimeOnly s { get; set; }
        public DateTime t { get; set; }
        public DateTime u { get; set; }
        public string v { get; set; }
        public byte[] w { get; set; }
    }

    public class TestModelB : MerlinModelBase
    {
        public int a { get; set; }

        [AutoPopSettings("b", "0")]
        public sbyte AutoPop { get; set; }
        public byte? c { get; set; }
        public bool? d { get; set; }
        public short? e { get; set; }
        public ushort? f { get; set; }
        public long? g { get; set; }
        public ulong? h { get; set; }
        public float? i { get; set; }
        public float? j { get; set; }
        public double? k { get; set; }
        public double? l { get; set; }
        public decimal? m { get; set; }
        public decimal? n { get; set; }
        public string? o { get; set; }
        public string? p { get; set; }
        public YesNoEnum? q { get; set; }
        public DateOnly? r { get; set; }
        public TimeOnly? s { get; set; }
        public DateTime? t { get; set; }
        public DateTime? u { get; set; }
        public string? v { get; set; }
        public byte[]? w { get; set; }
    }

    public class TestModelC: MerlinModelBase
    {
        public int a { get; set; }

        [AutoPopSettings("b", "0")]
        public int AutoPop { get; set; }
        public int? c { get; set; }
        public int? d { get; set; }
        public int? e { get; set; }
        public int? f { get; set; }
        public int? g { get; set; }
        public int? h { get; set; }
        public float? i { get; set; }
        public float? j { get; set; }
        public double? k { get; set; }
        public double? l { get; set; }
        public decimal? m { get; set; }
        public decimal? n { get; set; }
        public string? o { get; set; }
        public string? p { get; set; }
        public YesNoEnum? q { get; set; }
        public DateOnly? r { get; set; }
        public TimeOnly? s { get; set; }
        public DateTime? t { get; set; }
        public DateTime? u { get; set; }
        public string? v { get; set; }
        public byte[]? w { get; set; }
    }
}
