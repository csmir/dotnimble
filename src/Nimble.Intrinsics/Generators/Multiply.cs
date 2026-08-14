using System.Text;

namespace Nimble.Intrinsics.Generators;

internal partial class Generator
{
    private static readonly MultiplyTypeInfo[] MultiplyTypes = [new("float",  "Sse"), new("double", "Sse2")];

    private static string GenerateMultiply()
    {
        StringBuilder source = new StringBuilder();

        source.AppendLine(CORE_HEADER);

        source.AppendLine($$"""
                #region Multiply(T* input, T value, T* output, int length)

            """);

        foreach (MultiplyTypeInfo type in MultiplyTypes) GenerateBroadcastMultiply(source, type);

        source.AppendLine("""
                #endregion

                #region Multiply(T* input, T* values, T* output, int length)

            """);

        foreach (MultiplyTypeInfo type in MultiplyTypes) GenerateSxsMultiply(source, type);

        source.AppendLine("""
                #endregion
            }
            """);

        return source.ToString();
    }

    private static void GenerateBroadcastMultiply(StringBuilder source, MultiplyTypeInfo type) => source.AppendLine($$"""
                public static void Multiply({{type.Name}}* input, [ConstantExpected] {{type.Name}} value, {{type.Name}}* output, int length)
                {
                    int i = 0;

            #if NET8_0_OR_GREATER
                    const int v512 = 512 / (sizeof({{type.Name}}) * 8);
                    const int v256 = 256 / (sizeof({{type.Name}}) * 8);
                    const int v128 = 128 / (sizeof({{type.Name}}) * 8);

                    if (Avx512F.IsSupported && (length - i) >= v512)
                    {
                        Vector512<{{type.Name}}> values = Vector512.Create(value);

                        for (; i <= length - v512; i += v512) Avx512F.Store(output + i, Avx512F.Multiply(Avx512F.LoadVector512(input + i), values));
                    }

                    if (Avx.IsSupported && (length - i) >= v256)
                    {
                        Vector256<{{type.Name}}> values = Vector256.Create(value);

                        for (; i <= length - v256; i += v256) Avx.Store(output + i, Avx.Multiply(Avx.LoadVector256(input + i), values));
                    }

                    if ({{type.SseMode}}.IsSupported && (length - i) >= v128)
                    {
                        Vector128<{{type.Name}}> values = Vector128.Create(value);

                        for (; i <= length - v128; i += v128) {{type.SseMode}}.Store(output + i, {{type.SseMode}}.Multiply({{type.SseMode}}.LoadVector128(input + i), values));
                    }
            #endif

                    for (; i < length; i++) output[i] = ({{type.Name}})input[i] * value;
                }

            """);

    private static void GenerateSxsMultiply(StringBuilder source, MultiplyTypeInfo type) => source.AppendLine($$"""
                public static void Multiply({{type.Name}}* input, {{type.Name}}* values, {{type.Name}}* output, int length)
                {
                    int i = 0;

            #if NET8_0_OR_GREATER
                    const int v512 = 512 / (sizeof({{type.Name}}) * 8);
                    const int v256 = 256 / (sizeof({{type.Name}}) * 8);
                    const int v128 = 128 / (sizeof({{type.Name}}) * 8);

                    if (Avx512F.IsSupported && (length - i) >= v512) for (; i <= length - v512; i += v512) Avx512F.Store(output + i, Avx512F.Multiply(Avx512F.LoadVector512(input + i), Avx512F.LoadVector512(values + i)));

                    if (Avx.IsSupported && (length - i) >= v256) for (; i <= length - v256; i += v256) Avx.Store(output + i, Avx.Multiply(Avx.LoadVector256(input + i), Avx.LoadVector256(values + i)));

                    if ({{type.SseMode}}.IsSupported && (length - i) >= v128) for (; i <= length - v128; i += v128) {{type.SseMode}}.Store(output + i, {{type.SseMode}}.Multiply({{type.SseMode}}.LoadVector128(input + i), {{type.SseMode}}.LoadVector128(values + i)));
            #endif
            
                    for (; i < length; i++) output[i] = ({{type.Name}})input[i] * values[i];
                }
            
            """);

    private readonly struct MultiplyTypeInfo(string name, string? sseMode)
    {
        public string  Name { get; } = name;
        public string? SseMode { get; } = sseMode;
    }
}
