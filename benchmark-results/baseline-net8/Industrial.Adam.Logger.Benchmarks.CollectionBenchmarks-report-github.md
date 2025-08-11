```

BenchmarkDotNet v0.13.12, Debian GNU/Linux 12 (bookworm)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.411
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX2
  Job-UDQIVF : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX2

IterationCount=10  WarmupCount=3  

```
| Method                     | Mean        | Error       | StdDev      | Gen0   | Allocated |
|--------------------------- |------------:|------------:|------------:|-------:|----------:|
| ConcurrentDictionaryLookup | 31,388.1 ns | 4,624.73 ns | 2,752.10 ns |      - |         - |
| RegularDictionaryWithLock  | 59,930.6 ns | 4,401.21 ns | 2,911.13 ns |      - |         - |
| StringConcatenation        |    132.8 ns |    63.68 ns |    42.12 ns | 0.0114 |      48 B |
| StringInterpolation        |    148.1 ns |    18.32 ns |    10.90 ns | 0.0172 |      72 B |
| ConcurrentQueueOperations  |  5,045.9 ns |   504.95 ns |   264.10 ns | 0.6256 |    2624 B |
