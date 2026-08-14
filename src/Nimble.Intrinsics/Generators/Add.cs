using System.Text;

namespace Nimble.Intrinsics.Generators;

internal partial class Generator
{
    private static readonly AddTypeInfo[] AddTypes =
    [
        new("byte",   "Avx2", "Avx", "Sse2", "Sse2", false),
        new("sbyte",  "Avx2", "Avx", "Sse2", "Sse2", false),
        new("short",  "Avx2", "Avx", "Sse2", "Sse2", false),
        new("ushort", "Avx2", "Avx", "Sse2", "Sse2", false),
        new("int",    "Avx2", "Avx", "Sse2", "Sse2", true),
        new("uint",   "Avx2", "Avx", "Sse2", "Sse2", true),
        new("long",   "Avx2", "Avx", "Sse2", "Sse2", true),
        new("ulong",  "Avx2", "Avx", "Sse2", "Sse2", true),
        new("float",  "Avx",  "Avx", "Sse",  "Sse",  true),
        new("double", "Avx",  "Avx", "Sse2", "Sse2", true),
    ];

    private static string GenerateAdd()
    {
        StringBuilder source = new StringBuilder();

        source.AppendLine(CORE_HEADER);

        source.AppendLine($$"""
                #region Add(T* input, T value, T* output, int length)

            """);

        foreach (AddTypeInfo type in AddTypes) GenerateBroadcastAdd(source, type);

        source.AppendLine("""
                #endregion

                #region Add(T* input, T* values, T* output, int length)

            """);

        foreach (AddTypeInfo type in AddTypes) GenerateSxsAdd(source, type);

        source.AppendLine("""
                #endregion
            }
            """);

        return source.ToString();
    }

    private static void GenerateBroadcastAdd(StringBuilder source, AddTypeInfo type)
    {
        source.AppendLine($$"""
                public static void Add({{type.Name}}* input, [ConstantExpected] {{type.Name}} value, {{type.Name}}* output, int length)
                {
                    int i = 0;

            #if NET8_0_OR_GREATER
            """);

        if (type.HasAvx512) source.AppendLine($$"""
                    const int v512 = 512 / (sizeof({{type.Name}}) * 8);

                    if (Avx512F.IsSupported && (length - i) >= v512)
                    {
                        Vector512<{{type.Name}}> values = Vector512.Create(value);
                
                        for (; i <= length - v512; i += v512) Avx512F.Store(output + i, Avx512F.Add(Avx512F.LoadVector512(input + i), values));
                    }

            """);

        source.AppendLine($$"""
                    const int v256 = 256 / (sizeof({{type.Name}}) * 8);

                    if ({{type.AvxFeature}}.IsSupported && (length - i) >= v256)
                    {
                        Vector256<{{type.Name}}> values = Vector256.Create(value);

                        for (; i <= length - v256; i += v256) {{type.AvxOperations}}.Store(output + i, {{type.AvxFeature}}.Add({{type.AvxOperations}}.LoadVector256(input + i), values));
                    }

                    const int v128 = 128 / (sizeof({{type.Name}}) * 8);

                    if ({{type.SseFeature}}.IsSupported && (length - i) >= v128)
                    {
                        Vector128<{{type.Name}}> values = Vector128.Create(value);

                        for (; i <= length - v128; i += v128) {{type.SseOperations}}.Store(output + i, {{type.SseFeature}}.Add({{type.SseOperations}}.LoadVector128(input + i), values));
                    }
            #endif

                    for (; i < length; i++) output[i] = ({{type.Name}})(input[i] + value);
                }

            """);
    }

    private static void GenerateSxsAdd(StringBuilder source, AddTypeInfo type)
    {
        source.AppendLine($$"""
            public static void Add({{type.Name}}* input, {{type.Name}}* values, {{type.Name}}* output, int length)
            {
                int i = 0;

        #if NET8_0_OR_GREATER
        """);

        if (type.HasAvx512)
        {
            source.AppendLine($$"""
                const int v512 = 512 / (sizeof({{type.Name}}) * 8);

                if (Avx512F.IsSupported && (length - i) >= v512) for (; i <= length - v512; i += v512) Avx512F.Store(output + i, Avx512F.Add(Avx512F.LoadVector512(input + i), Avx512F.LoadVector512(values + i)));

        """);
        }

        source.AppendLine($$"""
                const int v256 = 256 / (sizeof({{type.Name}}) * 8);

                if ({{type.AvxFeature}}.IsSupported && (length - i) >= v256) for (; i <= length - v256; i += v256) {{type.AvxOperations}}.Store(output + i, {{type.AvxFeature}}.Add({{type.AvxOperations}}.LoadVector256(input + i), {{type.AvxOperations}}.LoadVector256(values + i)));

                const int v128 = 128 / (sizeof({{type.Name}}) * 8);

                if ({{type.SseFeature}}.IsSupported && (length - i) >= v128) for (; i <= length - v128; i += v128) {{type.SseOperations}}.Store(output + i, {{type.SseFeature}}.Add({{type.SseOperations}}.LoadVector128(input + i), {{type.SseOperations}}.LoadVector128(values + i)));
        #endif

                for (; i < length; i++) output[i] = ({{type.Name}})(input[i] + values[i]);
            }

        """);
    }

    private readonly struct AddTypeInfo(string name, string avxFeature, string avxOperations, string sseFeature, string sseOperations, bool hasAvx512)
    {
        public string Name { get; } = name;
        public string AvxFeature { get; } = avxFeature;
        public string AvxOperations { get; } = avxOperations;
        public string SseFeature { get; } = sseFeature;
        public string SseOperations { get; } = sseOperations;
        public bool HasAvx512 { get; } = hasAvx512;
    }
}