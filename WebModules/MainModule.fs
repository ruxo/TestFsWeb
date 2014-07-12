module WebModules

open System
open System.Threading
open Nancy

let count = ref 0
let inline (=>) a b = a, box b

type CpuInfo = { bmms: int64; last_update: string }

type MainModule() as this =
    inherit NancyModule()

    do
        this.Get.["/"] <- (fun _ -> dict[
                                            "link" => dict["count" => "/count"
                                                           "reset" => "/reset"]
                                        ]
                                    :> obj)
        this.Get.["count/"] <- (fun _ -> let myCount = Interlocked.Increment count
                                         myCount.ToString(System.Globalization.CultureInfo.InvariantCulture) :> obj)
        this.Get.["reset/"] <- (fun _ ->
                                    ignore <| Interlocked.Exchange(count, 0)
                                    200 :> obj
                               )
        this.Get.["cpu/"] <- (fun _ ->
                                    let bmms, timestamp = Benchmark.getLastCpuCheck()
                                    {
                                        bmms=(int64 bmms)
                                        last_update=(if timestamp.IsNone then null else timestamp.Value.ToString())
                                    } :> obj
                             )
