﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SonarRestService.fs" company="Copyright © 2014 Tekla Corporation. Tekla is a Trimble Company">
//     Copyright (C) 2013 [Jorge Costa, Jorge.Costa@tekla.com]
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
// This program is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License
// as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details. 
// You should have received a copy of the GNU Lesser General Public License along with this program; if not, write to the Free
// Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
// --------------------------------------------------------------------------------------------------------------------
module SettingsService

open FSharp.Data
open FSharp.Data.JsonExtensions
open RestSharp
open VSSonarPlugins
open VSSonarPlugins.Types
open System.Collections.ObjectModel
open System
open System.Web
open System.Net
open System.IO
open System.Text.RegularExpressions
open System.Linq
open SonarRestService

type SettingsValues = JsonProvider<""" {"settings": [{"key": "sonar.test.jira","value": "abc"},{"key": "sonar.autogenerated","values": ["val1","val2","val3"],"inherited": false},{"key": "sonar.demo","fieldValues": [{"boolean": "true","text": "foo"},{"boolean": "false","text": "bar"}],"inherited": false},{"key": "sonar.issue.enforce.multicriteria","fieldValues": [{"resourceKey": "**/test/*.cpp","ruleKey": "cxx:MethodName"},{"resourceKey": "**/test/*.cpp","ruleKey": "cxx:ClassName"}],"inherited": true}]} """>
type FieldValueJson = JsonProvider<""" { "boolean": "true", "text": "foo"} """>

let IgnoreAllFile2(conf : ISonarConfiguration, projectIn : Resource, file : string, service : ISonarRestService, httpconnector : IHttpSonarConnector) =
    let epoch = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds.ToString().Replace(",", "")

    let uploadProperty(currentproject : Resource) =
        // let get properties for project
        let properties = service.GetProperties(conf, currentproject)

        let getPropertiesForUpload =                                 
            let elem = properties |> Seq.tryFind (fun c -> c.Key.Equals("sonar.issue.ignore.allfile"))
            match elem with
            | Some(c) -> c.Value + "," + epoch
            | _ -> epoch

        // upload new epochs
        let url = sprintf "/api/properties?id=sonar.issue.ignore.allfile&value=%s&resource=%s" getPropertiesForUpload currentproject.Key
        let response = httpconnector.HttpSonarPostRequest(conf, url, Map.empty)
        if response.StatusCode = Net.HttpStatusCode.OK then
            let url = sprintf "/api/properties?id=sonar.issue.ignore.allfile.%s.fileRegexp&value=%s&resource=%s" epoch file currentproject.Key
            httpconnector.HttpSonarPostRequest(conf, url, Map.empty) |> ignore

    if projectIn.IsBranch then
        for branch in projectIn.BranchResources do
            uploadProperty(branch)
    else
        uploadProperty(projectIn)

    true

let mutable data = "{"
let SetSetting(newConf : ISonarConfiguration, setting: Setting, project : Resource,httpconnector : IHttpSonarConnector) =
    
    let url = "/api/settings/set"
    let options = new System.Collections.Generic.List<System.Tuple<string,string>>()

    let setValuesOption = 
        if setting.Values.Count <> 0 then
            setting.Values |> Seq.iter (fun elem -> options.Add(("values", elem)))
        elif setting.Value <> null then
            options.Add(("value", setting.Value))

        if setting.FieldValues.Count <> 0 then
            let GetValuesOfField(values:System.Collections.Generic.List<string * string>)=
                data <- "{"
                values
                |> Seq.iter (fun (a,b) -> data <- data + "\"" + a + "\":\"" + b + "\",")
                data <- data.Trim(',') + "}"
                data

            setting.FieldValues
            |> Seq.iter (fun elem -> options.Add(("fieldValues", GetValuesOfField(elem.Values))) )

    if project = null then
        options.Add(("key", setting.key))
        setValuesOption
    else
        options.Add(("component", project.Key))
        options.Add(("key", setting.key))
        setValuesOption

    let response = httpconnector.HttpSonarPostRequestTuple(newConf, url, options)
    if response.StatusCode <> Net.HttpStatusCode.NoContent then
        "Failed to set prop: " + response.Content
    else
        ""

let GetSettings(newConf : ISonarConfiguration, project : Resource, httpconnector : IHttpSonarConnector) =
    let url =
        if project = null then
            "/api/settings/values"
        else
            "/api/settings/values?component=" + project.Key

    let responsecontent = httpconnector.HttpSonarGetRequest(newConf, url)
    let data = SettingsValues.Parse(responsecontent)
    let settings = new System.Collections.Generic.List<Setting>()

    let CreateSetting(i:SettingsValues.Setting) = 
        let setting = new Setting()
        let AddFieldValue(fieldValue:SettingsValues.FieldValue) = 
            let valueData = new FieldValue()
            fieldValue.JsonValue.Properties |> Seq.iter (fun (key,data) -> valueData.Values.Add(key, data.AsString()))
            setting.FieldValues.Add(valueData)

        setting.key <- i.Key
        if i.Inherited.IsSome then
            setting.Inherited <- i.Inherited.Value


        if i.Value.IsSome then setting.Value <- i.Value.Value

        if not(obj.ReferenceEquals(i.JsonValue.TryGetProperty("values"), null)) then
            i.Values |> Seq.iter (fun value -> setting.Values.Add(value))

        if not(obj.ReferenceEquals(i.JsonValue.TryGetProperty("fieldValues"), null)) then
            i.FieldValues |> Seq.iter (fun elem -> AddFieldValue(elem))

        settings.Add(setting)

    data.Settings |> Seq.iter (fun elem -> CreateSetting elem)

    settings

let GetProperties(newConf : ISonarConfiguration, project : Resource, httpconnector : IHttpSonarConnector) =
    let url =
        if project = null then
            "/api/properties"
        else
            "/api/properties?resource=" + project.Key

    let responsecontent = httpconnector.HttpSonarGetRequest(newConf, url)
    let data = JSonProperties.Parse(responsecontent)
    let dic = new System.Collections.Generic.Dictionary<string, string>()
    for i in data do
        try
            dic.Add(i.Key, i.Value.JsonValue.InnerText())
        with
        | ex -> ()

    dic

let IgnoreAllFile(conf : ISonarConfiguration, projectIn : Resource, file : string, service : ISonarRestService) =
    let mutable ok = true

    let epoch = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds.ToString().Replace(",", "")
    let dummyRule = new Rule(Key = "*")
    service.IgnoreRuleOnFile(conf, projectIn, file, dummyRule)

let GetExclusions(conf : ISonarConfiguration, projectIn : Resource, service : ISonarRestService) =
    let mutable ok = true
    let exclusions = new System.Collections.Generic.List<Exclusion>()

    let uploadProperty(currentproject : Resource) =

        let properties = service.GetProperties(conf, currentproject)

        // upload new epochs
        let AddExclusionToList(ruleKey : string, fileKey : string) =
            let resourceKeyElem = List.ofSeq exclusions |> Seq.tryFind (fun c -> c.RuleRegx.Equals(ruleKey) && c.FileRegx.Equals(fileKey))
            match resourceKeyElem with
            | Some(m) -> ()
            | _ -> exclusions.Add(new Exclusion(RuleRegx = ruleKey, FileRegx = fileKey))
                    
        let AddExclusion(key : string, value : string) =
            if key.EndsWith(".ruleKey") && key.StartsWith("sonar.issue.ignore.multicriteria") then
                let id = key.Replace("sonar.issue.ignore.multicriteria.", "").Replace(".ruleKey", "")
                let ruleRegex = value
                let resourceKeyElem = properties |> Seq.tryFind (fun c -> c.Key.Equals("sonar.issue.ignore.multicriteria." + id + ".resourceKey"))
                match resourceKeyElem with
                | Some(m) -> AddExclusionToList(ruleRegex, m.Value)
                | _ -> ()

        properties |> Seq.iter (fun c -> AddExclusion(c.Key, c.Value))

    if projectIn.IsBranch then
        for branch in projectIn.BranchResources do
            uploadProperty(branch)
    else
        uploadProperty(projectIn)

    exclusions :> System.Collections.Generic.IList<Exclusion>


let UpdateProperty(conf : ISonarConfiguration, id : string, value : string, projectIn : Resource, httpconnector : IHttpSonarConnector) = 
    let url = 
        if conf.SonarVersion < 6.3 then
            let resourcestr = 
                if projectIn = null then
                    ""
                else
                    sprintf "&resource=%s" projectIn.Key
            sprintf "/api/properties?id=%s&value=%s%s" id (HttpUtility.UrlEncode(value)) resourcestr
        else
            let resourcestr = 
                if projectIn = null then
                    ""
                else
                    sprintf "&component=%s" projectIn.Key
            sprintf "/api/settings?key=%s&value=%s%s" id (HttpUtility.UrlEncode(value)) resourcestr

    let response = httpconnector.HttpSonarPostRequest(conf, url, Map.empty)
    if response.StatusCode <> Net.HttpStatusCode.OK then
        "Failed update property: " + response.StatusCode.ToString()
    else
        ""

let IgnoreRuleOnFile(conf : ISonarConfiguration,
                     projectIn : Resource,
                     file : string,
                     rule : Rule,
                     service : ISonarRestService,
                     httpconnector : IHttpSonarConnector) =

    let exclusions = new System.Collections.Generic.List<Exclusion>()

    let epoch = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds.ToString().Replace(",", "")

    let uploadProperty(currentproject : Resource) =

        let properties = service.GetProperties(conf, currentproject)

        let getPropertiesForUpload =                                 
            let elem = properties |> Seq.tryFind (fun c -> c.Key.Equals("sonar.issue.ignore.multicriteria"))
            match elem with
            | Some(c) -> c.Value + "," + epoch
            | _ -> epoch

        // upload new epochs
        let url = sprintf "/api/properties?id=sonar.issue.ignore.multicriteria.%s.ruleKey&value=%s&resource=%s" epoch rule.Key currentproject.Key
        let response = httpconnector.HttpSonarPostRequest(conf, url, Map.empty)
        if response.StatusCode = Net.HttpStatusCode.OK then
            let url = sprintf "/api/properties?id=sonar.issue.ignore.multicriteria.%s.resourceKey&value=%s&resource=%s" epoch file currentproject.Key
            let response = httpconnector.HttpSonarPostRequest(conf, url, Map.empty)
            if response.StatusCode = Net.HttpStatusCode.OK then
                let url = sprintf "/api/properties?id=sonar.issue.ignore.multicriteria&value=%s&resource=%s" getPropertiesForUpload currentproject.Key
                httpconnector.HttpSonarPostRequest(conf, url, Map.empty) |> ignore

        let AddExclusionToList(ruleKey : string, fileKey : string) =
            let resourceKeyElem = List.ofSeq exclusions |> Seq.tryFind (fun c -> c.RuleRegx.Equals(ruleKey) && c.FileRegx.Equals(fileKey))
            match resourceKeyElem with
            | Some(m) -> ()
            | _ -> exclusions.Add(new Exclusion(RuleRegx = ruleKey, FileRegx = fileKey))
                    
        let AddExclusion(key : string, value : string) =
            if key.EndsWith(".ruleKey") && key.StartsWith("sonar.issue.ignore.multicriteria") then
                let id = key.Replace("sonar.issue.ignore.multicriteria.", "").Replace(".ruleKey", "")
                let ruleRegex = value
                let resourceKeyElem = properties |> Seq.tryFind (fun c -> c.Key.Equals("sonar.issue.ignore.multicriteria." + id + ".resourceKey"))
                match resourceKeyElem with
                | Some(m) -> AddExclusionToList(ruleRegex, m.Value)
                | _ -> ()

        properties |> Seq.iter (fun c -> AddExclusion(c.Key, c.Value))


    if projectIn.IsBranch then
        for branch in projectIn.BranchResources do
            uploadProperty(branch)
    else
        uploadProperty(projectIn)

    let foundAlready = List.ofSeq exclusions |> Seq.tryFind (fun  c -> c.RuleRegx.Equals(rule.Key) && c.FileRegx.Equals(file))
    match foundAlready with
    | Some(c) -> ()
    | _ -> exclusions.Add(new Exclusion(RuleRegx = rule.Key, FileRegx = file))

    exclusions :> System.Collections.Generic.IList<Exclusion>
