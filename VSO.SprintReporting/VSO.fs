module VSO

open VSO.OAuth.UI
open Newtonsoft.Json
open VisualStudioOnline.Api.Rest.V1.Client
open System
open System.IO
open System.Configuration
open VisualStudioOnline.Api.Rest.V1.Model

let downloadWorkItems (asyncToken:Async<OAuthTokenInfo>) =

    let statusFlow = File.ReadAllLines "status-workflow.txt"

    let config = ConfigurationManager.AppSettings
    let accountName = config.["AccountName"]
    let projectName = config.["ProjectName"]

    async {

        let! token = asyncToken

        let vso = VsoClient (accountName, token.AccessToken)
        let wit = vso.GetService<IVsoWit>()

        let! query = wit.GetQuery(projectName, config.["QueryPath"]) |> Async.AwaitTask
//        let queryText =
//            "Select [System.Id]
//            From WorkItems 
//            Where ([System.WorkItemType] = 'Product Backlog Item' OR [System.WorkItemType] = 'Bug')
//                AND [State] <> 'Closed' 
//                AND [State] <> 'Removed'
//            Order by [Microsoft.VSTS.Common.Priority] asc, [System.CreatedDate] desc"

        let! items = 
            wit.RunFlatQuery(projectName, query) |> Async.AwaitTask

        let serializeTo filePrefix data =
            JsonConvert.SerializeObject(data)
            |> (fun contents -> 
                    let filename = sprintf "%s-%s.txt" filePrefix (DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fffffff"))
                    File.WriteAllText (filename, contents)
                    contents
                )
            |> ignore

        printfn "Got %d results. Getting work items..." items.WorkItems.Count

        let fetchWorkItems ids =
            async {
                let! workItems = 
                    wit.GetWorkItems(
                        ids, 
                        fields = [| "System.Id"; "System.Tags"; "State"; "Microsoft.VSTS.Scheduling.Effort" |]) 
                    |> Async.AwaitTask
                printfn "Got %d work item results..." workItems.Count
                workItems |> serializeTo "WorkItems"
                return workItems
            }

        let allWorkItems = 
            items.WorkItems
            |> Seq.map (fun w -> w.Id)
            |> Seq.split 200
            |> Seq.map (fun ids -> fetchWorkItems (ids |> Seq.toArray))
            |> Async.Parallel

        return! allWorkItems
    }
