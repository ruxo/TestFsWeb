open WebModules

open System
open System.Linq
open System.Reflection
open Nancy
open Nancy.Bootstrapper
open Nancy.Hosting.Self

[<EntryPoint>]
let main argv = 
    use bmUninit = Benchmark.initialize()

    StaticConfiguration.DisableErrorTraces <- false

    let config = HostConfiguration(
                    UrlReservations = UrlReservations(CreateAutomatically = true)
                 )

    let uri = Uri("http://localhost:8888")
    use host = new NancyHost(config, uri)
    host.Start()

    printfn "Host is started on %A" uri
    printfn "Press ENTER to end..."
    ignore <| Console.ReadLine()
    0
