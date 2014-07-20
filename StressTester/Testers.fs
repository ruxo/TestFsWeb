module StressTester.Testers

open System
open System.Threading
open System.Net
open System.Net.Http
open StressTester.HttpRequesters

type MyFirstTester(httpFactory: HttpRequestCreator) =
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
                ignore <| Interlocked.Increment requestCount
                ignore <| Interlocked.Increment currentRequest
                failwithf "Test faiure"
                let! response = httpFactory.GetAsync uri
                ignore <| Interlocked.Increment responseCount
                try
                    let! success, content = response()
                    match success with
                    | HttpSuccessStatus(false) -> ignore <| Interlocked.Increment requestError
                    | HttpSuccessStatus(true) -> ()
                finally
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

