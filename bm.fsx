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
    let requestError = ref 0
    let currentRequest = ref 0
    let requestDone = ref 0

    let requestCompleted (cs: TaskCompletionSource<bool>) (http: HttpClient) =
                ignore <| Interlocked.Decrement currentRequest
                match Interlocked.Decrement requestDone with
                | 0 -> http.Dispose()
                       ignore <| cs.TrySetResult true
                       printf "Disposed!"
                | _ -> ()

    let rec spamRequest cs (http: HttpClient) (uri: Uri) =
        async {
                let! response = Async.AwaitTask <| http.GetAsync uri
                ignore <| Interlocked.Increment requestCount
                ignore <| Interlocked.Increment currentRequest
                match response.StatusCode with
                | HttpStatusCode.OK -> let! s = Async.AwaitTask <| response.Content.ReadAsStringAsync()
                                       requestCompleted cs http
                | _ -> ignore <| Interlocked.Increment requestError
                       requestCompleted cs http
              }

    member this.RequestedCount with get() = !requestCount
    member this.ErrorCount with get() = !requestError

    member this.requestGet uri n =
                requestDone := n
                let cs = TaskCompletionSource<bool>()
                let result = Async.AwaitTask <| cs.Task

                async {
                    let http = new HttpClient()
                    for i = 1 to n do
                        Async.Start <| spamRequest cs http uri
                } |> Async.Start

                result

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
    tester.requestGet countUri reqcount
    |> Async.RunSynchronously
    |> ignore
    watch.Stop()

    printfn "Stress test took %d ms for %d requests" watch.ElapsedMilliseconds reqcount
    printfn "%d request has been performed.  %d errors has been occured" tester.RequestedCount tester.ErrorCount

startBenchmark "http://ruxoz.net:8888/"