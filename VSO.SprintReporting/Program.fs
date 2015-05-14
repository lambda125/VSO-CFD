
[<EntryPoint>]
let main argv = 

    printfn "Logging in to VSO..."

    Auth.login ()
    |> VSO.downloadWorkItems
    |> Async.RunSynchronously
    |> Reporting.summmarise
    |> Reporting.dedupe

    printfn "Done. Press [ENTER] to exit..."

    System.Console.ReadLine() |> ignore

    0
