[<AutoOpen>]
module Seq

// Returns a sequence that yields chunks of length n.
// Each chunk is returned as a list.
let split length (xs: seq<'T>) =
    let rec loop xs =
        [
            yield Seq.truncate length xs |> Seq.toList
            match Seq.length xs <= length with
            | false -> yield! loop (Seq.skip length xs)
            | true -> ()
        ]
    loop xs