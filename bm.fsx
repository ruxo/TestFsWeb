#r "System.Net.Http.dll"

open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent
open System.Diagnostics
open System.Net
open System.Net.Http

let uriText = "http://localhost:8888/"
let reqcount = 10*1000;

type StressTester() =
    let requestCount = ref 0
    let responseCount = ref 0
    let requestError = ref 0
    let currentRequest = ref 0
    let requestDone = ref 0

    let finishRequest (finish: EventWaitHandle) =
       ignore <| Interlocked.Decrement currentRequest
       match Interlocked.Decrement requestDone with
       | 0 -> ignore <| finish.Set()
       | _ -> ()

    let rec spamRequest finish (uri: Uri) =
        async {
                use http = new HttpClient()
                ignore <| Interlocked.Increment requestCount
                ignore <| Interlocked.Increment currentRequest
                let! response = Async.AwaitTask <| http.GetAsync uri
                ignore <| Interlocked.Increment responseCount
                match response.StatusCode with
                | HttpStatusCode.OK -> let! s = Async.AwaitTask <| response.Content.ReadAsStringAsync()
                                       finishRequest finish
                | _ -> ignore <| Interlocked.Increment requestError
                       finishRequest finish
              }

    member this.Progress with get() = Map [
                                              "request", !requestCount
                                              "response", !responseCount
                                              "ongoing", !currentRequest
                                              "left", !requestDone
                                              "error", !requestError
                                          ]

    member this.requestGet uri n =
                requestDone := n

                async {
                    use finish = new ManualResetEvent false
                    for i = 1 to n do
                        spamRequest finish uri |> Async.Start
                    ignore <| finish.WaitOne()
                } |> Async.StartAsTask

let report (tester: StressTester) =
    Console.Clear()
    printfn "%A" DateTime.Now
    tester.Progress
    |> Map.iter (fun name value -> printfn "%s = %d" name value)

let startBenchmark uriText =
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

    let tester = StressTester()

    let watch = Stopwatch.StartNew()
    let spamTask = tester.requestGet countUri reqcount
    while not <| spamTask.Wait(1000) do
        report tester
    watch.Stop()

    report tester
    printfn "Stress test took %d ms for %d requests" watch.ElapsedMilliseconds reqcount

startBenchmark "http://ruxoz.net:8888/"
