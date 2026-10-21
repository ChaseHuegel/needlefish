```

BenchmarkDotNet v0.15.2, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD Ryzen 5 3600X 4.41GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.112
  [Host]     : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX2


```
| Type                            | Method        | People | StringLength | Mean        | Error      | StdDev     | Median      | Gen0   | Gen1   | Allocated |
|-------------------------------- |-------------- |------- |------------- |------------:|-----------:|-----------:|------------:|-------:|-------:|----------:|
| **DeserializePrimitivesBenchmarks** | **Unpack**        | **?**      | **?**            |    **22.17 ns** |   **0.475 ns** |   **0.726 ns** |    **22.38 ns** |      **-** |      **-** |         **-** |
| DeserializePrimitivesBenchmarks | Deserialize   | ?      | ?            |    22.72 ns |   0.421 ns |   0.533 ns |    22.67 ns |      - |      - |         - |
| **SerializeNestedBenchmarks**       | **GetSize**       | **16**     | **?**            |   **103.78 ns** |   **2.084 ns** |   **4.066 ns** |   **102.38 ns** |      **-** |      **-** |         **-** |
| SerializeNestedBenchmarks       | SerializeInto | 16     | ?            |   717.19 ns |  13.327 ns |  25.356 ns |   705.65 ns |      - |      - |         - |
| SerializeNestedBenchmarks       | Serialize     | 16     | ?            | 1,058.82 ns |  24.000 ns |  68.472 ns | 1,060.70 ns | 0.4120 |      - |    3448 B |
| **DeserializeExhaustiveBenchmarks** | **Unpack_Fresh**  | **?**      | **16**           |   **260.72 ns** |   **5.454 ns** |  **15.822 ns** |   **257.23 ns** | **0.0763** |      **-** |     **640 B** |
| SerializeExhaustiveBenchmarks   | GetSize       | ?      | 16           |    12.57 ns |   0.279 ns |   0.545 ns |    12.78 ns |      - |      - |         - |
| DeserializeExhaustiveBenchmarks | Unpack_Reused | ?      | 16           |   296.47 ns |   5.896 ns |   8.825 ns |   298.17 ns | 0.0763 |      - |     640 B |
| SerializeExhaustiveBenchmarks   | SerializeInto | ?      | 16           |    62.97 ns |   1.263 ns |   1.890 ns |    63.29 ns |      - |      - |         - |
| DeserializeExhaustiveBenchmarks | Deserialize   | ?      | 16           |   274.07 ns |   5.540 ns |  14.692 ns |   275.94 ns | 0.0763 | 0.0029 |     640 B |
| SerializeExhaustiveBenchmarks   | Serialize     | ?      | 16           |    97.15 ns |   1.993 ns |   4.539 ns |    96.87 ns | 0.0497 |      - |     416 B |
| **SerializeNestedBenchmarks**       | **GetSize**       | **128**    | **?**            |   **945.08 ns** |  **18.905 ns** |  **40.289 ns** |   **952.36 ns** |      **-** |      **-** |         **-** |
| SerializeNestedBenchmarks       | SerializeInto | 128    | ?            | 5,956.72 ns | 114.168 ns | 144.386 ns | 5,964.92 ns |      - |      - |         - |
| SerializeNestedBenchmarks       | Serialize     | 128    | ?            | 7,936.31 ns | 236.518 ns | 670.963 ns | 7,654.34 ns | 3.2959 |      - |   27752 B |
| **DeserializeExhaustiveBenchmarks** | **Unpack_Fresh**  | **?**      | **1024**         |   **473.87 ns** |   **9.381 ns** |  **21.175 ns** |   **476.03 ns** | **0.5579** | **0.0057** |    **4672 B** |
| SerializeExhaustiveBenchmarks   | GetSize       | ?      | 1024         |    12.54 ns |   0.281 ns |   0.514 ns |    12.68 ns |      - |      - |         - |
| DeserializeExhaustiveBenchmarks | Unpack_Reused | ?      | 1024         |   468.97 ns |   6.301 ns |   6.189 ns |   468.83 ns | 0.5579 | 0.0095 |    4672 B |
| SerializeExhaustiveBenchmarks   | SerializeInto | ?      | 1024         |    89.58 ns |   1.835 ns |   3.402 ns |    89.38 ns |      - |      - |         - |
| DeserializeExhaustiveBenchmarks | Deserialize   | ?      | 1024         |   529.37 ns |  10.479 ns |  13.252 ns |   526.90 ns | 0.5579 | 0.1392 |    4672 B |
| SerializeExhaustiveBenchmarks   | Serialize     | ?      | 1024         |   291.56 ns |   5.817 ns |  11.751 ns |   293.13 ns | 0.5312 |      - |    4448 B |
