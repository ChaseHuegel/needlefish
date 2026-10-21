```

BenchmarkDotNet v0.15.2, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD Ryzen 5 3600X 4.41GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.112
  [Host]     : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX2


```
| Type                            | Method        | People | StringLength | Mean         | Error      | StdDev     | Median       | Gen0   | Gen1   | Allocated |
|-------------------------------- |-------------- |------- |------------- |-------------:|-----------:|-----------:|-------------:|-------:|-------:|----------:|
| **DeserializePrimitivesBenchmarks** | **Unpack**        | **?**      | **?**            |     **28.33 ns** |   **0.593 ns** |   **0.812 ns** |     **28.19 ns** |      **-** |      **-** |         **-** |
| DeserializePrimitivesBenchmarks | Deserialize   | ?      | ?            |     30.89 ns |   0.612 ns |   0.572 ns |     30.79 ns |      - |      - |         - |
| **SerializeNestedBenchmarks**       | **GetSize**       | **16**     | **?**            |    **111.83 ns** |   **2.256 ns** |   **3.162 ns** |    **112.31 ns** |      **-** |      **-** |         **-** |
| SerializeNestedBenchmarks       | SerializeInto | 16     | ?            |    951.35 ns |  18.765 ns |  34.782 ns |    952.93 ns |      - |      - |         - |
| SerializeNestedBenchmarks       | Serialize     | 16     | ?            |  1,262.31 ns |  24.834 ns |  47.249 ns |  1,262.02 ns | 0.4120 |      - |    3448 B |
| **DeserializeExhaustiveBenchmarks** | **Unpack_Fresh**  | **?**      | **16**           |    **300.15 ns** |   **6.051 ns** |  **11.802 ns** |    **301.02 ns** | **0.0763** |      **-** |     **640 B** |
| SerializeExhaustiveBenchmarks   | GetSize       | ?      | 16           |     13.76 ns |   0.307 ns |   0.504 ns |     13.65 ns |      - |      - |         - |
| DeserializeExhaustiveBenchmarks | Unpack_Reused | ?      | 16           |    310.43 ns |   6.218 ns |  13.907 ns |    307.78 ns | 0.0763 |      - |     640 B |
| SerializeExhaustiveBenchmarks   | SerializeInto | ?      | 16           |     72.57 ns |   0.907 ns |   0.708 ns |     72.70 ns |      - |      - |         - |
| DeserializeExhaustiveBenchmarks | Deserialize   | ?      | 16           |    324.69 ns |   6.494 ns |  15.807 ns |    324.43 ns | 0.0763 | 0.0029 |     640 B |
| SerializeExhaustiveBenchmarks   | Serialize     | ?      | 16           |    146.25 ns |   5.040 ns |  14.780 ns |    149.57 ns | 0.0496 |      - |     416 B |
| **SerializeNestedBenchmarks**       | **GetSize**       | **128**    | **?**            |    **935.66 ns** |  **18.361 ns** |  **30.677 ns** |    **921.89 ns** |      **-** |      **-** |         **-** |
| SerializeNestedBenchmarks       | SerializeInto | 128    | ?            |  7,441.16 ns | 145.673 ns | 262.679 ns |  7,360.94 ns |      - |      - |         - |
| SerializeNestedBenchmarks       | Serialize     | 128    | ?            | 10,286.51 ns | 203.450 ns | 361.633 ns | 10,304.31 ns | 3.2959 |      - |   27752 B |
| **DeserializeExhaustiveBenchmarks** | **Unpack_Fresh**  | **?**      | **1024**         |    **545.12 ns** |  **21.405 ns** |  **62.778 ns** |    **527.98 ns** | **0.5579** | **0.0057** |    **4672 B** |
| SerializeExhaustiveBenchmarks   | GetSize       | ?      | 1024         |     13.50 ns |   0.301 ns |   0.586 ns |     13.39 ns |      - |      - |         - |
| DeserializeExhaustiveBenchmarks | Unpack_Reused | ?      | 1024         |    636.42 ns |  30.636 ns |  81.242 ns |    627.37 ns | 0.5579 | 0.0095 |    4672 B |
| SerializeExhaustiveBenchmarks   | SerializeInto | ?      | 1024         |  1,480.25 ns |  29.207 ns |  74.341 ns |  1,476.64 ns |      - |      - |         - |
| DeserializeExhaustiveBenchmarks | Deserialize   | ?      | 1024         |    594.05 ns |  16.869 ns |  49.738 ns |    592.67 ns | 0.5579 | 0.1392 |    4672 B |
| SerializeExhaustiveBenchmarks   | Serialize     | ?      | 1024         |  1,829.47 ns |  49.084 ns | 134.366 ns |  1,840.99 ns | 0.5302 |      - |    4448 B |
