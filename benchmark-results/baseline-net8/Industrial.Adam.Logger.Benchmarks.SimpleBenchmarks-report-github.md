```

BenchmarkDotNet v0.13.12, Debian GNU/Linux 12 (bookworm)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.411
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX2
  Job-DPNQEG : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX2

IterationCount=5  WarmupCount=3  

```
| Method                     | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|--------------------------- |-------------:|--------------:|--------------:|------:|--------:|--------:|----------:|------------:|
| StringConcatenation        | 509,009.2 ns | 721,219.31 ns | 187,298.36 ns | 1.000 |    0.00 | 19.5313 |   88001 B |       1.000 |
| ConcurrentDictionaryLookup | 264,155.4 ns |  68,375.19 ns |  17,756.82 ns | 0.583 |    0.23 | 11.2305 |   48000 B |       0.545 |
| ProcessReading             |     304.4 ns |      66.48 ns |      17.26 ns | 0.001 |    0.00 |  0.0763 |     320 B |       0.004 |
