using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rittal.IMP2020.Utility;
using Rittal.IMP2020.Connection.Util.http;
using Rittal.IMP2020.Global.Definitions;
using Rittal.IMP2020.Communication.Base.JsonMsgDescriptions;
using Rittal.IMP2020.Connection;
using Rittal.IMP2020.Drivers.Base;
using Rittal.IMP2020.Utility.ScriptingHelper;
using unirest_net.http;

namespace Rittal.IMP2020.Drivers.Concrete
{
    public class PuiiiHttpDriver : SessionBasedHttpCommunicationDevice
    {
        public const string BASE_URI_TEMPLATE = @"$""http://{IpAddress}/cgi-bin/json.cgi""";

        public const string CREATE_SESSION_TEMPLATE = @"$""createSession={{\""username\"":\""{Username}\"",\""password\"":\""{Password}\"",\""force\"":1}}""";
        public const string GET_DEVICE_DESCRIPTIONS = @"$""getDeviceDescriptions={{\""sessionId\"":{SessionId},\""devices\"":[]}}""";
        public const string GET_DEVICE_INFOS = @"$""getDeviceInfos={{\""sessionId\"":{SessionId},\""devices\"":[]}}""";
        public const string GET_PROCESS_DATA = @"$""getProcessData={{\""sessionId\"":{SessionId},\""lastTimestamp\"":{LastTimeStamp}}}""";

        public const string CHECK_SESSION_CREATION_SUCCESS = @"$""JObject.Parse(@\""{JsonString}\"").SelectToken(\""result\"").ToString() == \""0\""""";
        public const string GET_SESSION_ID = @"$""JObject.Parse(@\""{JsonString}\"").SelectToken(\""sessionId\"").ToString()""";
        public const string GET_SESSION_TIMEOUT = @"$""JObject.Parse(@\""{JsonString}\"").SelectToken(\""user.autologout\"").ToString()""";

        public const string MATCH_DEVICE =
                @"$""JObject.Parse(@\""{JsonString}\"").SelectToken(\""result\"").ToString() == \""0\"" &&" +
                @"JObject.Parse(@\""{JsonString}\"").SelectToken(\""deviceInfos[{DeviceIndex}].info.type\"").ToString() == \""CMCIII-PU\"" &&" +
                @"JObject.Parse(@\""{JsonString}\"").SelectToken(\""deviceInfos[{DeviceIndex}].info.orderNumber\"").ToString() == \""7030.000\""""";

        public const string GET_VALUE_FROM_JSON = @"$""JObject.Parse(@\""{JsonString}\"").SelectToken(\""{TokenPath}\"").ToString()""";
        public const string GET_VALUE_FROM_JSON_GENERIC = @"$""JObject.Parse(@\""{JsonString}\"").SelectToken(\""{TokenPath}\"").ToObject<{Type}>()""";
        public const string GET_VALUE_FROM_JSON_FOR_LINQ = @"$""JObject.Parse(@\""{JsonString}\"").SelectToken(\""{TokenPath}\"")""";

        public const string CALCULATE_SCALING = @"$""{Value} < 0 ?  -1 / {Value}f : {Value}""";
        public const string CALCULATE_SCALE_DEPENDING_VALUES = @"$""{Value}f*{Factor.ToString(CultureInfo.InvariantCulture)}f""";

        //private Script<string> _baseUriScript;
        
        //private Script<string> _createSessionScript;
        //private Script<string> _getDeviceDescriptionsScript;
        //private Script<string> _getDeviceInfosScript;
        //private Script<string> _getProcessDataScript;

        //private Script<string> _checkSessionCreationSuccessScript;
        //private Script<string> _getSessionIdScript;
        //private Script<string> _getSessionTimeoutScript;
        
        //private Script<string> _matchDevice;

        //private Script<string> _getValueFromJson;

        //private Script<string> _calculateScaleing;
        //private Script<string> _calculateScaleDependingValues;

        public string Username { get; set; }
        public string Password { get; set; }

        public PuiiiHttpDriver()
        {

            //_baseUriScript = CSharpScriptHelper.CompileScript<string>(BASE_URI_TEMPLATE, typeof(IpAddressCapsule));

            //_createSessionScript = CSharpScriptHelper.CompileScript<string>(CREATE_SESSION_TEMPLATE, typeof(UsernamePasswordCapsule));
            //_getDeviceDescriptionsScript = CSharpScriptHelper.CompileScript<string>(GET_DEVICE_DESCRIPTIONS, typeof(SessionIdCapsule));
            //_getDeviceInfosScript = CSharpScriptHelper.CompileScript<string>(GET_DEVICE_INFOS, typeof(SessionIdCapsule));
            //_getProcessDataScript = CSharpScriptHelper.CompileScript<string>(GET_PROCESS_DATA, typeof(SessionIdLastTimeStampCapsule));

            //_checkSessionCreationSuccessScript = CSharpScriptHelper.CompileScript<string>(CHECK_SESSION_CREATION_SUCCESS, typeof(JsonStringCapsule));
            //_getSessionIdScript = CSharpScriptHelper.CompileScript<string>(GET_SESSION_ID, typeof(JsonStringCapsule));
            //_getSessionTimeoutScript = CSharpScriptHelper.CompileScript<string>(GET_SESSION_TIMEOUT, typeof(JsonStringCapsule));

            //_matchDevice = CSharpScriptHelper.CompileScript<string>(MATCH_DEVICE, typeof(JsonStringCapsule));

            //_getValueFromJson = CSharpScriptHelper.CompileScript<string>(GET_VALUE_FROM_JSON, typeof(JsonStringTokenPathCapsule));

            //_calculateScaleing = CSharpScriptHelper.CompileScript<string>(CALCULATE_SCALING, typeof(MathValueInjector));
            //_calculateScaleDependingValues = CSharpScriptHelper.CompileScript<string>(CALCULATE_SCALE_DEPENDING_VALUES, typeof(MathValueAndFactorInjector));

        }
        

        public string GetDevicedescirptionString(string sessionId)
        {
            return CSharpScript.Create($"string SessionId = \"{sessionId}\";")
                .ContinueWith<string>(GET_DEVICE_DESCRIPTIONS)
                .RunAsync().Result.ReturnValue;
            //_getDeviceDescriptionsScript.RunAsync(new SessionIdCapsule() {SessionId = sessionId}).Result.ReturnValue;
        }

        public string GetDeviceInfoString(string sessionId)
        {
            return CSharpScript.Create($"string SessionId = \"{sessionId}\";")
                .ContinueWith<string>(GET_DEVICE_INFOS)
                .RunAsync().Result.ReturnValue;
            //_getDeviceInfosScript.RunAsync(new SessionIdCapsule() {SessionId = sessionId}).Result.ReturnValue;
        }
        
        private bool HandleSessionCreation(HttpResponse<string> httpResponse, IConnector connector)
        {
            var httpCon = connector as SessionBasedHttpConnector;

            var escapedResponse = $"string JsonString = @\"{httpResponse.Body.Replace("\"", "\"\"\"\"")}\";";

            if (!CSharpScript.Create<bool>(
                    CSharpScript.Create(escapedResponse)
                        .ContinueWith(CHECK_SESSION_CREATION_SUCCESS)
                        .RunAsync().Result.ReturnValue.ToString())
                    .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                    .WithReferences(typeof(JObject).Assembly))
                .RunAsync().Result.ReturnValue)
                return false;

            httpCon.SessionId = CSharpScript.Create<string>(
                                        CSharpScript.Create(escapedResponse)
                                                .ContinueWith(GET_SESSION_ID)
                                                .RunAsync().Result.ReturnValue.ToString())
                                            .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                            .WithReferences(typeof(JObject).Assembly))
                                    .RunAsync().Result.ReturnValue;

            //var lowest = CSharpScript.Create<string>($"{escapedResponse}")
            //    .ContinueWith("string TokenPath = \"user.autologout\";")
            //    .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
            //    .RunAsync().Result.ReturnValue.ToString();


            //var secondlowest = CSharpScript.Create<JToken>(
            //                lowest)
            //            .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
            //                .WithReferences(typeof(JObject).Assembly))
            //            .RunAsync().Result.ReturnValue?.Value<long>() ?? 0;

            //var thirdInner = CSharpScript.Create<DateTime>($@"DateTime.Now.Add(TimeSpan.FromSeconds({secondlowest}))")
            //    .WithOptions(ScriptOptions.Default.WithImports("System"))
            //    .RunAsync().Result.ReturnValue;

            httpCon.SetSessionTimeout(
                        CSharpScript.Create<DateTime>($@"DateTime.Now.Add(TimeSpan.FromSeconds({
                            CSharpScript.Create<JToken>(
                                CSharpScript.Create<string>(escapedResponse)
                                    .ContinueWith("string TokenPath = \"user.autologout\";")
                                    .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                .RunAsync().Result.ReturnValue.ToString())
                                .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                .WithReferences(typeof(JObject).Assembly))
                            .RunAsync().Result.ReturnValue?.Value<long>() ?? 0}))")
                            .WithOptions(ScriptOptions.Default.WithImports("System"))
                        .RunAsync().Result.ReturnValue);


            return CSharpScript.Create<bool>(
                          CSharpScript.Create(escapedResponse)
                                .ContinueWith(CHECK_SESSION_CREATION_SUCCESS)
                            .RunAsync().Result.ReturnValue.ToString())
                            .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                            .WithReferences(typeof(JObject).Assembly))
                        .RunAsync().Result.ReturnValue;
        }

        public override bool Matches(IConnector connector)
        {
            var httpConnector = connector as SessionBasedHttpConnector;
            //httpConnector.CheckSessionCreationSuccessfull = _checkSessionCreationSuccessScript;
            //httpConnector.GetSessionId = _getSessionIdScript;
            //httpConnector.GetSessionTimeout = _getSessionTimeoutScript;

            httpConnector.BaseUri = CSharpScript.Create($"string IpAddress = \"{httpConnector.HostOrIp}\";").
                                            ContinueWith<string>(BASE_URI_TEMPLATE).RunAsync().Result.ReturnValue;

            var body = CSharpScript.Create($"string Username = \"{httpConnector.AuthenticationInfo.Username}\";")
                                .ContinueWith($"string Password = \"{httpConnector.AuthenticationInfo.Password}\";")
                                .ContinueWith<string>(CREATE_SESSION_TEMPLATE)
                            .RunAsync().Result.ReturnValue;

            httpConnector.CreateSessionRequest = Unirest.post(httpConnector.BaseUri)
                                                    .body(body);

            if (!httpConnector.OpenSession<bool,string>(HandleSessionCreation)) return false;

            var getDeviceDescriptionReq = Unirest.post(httpConnector.BaseUri)
                .body(GetDeviceInfoString(httpConnector.SessionId));


            var deviceDesc = httpConnector.ExecuteRequest(getDeviceDescriptionReq);

            var matches = CSharpScript.Create<bool>(
                                CSharpScript.Create($"string JsonString = @\"{deviceDesc.Body.Replace("\"", "\"\"\"\"")}\";")
                                    .ContinueWith("string DeviceIndex = \"0\";")
                                    .ContinueWith<string>(MATCH_DEVICE)
                                .RunAsync().Result.ReturnValue)
                                .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                .WithReferences(typeof(JObject).Assembly))
                            .RunAsync().Result.ReturnValue;

            if (matches)
            {
                httpConnector.SetUniqueId(httpConnector.BaseUri);
                return true;
            }
            return false;
        }

        public override Imp2020DevicePublishMsg Discover(IConnector connector)
        {
            var httpConnector = connector as SessionBasedHttpConnector;
            if (!httpConnector.OpenSession<bool, string>(HandleSessionCreation)) return null;

            var getDeviceInfosReq = Unirest.post(httpConnector.BaseUri)
                .body(GetDeviceInfoString(httpConnector.SessionId));

            var deviceInfos = httpConnector.ExecuteRequest(getDeviceInfosReq);
            var escapedJson = deviceInfos.Body.Replace("\"", "\"\"");
            var doubleEscapedJson = deviceInfos.Body.Replace("\"", "\"\"\"\"");
            
            var newDevMsg = new Imp2020DevicePublishMsg();

            //newDevMsg.Name = CSharpScriptHelper.ExecuteValueEnrichedScript<string>(
            //    _getValueFromJson.RunAsync(new JsonStringTokenPathCapsule()
            //        {
            //            JsonString = escapedJson,
            //            TokenPath = "deviceInfos[0].info.name"
            //        }).Result.ReturnValue,
            //    new[] {"Newtonsoft.Json.Linq"}, new[] {typeof(JObject).Assembly});

            newDevMsg.Name = CSharpScript.Create<JToken>(
                    CSharpScript.Create<string>($"string JsonString = @\"{doubleEscapedJson}\";")
                        .ContinueWith("string TokenPath = \"deviceInfos[0].info.name\";")
                        .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                        .RunAsync().Result.ReturnValue.ToString())
                .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                    .WithReferences(typeof(JObject).Assembly))
                .RunAsync().Result.ReturnValue?.Value<string>() ?? string.Empty;

            //newDevMsg.DevicePath = CSharpScriptHelper.ExecuteValueEnrichedScript<string>(
            //    _getValueFromJson.RunAsync(new JsonStringTokenPathCapsule()
            //    {
            //        JsonString = escapedJson,
            //        TokenPath = "deviceInfos[0].deviceIndex"
            //    }).Result.ReturnValue,
            //    new[] { "Newtonsoft.Json.Linq" }, new[] { typeof(JObject).Assembly });

            newDevMsg.DevicePath = CSharpScript.Create<JToken>(
                                           CSharpScript.Create<string>($"string JsonString = @\"{doubleEscapedJson}\";")
                                               .ContinueWith("string TokenPath = \"deviceInfos[0].deviceIndex\";")
                                               .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                               .RunAsync().Result.ReturnValue.ToString())
                                       .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                           .WithReferences(typeof(JObject).Assembly))
                                       .RunAsync().Result.ReturnValue?.Value<string>() ?? string.Empty;


            //newDevMsg.OrderNumber = CSharpScriptHelper.ExecuteValueEnrichedScript<string>(
            //    _getValueFromJson.RunAsync(new JsonStringTokenPathCapsule()
            //    {
            //        JsonString = escapedJson,
            //        TokenPath = "deviceInfos[0].info.orderNumber"
            //    }).Result.ReturnValue,
            //    new[] { "Newtonsoft.Json.Linq" }, new[] { typeof(JObject).Assembly });

            newDevMsg.OrderNumber = CSharpScript.Create<JToken>(
                                            CSharpScript.Create<string>($"string JsonString = @\"{doubleEscapedJson}\";")
                                                .ContinueWith("string TokenPath = \"deviceInfos[0].info.orderNumber\";")
                                                .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                .RunAsync().Result.ReturnValue.ToString())
                                        .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                            .WithReferences(typeof(JObject).Assembly))
                                        .RunAsync().Result.ReturnValue?.Value<string>() ?? string.Empty;

            //newDevMsg.SerialNumber = CSharpScriptHelper.ExecuteValueEnrichedScript<string>(
            //    _getValueFromJson.RunAsync(new JsonStringTokenPathCapsule()
            //    {
            //        JsonString = escapedJson,
            //        TokenPath = "deviceInfos[0].info.serialNumber"
            //    }).Result.ReturnValue,
            //    new[] { "Newtonsoft.Json.Linq" }, new[] { typeof(JObject).Assembly });

            newDevMsg.SerialNumber = CSharpScript.Create<JToken>(
                                             CSharpScript.Create<string>($"string JsonString = @\"{doubleEscapedJson}\";")
                                                 .ContinueWith("string TokenPath = \"deviceInfos[0].info.serialNumber\";")
                                                 .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                 .RunAsync().Result.ReturnValue.ToString())
                                         .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                             .WithReferences(typeof(JObject).Assembly))
                                         .RunAsync().Result.ReturnValue?.Value<string>() ?? string.Empty;


            newDevMsg.VariablePublishMsgs = DiscoverVariables(connector, "0" /*newDevMsg.DevicePath   CMCIII DevicePath starts with 1 JSON Array is zero based*/);

            return newDevMsg;

        }

        public List<Imp2020VariablePublishMsg> DiscoverVariables(IConnector connector, string devicePath)
        {
            var httpConnector = connector as SessionBasedHttpConnector;
            if (!httpConnector.OpenSession<bool, string>(HandleSessionCreation)) return null;


            var getDeviceDescReq = Unirest.post(httpConnector.BaseUri)
                .body(GetDevicedescirptionString(httpConnector.SessionId));

            var deviceDesc = httpConnector.ExecuteRequest(getDeviceDescReq);
            var escapedJson = deviceDesc.Body.Replace("\"", "\"\"\"\"");

            var devDescTokenPath = $"deviceDescriptions[{devicePath}].descriptions";

            //var specificDesc = CSharpScriptHelper.ExecuteValueEnrichedScript<string>(
            //    _getValueFromJson.RunAsync(new JsonStringTokenPathCapsule()
            //        {
            //            JsonString = escapedJson,
            //            TokenPath = devDescTokenPath
            //    } 
            //    ).Result.ReturnValue, 
            //    new []{"Newtonsoft.Json.Linq"}, new []{typeof(JObject).Assembly});

            var inner = CSharpScript.Create<string>($"string JsonString = @\"{escapedJson}\";")
                .ContinueWith($"string TokenPath = \"{devDescTokenPath}\";")
                .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                .RunAsync().Result.ReturnValue.ToString();

            var specificDesc = CSharpScript.Create(
                    inner)
                .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                    .WithReferences(typeof(JObject).Assembly))
                .RunAsync().Result.ReturnValue.ToString();

            var returnList = JArray.Parse(specificDesc)
                                .Where(vd => vd.HasValues)
                                .Select(vd => vd.ToString().Replace("\"", "\"\"\"\""))
                                .Select(ej =>
                                    new Imp2020VariablePublishMsg()
                                    {
                                        Name = CSharpScript.Create<JToken>(
                                                       CSharpScript.Create<string>($"string JsonString = @\"{ej}\";")
                                                           .ContinueWith("string TokenPath = \"name\";")
                                                           .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                           .RunAsync().Result.ReturnValue.ToString())
                                                   .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                                   .WithReferences(typeof(JObject).Assembly))
                                                   .RunAsync().Result.ReturnValue?.Value<string>() ?? string.Empty,

                                        DataType = GetVariableDataType(CSharpScript.Create<JToken>(
                                                                               CSharpScript.Create<string>($"string JsonString = @\"{ej}\";")
                                                                                   .ContinueWith("string TokenPath = \"dataType\";")
                                                                                   .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                                                   .RunAsync().Result.ReturnValue.ToString())
                                                                           .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                                                           .WithReferences(typeof(JObject).Assembly))
                                                                           .RunAsync().Result.ReturnValue?.Value<string>() ?? string.Empty),

                                        ValueUnitType = CSharpScript.Create<JToken>(
                                                                CSharpScript.Create<string>($"string JsonString = @\"{ej}\";")
                                                                    .ContinueWith("string TokenPath = \"unitName\";")
                                                                    .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                                    .RunAsync().Result.ReturnValue.ToString())
                                                            .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                                            .WithReferences(typeof(JObject).Assembly))
                                                            .RunAsync().Result.ReturnValue?.Value<string>() ?? string.Empty,

                                        Constraints = new Imp2020VariableConstraintsPublishMsg()
                                        {
                                            MaxLength = CSharpScript.Create<JToken>(
                                                                CSharpScript.Create<string>($"string JsonString = @\"{ej}\";")
                                                                    .ContinueWith("string TokenPath = \"constraints.maxLen\";")
                                                                    .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                                    .RunAsync().Result.ReturnValue.ToString())
                                                            .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                                            .WithReferences(typeof(JObject).Assembly))
                                                            .RunAsync().Result.ReturnValue?.Value<int>() ?? 0,

                                            RegularExpression = CSharpScript.Create<JToken>(
                                                                        CSharpScript.Create<string>($"string JsonString = @\"{ej}\";")
                                                                            .ContinueWith("string TokenPath = \"constraints.regExpr\";")
                                                                            .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                                            .RunAsync().Result.ReturnValue.ToString())
                                                                    .WithOptions(ScriptOptions.Default
                                                                    .WithImports("Newtonsoft.Json.Linq")
                                                                    .WithReferences(typeof(JObject).Assembly))
                                                                    .RunAsync().Result.ReturnValue?.Value<string>() ?? string.Empty,

                                            Scaling = CSharpScript.Create<float>(
                                                                CSharpScript.Create($@"int Value = {
                                                                        CSharpScript.Create<JToken>(
                                                                                CSharpScript.Create<string>($"string JsonString = @\"{ej}\";")
                                                                                    .ContinueWith("string TokenPath = \"constraints.scaling\";")
                                                                                    .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                                                    .RunAsync().Result.ReturnValue.ToString())
                                                                            .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                                                            .WithReferences(typeof(JObject).Assembly))
                                                                            .RunAsync().Result.ReturnValue?.Value<int>() ?? 0};")
                                                                    .ContinueWith<string>(CALCULATE_SCALING)
                                                                    .RunAsync().Result.ReturnValue)
                                                                .RunAsync().Result.ReturnValue,

                                            Minimum = CSharpScript.Create<float>(
                                                    CSharpScript.Create($@"int Value = {
                                                            CSharpScript.Create<JToken>(
                                                                    CSharpScript.Create<string>($"string JsonString = @\"{ej}\";")
                                                                        .ContinueWith("string TokenPath = \"constraints.min\";")
                                                                        .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                                        .RunAsync().Result.ReturnValue.ToString())
                                                                .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                                                    .WithReferences(typeof(JObject).Assembly))
                                                                .RunAsync().Result.ReturnValue?.Value<int>() ?? 0};")
                                                        .ContinueWith($@"float Factor = {CSharpScript.Create<float>(
                                                                CSharpScript.Create($@"int Value = {
                                                                        CSharpScript.Create<JToken>(
                                                                                CSharpScript.Create<string>($"string JsonString = @\"{ej}\";")
                                                                                    .ContinueWith("string TokenPath = \"constraints.scaling\";")
                                                                                    .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                                                    .RunAsync().Result.ReturnValue.ToString())
                                                                            .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                                                                .WithReferences(typeof(JObject).Assembly))
                                                                            .RunAsync().Result.ReturnValue?.Value<int>() ?? 0};")
                                                                    .ContinueWith<string>(CALCULATE_SCALING)
                                                                    .RunAsync().Result.ReturnValue)
                                                            .RunAsync().Result.ReturnValue.ToString(CultureInfo.InvariantCulture)}f;")
                                                        .ContinueWith(CALCULATE_SCALE_DEPENDING_VALUES)
                                                        .WithOptions(ScriptOptions.Default.WithImports("System.Globalization"))
                                                        .RunAsync().Result.ReturnValue.ToString())
                                                .RunAsync().Result.ReturnValue,

                                            Maximum = CSharpScript.Create<float>(
                                                    CSharpScript.Create($@"int Value = {
                                                            CSharpScript.Create<JToken>(
                                                                    CSharpScript.Create<string>($"string JsonString = @\"{ej}\";")
                                                                        .ContinueWith("string TokenPath = \"constraints.max\";")
                                                                        .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                                        .RunAsync().Result.ReturnValue.ToString())
                                                                .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                                                    .WithReferences(typeof(JObject).Assembly))
                                                                .RunAsync().Result.ReturnValue?.Value<int>() ?? 0};")
                                                        .ContinueWith($@"float Factor = {CSharpScript.Create<float>(
                                                                CSharpScript.Create($@"int Value = {
                                                                        CSharpScript.Create<JToken>(
                                                                                CSharpScript.Create<string>($"string JsonString = @\"{ej}\";")
                                                                                    .ContinueWith("string TokenPath = \"constraints.scaling\";")
                                                                                    .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                                                    .RunAsync().Result.ReturnValue.ToString())
                                                                            .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                                                                .WithReferences(typeof(JObject).Assembly))
                                                                            .RunAsync().Result.ReturnValue?.Value<int>() ?? 0};")
                                                                    .ContinueWith<string>(CALCULATE_SCALING)
                                                                    .RunAsync().Result.ReturnValue)
                                                            .RunAsync().Result.ReturnValue.ToString(CultureInfo.InvariantCulture)}f;")
                                                        .ContinueWith(CALCULATE_SCALE_DEPENDING_VALUES)
                                                        .WithOptions(ScriptOptions.Default.WithImports("System.Globalization"))
                                                        .RunAsync().Result.ReturnValue.ToString())
                                                .RunAsync().Result.ReturnValue,

                                            Step = CSharpScript.Create<float>(
                                                    CSharpScript.Create($@"int Value = {
                                                            CSharpScript.Create<JToken>(
                                                                    CSharpScript.Create<string>($"string JsonString = @\"{ej}\";")
                                                                        .ContinueWith("string TokenPath = \"constraints.steps\";")
                                                                        .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                                        .RunAsync().Result.ReturnValue.ToString())
                                                                .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                                                    .WithReferences(typeof(JObject).Assembly))
                                                                .RunAsync().Result.ReturnValue?.Value<int>() ?? 0};")
                                                        .ContinueWith($@"float Factor = {CSharpScript.Create<float>(
                                                                CSharpScript.Create($@"int Value = {
                                                                        CSharpScript.Create<JToken>(
                                                                                CSharpScript.Create<string>($"string JsonString = @\"{ej}\";")
                                                                                    .ContinueWith("string TokenPath = \"constraints.scaling\";")
                                                                                    .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
                                                                                    .RunAsync().Result.ReturnValue.ToString())
                                                                            .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
                                                                                .WithReferences(typeof(JObject).Assembly))
                                                                            .RunAsync().Result.ReturnValue?.Value<int>() ?? 0};")
                                                                    .ContinueWith<string>(CALCULATE_SCALING)
                                                                    .RunAsync().Result.ReturnValue)
                                                            .RunAsync().Result.ReturnValue.ToString(CultureInfo.InvariantCulture)}f;")
                                                        .ContinueWith(CALCULATE_SCALE_DEPENDING_VALUES)
                                                        .WithOptions(ScriptOptions.Default.WithImports("System.Globalization"))
                                                        .RunAsync().Result.ReturnValue.ToString())
                                                .RunAsync().Result.ReturnValue
                                        }
                                    }).ToList();

            //foreach (var varDesc in JArray.Parse(specificDesc))
            //{
            //    if(!varDesc.HasValues) continue;

                



            //    var jsonString = varDesc.ToString().Replace("\"","\"\"");
            //    var doubleEscapedJson = varDesc.ToString().Replace("\"", "\"\"\"\"");

                


            //    var newVarMsg = new Imp2020VariablePublishMsg();
                
            //    newVarMsg.Name = CSharpScriptHelper.ExecuteValueEnrichedScript<string>(
            //        _getValueFromJson.RunAsync(new JsonStringTokenPathCapsule()
            //        {
            //            JsonString = jsonString,
            //            TokenPath = "name"
            //        }).Result.ReturnValue,
            //        new[] { "Newtonsoft.Json.Linq" }, new[] { typeof(JObject).Assembly });

            //    var tmp = CSharpScriptHelper.ExecuteValueEnrichedScript<string>(
            //        _getValueFromJson.RunAsync(new JsonStringTokenPathCapsule()
            //        {
            //            JsonString = jsonString,
            //            TokenPath = "dataType"
            //        }).Result.ReturnValue,
            //        new[] {"Newtonsoft.Json.Linq"}, new[] {typeof(JObject).Assembly});

            //    newVarMsg.DataType = GetVariableDataType(tmp);

            //    newVarMsg.ValueUnitType = CSharpScriptHelper.ExecuteValueEnrichedScript<string>(
            //        _getValueFromJson.RunAsync(new JsonStringTokenPathCapsule()
            //        {
            //            JsonString = jsonString,
            //            TokenPath = "unitName"
            //        }).Result.ReturnValue,
            //        new[] {"Newtonsoft.Json.Linq"}, new[] {typeof(JObject).Assembly});

            //    var constraints = new Imp2020VariableConstraintsPublishMsg();

            //    var script = CSharpScript
            //        .Create<string>($"string JsonString = @\"{doubleEscapedJson}\";")
            //        .ContinueWith("string TokenPath = \"constraints.maxLen\";")
            //        .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
            //        .RunAsync().Result.ReturnValue.ToString();

            //    constraints.MaxLength = CSharpScript.Create<JToken>(script)
            //        .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
            //            .WithReferences(typeof(JObject).Assembly))
            //        .RunAsync().Result.ReturnValue?.Value<int>() ?? 0;


            //    script = CSharpScript
            //        .Create<string>($"string JsonString = @\"{doubleEscapedJson}\";")
            //        .ContinueWith("string TokenPath = \"constraints.regExpr\";")
            //        .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
            //        .RunAsync().Result.ReturnValue.ToString();

            //    constraints.RegularExpression = CSharpScript.Create<JToken>(script)
            //        .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
            //        .WithReferences(typeof(JObject).Assembly))
            //        .RunAsync().Result.ReturnValue?.Value<string>() ?? string.Empty;


            //    script = CSharpScript
            //        .Create<string>($"string JsonString = @\"{doubleEscapedJson}\";")
            //        .ContinueWith("string TokenPath = \"constraints.scaling\";")
            //        .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
            //        .RunAsync().Result.ReturnValue.ToString();


            //    var tmpInt = CSharpScript.Create<JToken>(script)
            //        .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
            //            .WithReferences(typeof(JObject).Assembly))
            //        .RunAsync().Result.ReturnValue?.Value<int>() ?? 0; 


            //    constraints.Scaling = CSharpScriptHelper.ExecuteValueEnrichedScript<float>(
            //        _calculateScaleing.RunAsync(new MathValueInjector()
            //        {
            //            Value = tmpInt
            //        }).Result.ReturnValue,
            //        new string[0], new Assembly[0]);

            //    script = CSharpScript
            //        .Create<string>($"string JsonString = @\"{doubleEscapedJson}\";")
            //        .ContinueWith("string TokenPath = \"constraints.min\";")
            //        .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
            //        .RunAsync().Result.ReturnValue.ToString();

            //    tmpInt = CSharpScript.Create<JToken>(script)
            //        .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
            //        .WithReferences(typeof(JObject).Assembly))
            //        .RunAsync().Result.ReturnValue?.Value<int>() ?? 0;
                
            //    script = CSharpScript
            //        .Create($"int Value = {tmpInt};")
            //        .ContinueWith($"float Factor = {constraints.Scaling.ToString(CultureInfo.InvariantCulture)}f;")
            //        .ContinueWith(CALCULATE_SCALE_DEPENDING_VALUES)
            //        .WithOptions(ScriptOptions.Default.WithImports("System.Globalization"))
            //        .RunAsync().Result.ReturnValue.ToString();

            //    constraints.Minimum = CSharpScript.Create<float>(script).RunAsync().Result.ReturnValue;
                  
            //    script = CSharpScript
            //        .Create<string>($"string JsonString = @\"{doubleEscapedJson}\";")
            //        .ContinueWith("string TokenPath = \"constraints.max\";")
            //        .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
            //        .RunAsync().Result.ReturnValue.ToString();

            //    tmpInt = CSharpScript.Create<JToken>(script)
            //        .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
            //            .WithReferences(typeof(JObject).Assembly))
            //        .RunAsync().Result.ReturnValue?.Value<int>() ?? 0;

            //   script = CSharpScript
            //        .Create($"int Value = {tmpInt};")
            //        .ContinueWith($"float Factor = {constraints.Scaling.ToString(CultureInfo.InvariantCulture)}f;")
            //        .ContinueWith(CALCULATE_SCALE_DEPENDING_VALUES)
            //        .WithOptions(ScriptOptions.Default.WithImports("System.Globalization"))
            //        .RunAsync().Result.ReturnValue.ToString();

            //    constraints.Maximum = CSharpScript.Create<float>(script).RunAsync().Result.ReturnValue;

            //    script = CSharpScript
            //        .Create<string>($"string JsonString = @\"{doubleEscapedJson}\";")
            //        .ContinueWith("string TokenPath = \"constraints.steps\";")
            //        .ContinueWith(GET_VALUE_FROM_JSON_FOR_LINQ)
            //        .RunAsync().Result.ReturnValue.ToString();

            //    tmpInt = CSharpScript.Create<JToken>(script)
            //        .WithOptions(ScriptOptions.Default.WithImports("Newtonsoft.Json.Linq")
            //            .WithReferences(typeof(JObject).Assembly))
            //        .RunAsync().Result.ReturnValue?.Value<int>() ?? 0;

            //    script = CSharpScript
            //        .Create($"int Value = {tmpInt};")
            //        .ContinueWith($"float Factor = {constraints.Scaling.ToString(CultureInfo.InvariantCulture)}f;")
            //        .ContinueWith(CALCULATE_SCALE_DEPENDING_VALUES)
            //        .WithOptions(ScriptOptions.Default.WithImports("System.Globalization"))
            //        .RunAsync().Result.ReturnValue.ToString();

            //    constraints.Step = CSharpScript.Create<float>(script).RunAsync().Result.ReturnValue;

            //    newVarMsg.Constraints = constraints;

            //    returnValue.Add(newVarMsg);
            //}

            return returnList;
        }

        private ImpVariableDataTypes GetVariableDataType(string type)
        {
            switch (type)
            {
                case "string":
                    return ImpVariableDataTypes.dtString;
                case "float":
                    return ImpVariableDataTypes.dtFloat;
                case "int":
                    return ImpVariableDataTypes.dtInt;
                case "enum":
                    return ImpVariableDataTypes.dtEnum;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Value must be string, float or int" );
            }
        }
    }


    /*{"result":0,"deviceInfos":[{"deviceIndex":1,"info":{"name":"CMCIII-PU INT","type":"CMCIII-PU","serialNumber":40219080,"canBus":0,"canPos":1,"location":"CMCIII Network","orderNumber":"7030.000","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V3.15.40_2","hwRevision":"V2.04","productionDate":"49.2011","interface":"BUS 1.01"}},{"deviceIndex":2,"info":{"name":"23","type":"CMCIII-DRC","serialNumber":43084545,"canBus":0,"canPos":2,"location":"CMCIII Network ","orderNumber":"7030.550","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V12.005","hwRevision":"V0300","productionDate":"08.2013","interface":"BUS 1.02"}},{"deviceIndex":3,"info":{"name":"CMCIII-DRC 42","type":"CMCIII-DRC","serialNumber":43153431,"canBus":0,"canPos":3,"location":"CMCIII Network","orderNumber":"7030.550","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V12.005","hwRevision":"V0100","productionDate":"48.2013","interface":"BUS 1.03"}},{"deviceIndex":4,"info":{"name":"CMCIII-GAT","type":"CMCIII-GAT","serialNumber":87100741,"canBus":0,"canPos":4,"location":"CMCIII Network","orderNumber":"7030.030","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V09.103","hwRevision":"V0517","productionDate":"48.2013","interface":"BUS 1.04"}},{"deviceIndex":5,"info":{"name":"CMCIII-ACC","type":"CMCIII-ACC","serialNumber":87033654,"canBus":0,"canPos":5,"location":"CMCIII Network","orderNumber":"7030.120","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V12.012","hwRevision":"V0000","productionDate":"02.2011","interface":"BUS 1.05"}},{"deviceIndex":6,"info":{"name":"CMCIII-VAN","type":"CMCIII-VAN","serialNumber":87244548,"canBus":0,"canPos":6,"location":"CMCIII Network","orderNumber":"7030.130","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V12.010","hwRevision":"V0200","productionDate":"41.2011","interface":"BUS 1.06"}},{"deviceIndex":7,"info":{"name":"Keypad vorne","type":"CMCIII-GRF","serialNumber":87220727,"canBus":0,"canPos":7,"location":"CMCIII Network","orderNumber":"7030.200","oid":0,"devStatus":2,"msgStatus":3,"swRevision":"V12.026","hwRevision":"V0000","productionDate":"02.2011","interface":"BUS 1.07"}},{"deviceIndex":8,"info":{"name":"CMCIII-HUM","type":"CMCIII-HUM","serialNumber":87071914,"canBus":0,"canPos":8,"location":"CMCIII Network","orderNumber":"7030.111","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V12.018","hwRevision":"V0202","productionDate":"46.2014","interface":"BUS 1.08"}},{"deviceIndex":9,"info":{"name":"CMCIII-SEN","type":"CMCIII-SEN","serialNumber":87073960,"canBus":0,"canPos":9,"location":"CMCIII Network","orderNumber":"7030.100","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V12.023","hwRevision":"V0000","productionDate":"02.2011","interface":"BUS 1.09"}},{"deviceIndex":10,"info":{"name":"CMCIII-DIF","type":"CMCIII-DIF","serialNumber":87191413,"canBus":0,"canPos":10,"location":"CMCIII Network","orderNumber":"7030.150","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V12.005","hwRevision":"V0200","productionDate":"02.2011","interface":"BUS 1.10"}},{"deviceIndex":11,"info":{"name":"CMCIII-UNI","type":"CMCIII-UNI","serialNumber":87073407,"canBus":0,"canPos":11,"location":"CMCIII Network","orderNumber":"7030.190","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V12.020","hwRevision":"V0200","productionDate":"47.2011","interface":"BUS 1.11"}},{"deviceIndex":12,"info":{"name":"CMCIII-TMP","type":"CMCIII-TMP","serialNumber":87194259,"canBus":0,"canPos":12,"location":"CMCIII Network","orderNumber":"7030.110","oid":0,"devStatus":2,"msgStatus":3,"swRevision":"V12.012","hwRevision":"V0000","productionDate":"02.2011","interface":"BUS 1.12"}},{"deviceIndex":13,"info":{"name":"CMCIII-TMP","type":"CMCIII-TMP","serialNumber":87221646,"canBus":0,"canPos":13,"location":"CMCIII Network","orderNumber":"7030.110","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V12.012","hwRevision":"V0200","productionDate":"38.2012","interface":"BUS 1.13"}},{"deviceIndex":14,"info":{"name":"Rauchmelder","type":"CMCIII-SEN","serialNumber":87242958,"canBus":0,"canPos":14,"location":"sdfsdfs","orderNumber":"7030.100","oid":0,"devStatus":2,"msgStatus":3,"swRevision":"V12.023","hwRevision":"V0000","productionDate":"02.2011","interface":"BUS 1.14"}},{"deviceIndex":15,"info":{"name":"CMCIII-HUM","type":"CMCIII-HUM","serialNumber":67214934,"canBus":0,"canPos":15,"location":"CMCIII Network","orderNumber":"7030.111","oid":0,"devStatus":2,"msgStatus":3,"swRevision":"V12.018","hwRevision":"V0202","productionDate":"07.2015","interface":"BUS 1.15"}},{"deviceIndex":16,"info":{"name":"CMCIII-GAT","type":"CMCIII-GAT","serialNumber":87250737,"canBus":1,"canPos":1,"location":"CMCIII Network","orderNumber":"7030.030","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V09.103","hwRevision":"V0300","productionDate":"42.2011","interface":"BUS 2.01"}},{"deviceIndex":17,"info":{"name":"CMCIII-POW","type":"CMCIII-POW","serialNumber":87254747,"canBus":1,"canPos":2,"location":"CMCIII Network","orderNumber":"7030.050","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V12.026","hwRevision":"V0000","productionDate":"02.2011","interface":"BUS 2.02"}},{"deviceIndex":18,"info":{"name":"Access controller","type":"Access controller","serialNumber":40219001,"canBus":2,"canPos":2,"location":"CMCIII Internal","orderNumber":"n/a","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V3.15.20_6","hwRevision":"Virtual","productionDate":"41.2016","interface":"Virtual"}},{"deviceIndex":19,"info":{"name":"zwei","type":"Access controller","serialNumber":40219002,"canBus":2,"canPos":3,"location":"CMCIII Internal","orderNumber":"n/a","oid":0,"devStatus":2,"msgStatus":1,"swRevision":"V3.15.20_6","hwRevision":"Virtual","productionDate":"17.2017","interface":"Virtual"}}]}*/
}
