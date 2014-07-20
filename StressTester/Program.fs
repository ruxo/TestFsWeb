open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent
open System.Diagnostics
open System.Net
open System.Net.Http
open StressTester.Testers

let uriText = "http://localhost:8888/"
let reqcount = 1000;

let report (tester: MyFirstTester) =
    Console.Clear()
    printfn "%A" DateTime.Now
    tester.Progress
    |> Map.iter (fun name value -> printfn "%s = %d" name value)

let startBenchmark httpFactory uriText =
    let uri = Uri(uriText)
    let resetUri = Uri(uri, "reset/")
    let countUri = Uri(uri, "count/")

    let verifyTestServer() =
        use http = new HttpClient()
        let x = http.GetStringAsync(countUri).Result
        let ok, _ = Int32.TryParse(x)
        if not ok then failwithf "CountURI response is not recognized: %s" x

        ignore <| http.GetStringAsync(resetUri).Result

        let v = http.GetStringAsync(countUri).Result
        if v <> "1" then failwithf "ResetURI doesn't work correctly, count is not set to 1 but to %s" v

    verifyTestServer()

    let tester = MyFirstTester httpFactory

    let watch = Stopwatch.StartNew()
    let spamTask = tester.requestGet countUri reqcount
    while not <| spamTask.Wait(1000) do
        report tester
    watch.Stop()

    report tester
    printfn "Stress test took %d ms for %d requests" watch.ElapsedMilliseconds reqcount


[<EntryPoint>]
let main argv = 
    startBenchmark (StressTester.HttpRequesters.DefaultHttpGet()) "http://localhost:8888/" 
    0 // return an integer exit code
