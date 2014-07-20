module StressTester.HttpRequesters

open System
open System.Net
open System.Net.Http

type HttpSuccessStatus = HttpSuccessStatus of bool
type HttpResponseCreator = unit -> Async<HttpSuccessStatus * string>
type HttpRequestCreator =
    abstract GetAsync: Uri -> Async<HttpResponseCreator>

type DefaultHttpGet() =
    interface HttpRequestCreator with
        override this.GetAsync uri =
                async {
                    let client = new HttpClient()
                    let! response = Async.AwaitTask <| client.GetAsync uri
                    return fun () -> 
                        async {
                            try
                                match response.StatusCode with
                                | HttpStatusCode.OK -> 
                                        let! content = Async.AwaitTask <| response.Content.ReadAsStringAsync()
                                        return (HttpSuccessStatus true), content
                                | code ->
                                        return (HttpSuccessStatus false), (sprintf "HTTP Response from %A failed with %A" uri code)
                            finally
                                client.Dispose()
                        }
                }
