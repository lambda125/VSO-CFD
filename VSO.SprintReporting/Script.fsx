(*
[1..100]
|> Seq.windowed (20)
|> Seq.map (fun l -> printfn "# %d. First: %d. Last: %d" l.Length (l |> Seq.head) (l |> Seq.last))
*)
open System.Linq

let workflowTags = [|"todo";"in progress"|]

let filtered = 
    [|"Blocked"; "todo"|]
    |> Seq.filter (fun t -> 
        printfn "Checking tag %s." t
        workflowTags.Contains(t)) //get only tags that are in our valid workflow statuses (see status-workflow.txt)
    |> Seq.toList

filtered;;
