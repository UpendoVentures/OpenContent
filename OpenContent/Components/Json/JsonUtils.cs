﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components.TemplateHelpers;
using DotNetNuke.Services.FileSystem;
using Satrabel.OpenContent.Components.Datasource;


namespace Satrabel.OpenContent.Components.Json
{
    public static class JsonUtils
    {
        public static bool IsJson(this string jsonData)
        {
            if (string.IsNullOrWhiteSpace(jsonData)) return false;
            return jsonData.Trim().Substring(0, 1).IndexOfAny(new[] { '[', '{' }) == 0;
        }

        public static JToken LoadJsonFromFile(string filename)
        {
            return new FileUri(filename).ToJObject();
        }

        public static JObject GetJsonFromFile(string filename)
        {
            JObject retval;
            try
            {
                retval = JObject.Parse(File.ReadAllText(filename));
            }
            catch (Exception ex)
            {
                throw new InvalidJsonFileException($"Invalid json in file {filename}", ex, filename);
            }
            return retval;
        }

        public static dynamic JsonToDynamic(string json)
        {
            var dynamicObject = System.Web.Helpers.Json.Decode(json);
            return dynamicObject;
        }

        public static string SimplifyJson(string json, string culture)
        {
            JObject obj = JObject.Parse(json);
            SimplifyJson(obj, culture);
            return obj.ToString();
        }
        public static void SimplifyJson(JObject o, string culture)
        {
            foreach (var child in o.Children<JProperty>().ToList())
            {
                var childProperty = child;
                var array = childProperty.Value as JArray;
                if (array != null)
                {
                    foreach (var value in array)
                    {
                        var obj = value as JObject;
                        if (obj != null)
                        {
                            SimplifyJson(obj, culture);
                        }
                    }
                }
                else
                {
                    var obj = childProperty.Value as JObject;
                    if (obj != null)
                    {
                        bool languages = obj.Children<JProperty>().Any(v => v.Name.Length == 5 && v.Name.Substring(2, 1) == "-");
                        if (languages)
                        {
                            var cultureToken = obj[culture];
                            if (cultureToken != null)
                            {
                                childProperty.Value = cultureToken;
                            }
                            else
                            {
                                childProperty.Value = new JValue("");
                            }
                        }
                        else
                        {
                            SimplifyJson(obj, culture);
                        }
                    }
                }
            }
        }

        public static void SimplifyJson(JToken o, string culture)
        {
            var array = o as JArray;
            if (array != null)
            {
                foreach (var value in array)
                {
                    var obj = value as JObject;
                    if (obj != null)
                    {
                        SimplifyJson(obj, culture);
                    }
                }
            }
            else
            {
                var obj = o as JObject;
                if (obj != null)
                {
                    SimplifyJson(obj, culture);
                }
            }
        }

        public static void LookupJson(JObject o, JObject additionalData, JObject options)
        {
            foreach (var child in o.Children<JProperty>().ToList())
            {
                JObject opt = null;
                if (options?["fields"] != null)
                {
                    opt = options["fields"][child.Name] as JObject;
                }
                if (opt == null) continue;
                bool lookup =
                    opt["type"] != null &&
                    opt["type"].ToString() == "select2" &&
                    opt["dataService"]?["data"]?["dataKey"] != null;

                string dataKey = "";
                string dataMember = "";
                string valueField = "Id";
                if (lookup)
                {
                    dataKey = opt["dataService"]["data"]["dataKey"].ToString();
                    dataMember = opt["dataService"]["data"]["dataMember"]?.ToString() ?? "";
                    valueField = opt["dataService"]["data"]["valueField"]?.ToString() ?? "Id";
                }

                var childProperty = child;

                if (childProperty.Value is JArray)
                {
                    var array = childProperty.Value as JArray;
                    JArray newArray = new JArray();
                    foreach (var value in array)
                    {
                        var obj = value as JObject;
                        if (obj != null)
                        {
                            LookupJson(obj, additionalData, opt["items"] as JObject);
                        }
                        else if (lookup)
                        {
                            var val = value as JValue;
                            if (val != null)
                            {
                                try
                                {
                                    newArray.Add(GenerateObject(additionalData, dataKey, val.ToString(), dataMember, valueField));
                                }
                                catch (System.Exception)
                                {
                                }
                            }
                        }
                    }
                    if (lookup)
                    {
                        childProperty.Value = newArray;
                    }
                }
                else if (childProperty.Value is JObject)
                {
                    var obj = childProperty.Value as JObject;
                    LookupJson(obj, additionalData, opt);
                }
                else if (childProperty.Value is JValue)
                {
                    if (lookup)
                    {
                        string val = childProperty.Value.ToString();
                        try
                        {
                            o[childProperty.Name] = GenerateObject(additionalData, dataKey, val, dataMember, valueField);
                        }
                        catch (System.Exception ex)
                        {
                            Debugger.Break();
                        }
                    }
                }
            }
        }

        public static void LookupSelect2InOtherModule(JObject o, JObject options)
        {
            foreach (var child in o.Children<JProperty>().ToList())
            {
                JObject opt = null;
                if (options?["fields"] != null)
                {
                    opt = options["fields"][child.Name] as JObject;
                }
                if (opt == null) continue;
                bool lookup =
                    opt["type"] != null &&
                    opt["type"].ToString() == "select2" &&
                    opt["dataService"]?["data"]?["moduleId"] != null &&
                    opt["dataService"]?["data"]?["tabId"] != null;

                string dataMember = "";
                string valueField = "Id";
                string moduleId = "";
                string tabId = "";
                if (lookup)
                {
                    dataMember = opt["dataService"]["data"]["dataMember"]?.ToString() ?? "";
                    valueField = opt["dataService"]["data"]["valueField"]?.ToString() ?? "Id";
                    moduleId = opt["dataService"]["data"]["moduleId"]?.ToString() ?? "0";
                    tabId = opt["dataService"]["data"]["tabId"]?.ToString() ?? "0";
                }

                var childProperty = child;

                if (childProperty.Value is JArray)
                {
                    var array = childProperty.Value as JArray;
                    JArray newArray = new JArray();
                    foreach (var value in array)
                    {
                        var obj = value as JObject;
                        if (obj != null)
                        {
                            LookupSelect2InOtherModule(obj, opt["items"] as JObject);
                        }
                        else if (lookup)
                        {
                            var val = value as JValue;
                            if (val != null)
                            {
                                try
                                {
                                    var module = new OpenContentModuleInfo(int.Parse(moduleId), int.Parse(tabId));
                                    var ds = DataSourceManager.GetDataSource(module.Settings.Manifest.DataSource);
                                    var dsContext = OpenContentUtils.CreateDataContext(module);
                                    IDataItem dataItem = ds.Get(dsContext, val.ToString());
                                    newArray.Add(GenerateObject(dataItem, val.ToString(), dataMember, valueField));
                                }
                                catch (System.Exception)
                                {
                                    Debugger.Break();
                                }
                            }
                        }
                    }
                    if (lookup)
                    {
                        childProperty.Value = newArray;
                    }
                }
                else if (childProperty.Value is JObject)
                {
                    var obj = childProperty.Value as JObject;
                    LookupSelect2InOtherModule(obj, opt);
                }
                else if (childProperty.Value is JValue)
                {
                    if (lookup)
                    {
                        string val = childProperty.Value.ToString();
                        try
                        {
                            var module = new OpenContentModuleInfo(int.Parse(moduleId), int.Parse(tabId));
                            var ds = DataSourceManager.GetDataSource(module.Settings.Manifest.DataSource);
                            var dsContext = OpenContentUtils.CreateDataContext(module);
                            IDataItem dataItem = ds.Get(dsContext, val);
                            o[childProperty.Name] = GenerateObject(dataItem, val, dataMember, valueField);
                        }
                        catch (System.Exception ex)
                        {
                            Debugger.Break();
                        }
                    }
                }
            }
        }



        public static void ImagesJson(JObject o, JObject requestOptions, JObject options, bool isEditable)
        {
            foreach (var child in o.Children<JProperty>().ToList())
            {
                JObject opt = null;
                if (options?["fields"] != null)
                {
                    opt = options["fields"][child.Name] as JObject;
                }
                JObject reqOpt = null;
                if (requestOptions?["fields"] != null)
                {
                    reqOpt = requestOptions["fields"][child.Name] as JObject;
                }

                bool image = opt?["type"] != null && opt["type"].ToString() == "image2";

                if (image && reqOpt != null)
                {
                }
                var childProperty = child;
                if (childProperty.Value is JArray)
                {
                    var array = childProperty.Value as JArray;
                    JArray newArray = new JArray();
                    foreach (var value in array)
                    {
                        var obj = value as JObject;
                        if (obj != null)
                        {
                            //LookupJson(obj, reqOpt, opt["items"] as JObject);
                        }
                        else if (image)
                        {
                            var val = value as JValue;
                            if (val != null)
                            {
                                try
                                {
                                    newArray.Add(GenerateImage(reqOpt, val.ToString(), isEditable));
                                }
                                catch (System.Exception)
                                {
                                }
                            }
                        }
                    }
                    if (image)
                    {
                        childProperty.Value = newArray;
                    }
                }
                else if (childProperty.Value is JObject)
                {
                    var obj = childProperty.Value as JObject;

                }
                else if (childProperty.Value is JValue)
                {
                    if (image)
                    {
                        string val = childProperty.Value.ToString();
                        try
                        {
                            //o[childProperty.Name] = GenerateObject(additionalData, dataKey, val, dataMember, valueField);
                            o[childProperty.Name] = GenerateImage(reqOpt, val, isEditable);
                        }
                        catch (System.Exception)
                        {
                        }
                    }
                }
            }
        }

        private static JToken GenerateImage(JObject reqOpt, string p, bool isEditable)
        {
            var ratio = new Ratio(100, 100);
            if (reqOpt != null && reqOpt["ratio"] != null)
            {
                ratio = new Ratio(reqOpt["ratio"].ToString());
            }
            int fileId = int.Parse(p);
            IFileInfo file = FileManager.Instance.GetFile(fileId);
            var imageUrl = ImageHelper.GetImageUrl(file, ratio);
            var editUrl = isEditable ? GetFileEditUrl(file) : "";

            var obj = new JObject();
            obj["ImageId"] = fileId;
            obj["ImageUrl"] = imageUrl;
            if (isEditable)
                obj["EditUrl"] = editUrl;
            return obj;
            //return new JValue(imageUrl);
        }
        private static string GetFileEditUrl(IFileInfo f)
        {
            if (f == null) return "";
            var portalFileUri = new PortalFileUri(f);
            return portalFileUri.EditUrl();
        }
        private static JObject GenerateObject(IDataItem additionalData, string id, string dataMember, string valueField)
        {
            var json = additionalData?.Data;
            //if (!string.IsNullOrEmpty(dataMember))
            //{
            //    json = json[dataMember];
            //}
            if (json != null)
            {
                return json as JObject;
            }
            JObject res = new JObject();
            res["Id"] = id;
            res["Title"] = "unknow";
            return res;
        }

        private static JObject GenerateObject(JObject additionalData, string key, string id, string dataMember, string valueField)
        {
            var json = additionalData[key.ToLowerInvariant()];
            if (!string.IsNullOrEmpty(dataMember))
            {
                json = json[dataMember];
            }
            JArray array = json as JArray;
            if (array != null)
            {
                foreach (var obj in array)
                {
                    var objid = obj[valueField].ToString();
                    if (id.Equals(objid))
                    {
                        return obj as JObject;
                    }
                }
            }
            JObject res = new JObject();
            res["Id"] = id;
            res["Title"] = "unknow";
            return res;
        }

        public static void Merge(JObject model, JObject completeModel)
        {
            foreach (var prop in completeModel.Properties())
            {
                model[prop.Name] = prop.Value;
            }
        }

        internal static void IdJson(JToken o)
        {
            var array = o as JArray;
            if (array != null)
            {
                foreach (var value in array)
                {
                    var obj = value as JObject;
                    if (obj != null)
                    {
                        if (obj["id"] == null)
                        {
                            obj["id"] = Guid.NewGuid().ToString();
                        }
                        IdJson(obj);
                    }
                }
            }
            else
            {
                var obj = o as JObject;
                if (obj != null)
                {
                    if (obj["id"] == null)
                    {
                        obj["id"] = Guid.NewGuid().ToString();
                    }
                    foreach (var child in o.Children<JProperty>().ToList())
                    {
                        IdJson(child.Value);
                    }
                }
            }
        }

        public static void RemoveType(JToken o)
        {
            var array = o as JArray;
            if (array != null)
            {
                foreach (var value in array)
                {
                    var obj = value as JObject;
                    if (obj != null)
                    {
                        if (obj["__type"] != null)
                        {
                            obj.Remove("__type");
                        }
                        RemoveType(obj);
                    }
                }
            }
            else
            {
                var obj = o as JObject;
                if (obj != null)
                {
                    if (obj["__type"] != null)
                    {
                        obj.Remove("__type");
                    }
                    foreach (var child in o.Children<JProperty>().ToList())
                    {
                        RemoveType(child.Value);
                    }
                }
            }
        }
    }
}