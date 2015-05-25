module Reporting

open VisualStudioOnline.Api.Rest
open VisualStudioOnline.Api.Rest.V1.Model
open System
open System.IO
open System.Linq
open System.Collections.Generic

let summmarise (data: JsonCollection<WorkItem>[]) =
    let workflowTags = 
        File.ReadAllLines "status-workflow.txt"
        |> Seq.filter (fun s -> not (System.String.IsNullOrWhiteSpace(s)))
        |> Seq.map (fun s -> s.Trim().ToLower())
        |> Seq.toArray

    (*
        tags are returned as fields in a work item as follows:

        {
            "fields": {
                "System.Id": 2023,
                "System.Tags": "authorisation; feature"
            },
            "_links": null,
            "relations": [],
            "rev": 4,
            "id": 2023,
            "url": "https://blah.visualstudio.com/DefaultCollection/_apis/wit/workItems/2023"
        }
    *)
    let getTagsFromFieldValue (tagOption: KeyValuePair<string,obj> option) =
        if tagOption.IsNone || tagOption.Value.Value = null
        then [||]
        else
            let tags = tagOption.Value.Value.ToString()
            let t = tags.Split([|";"|], StringSplitOptions.RemoveEmptyEntries)
            t 
            |> Seq.map (fun s -> s.Trim()) 
            |> Seq.toArray

    let tag (item:WorkItem) =
        item.Fields 
        |> Seq.tryFind (fun f -> f.Key = "System.Tags") 
        |> getTagsFromFieldValue
        |> Seq.filter (fun t -> workflowTags.Contains(t)) //get only tags that are in our valid workflow statuses (see status-workflow.txt)
        |> Seq.map(fun t -> Array.IndexOf(workflowTags, t), t) //find the index of the item and get a tuple of (index,tag)
        |> Seq.sortBy (fun tt -> fst tt) //sort by index
        |> (fun tags -> 
                let tagList = Seq.toList tags
                if Seq.isEmpty tagList
                then 0,"" 
                else Seq.last tagList) //get the last tag tuple
        |> snd //get the tag

    let effort (item:WorkItem) = 
        item.Fields
        |> Seq.tryFind (fun f -> f.Key = "Microsoft.VSTS.Scheduling.Effort")
        |> (fun f -> 
                if f.IsNone || f.Value.Value = null
                then 
                    0
                else 
                    let effortString = f.Value.Value.ToString()
                    match System.Double.TryParse effortString with
                    | true, e -> int e
                    | _ -> 0
           )

    let itemsByTag = 
        data
        |> Seq.map (fun d -> d.Items)
        |> Seq.concat
        |> Seq.groupBy tag
        |> Seq.filter (fun (tag, items) -> tag <> null && tag.Trim().Length > 0)

    let itemSummary = 
        itemsByTag
        |> Seq.map (fun (tag, items) -> 
                let itemIds = items |> Seq.map (fun i -> i.Id) |> Seq.toArray
                let idString = System.String.Join(";", itemIds)

                let count = items |> Seq.length
                let storyPoints = items |> Seq.sumBy effort
                sprintf "%s,%s,%d,%d,%s" (DateTime.Now.ToString("yyyy-MM-dd")) tag count storyPoints idString
            )

    let filename = "summary.csv"
    File.AppendAllLines(filename, itemSummary)

    filename

type Item = {
    Date : DateTime
    Status : string
    NumberOfItems: int
    StoryPoints : int
}

let dedupe file = 
    //Take in the file data
    let lines = File.ReadAllLines file
    
    //parse
    let parse (s:string) =
        let fields = s.Split([|","|], StringSplitOptions.None)
        
        let date = 
            match DateTime.TryParse fields.[0] with
            | true, d -> Some d
            | _ -> None

        let numItems = 
            match Int32.TryParse fields.[2] with
            | true, i -> i
            | _ -> 0

        let storyPoints =
            match Int32.TryParse fields.[3] with
            | true, s -> s
            | _ -> 0

        match date with
        | Some d -> 
            Some {
                Date = date.Value
                Status = fields.[1]
                NumberOfItems = numItems
                StoryPoints = storyPoints
            }
        | _ -> None


    let dateAndStatus item =
        sprintf "%A-%s" item.Date item.Status

    let dateDesc item = 
        DateTime.MaxValue - item.Date

    //de-dupe
    let deDuped =
        lines
        |> Seq.map parse
        |> Seq.choose (fun s -> s)
        |> Seq.distinctBy dateAndStatus
        |> Seq.sortBy dateDesc

    //write to file which will be the source for Excel
    deDuped
    |> Seq.map (fun i ->
            sprintf "%s,%s,%d,%d" (i.Date.ToString("yyyy-MM-dd")) i.Status i.NumberOfItems i.StoryPoints
        )
    |> fun c ->
        let contents = "Date,Status,NumberOfItems,StoryPoints"::(c |> Seq.toList)
        File.WriteAllLines("datasource.csv", contents)
    
    ()