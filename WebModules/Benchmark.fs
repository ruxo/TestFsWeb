module Benchmark

open System
open System.Threading
open System.Diagnostics

[<Measure>]
type ms // Milliseconds

let mutable lastCpuCheck = 0L<ms>
let mutable (lastUpdate: DateTime option) = None

let benchmarkCpu (argN: obj) =
    let n = argN :?> int
    let watch = Stopwatch.StartNew()
    Thread.SpinWait n
    lastCpuCheck <- watch.ElapsedMilliseconds * 1L<ms>
    lastUpdate <- Some DateTime.UtcNow

let initialize() =
    let timer = new Timer(benchmarkCpu, 1000*1000, (TimeSpan.FromSeconds 0.0), (TimeSpan.FromSeconds 10.0))
    timer :> IDisposable

let getLastCpuCheck() = lastCpuCheck, lastUpdate