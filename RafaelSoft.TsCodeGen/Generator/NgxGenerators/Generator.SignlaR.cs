using System;
using System.Linq;
using RafaelSoft.TsCodeGen.Common;
using RafaelSoft.TsCodeGen.Models;

namespace RafaelSoft.TsCodeGen.Generator.NgxGenerators
{
    public class GeneratorNgxSignalR
    {
        public const string Ts_SignalRConnectionState = @"enum SignalRConnectionState {
  Connecting = 0,
  Connected = 1,
  Reconnecting = 2,
  Disconnected = 4
}";

        public TsMethodCollection HubInterface { get; set; }
        public TsMethodCollection ConsumerInterface { get; set; }
        public string AngularClassName { get; set; } = "SignalRService";
        public string SignalRHubName { get; set; } = "hub";
        public long ReconnectRetryDelayMs { get; set; } = 3000;
        public ITsClassGenerationConfig TsClassGenConfig { get; set; } = new TsClassGenerationManualConfig(); // NOTE: default

        public GeneratorNgxSignalR(TsImportsManager importsManager)
        {
            importsManager.AddImportStatements(new[]
            {
                TsCodeImports.Injectable,
                TsCodeImports.Subject,
                TsCodeImports.Signalr,
                TsCodeImports.NgZone,
            });
        }

        public string Generate()
        {
            var code_makeRootProviders = NgxTsSnippets.Service_makeRootProviders(AngularClassName);
            var code_requestCalls_interface = HubInterface.Methods
                .ConditionallyIf(TsClassGenConfig.SortMethodsAlphabetically, thisLinq => thisLinq.OrderBy(m => m.MethodName))
                .Select(Generate_invocation_interface)
                .StringJoin("\n");
            var code_requestCalls = HubInterface.Methods
                .ConditionallyIf(TsClassGenConfig.SortMethodsAlphabetically, thisLinq => thisLinq.OrderBy(m => m.MethodName))
                .Select(Generate_invocation)
                .StringJoin("\n");
            var code_sigRPushesSubjectFields_interface = ConsumerInterface.Methods
                .ConditionallyIf(TsClassGenConfig.SortMethodsAlphabetically, thisLinq => thisLinq.OrderBy(m => m.MethodName))
                .Select(Generate_sigRPushSubjectField_interface)
                .StringJoin("\n");
            var code_sigRPushesSubjectFields = ConsumerInterface.Methods
                .ConditionallyIf(TsClassGenConfig.SortMethodsAlphabetically, thisLinq => thisLinq.OrderBy(m => m.MethodName))
                .Select(Generate_sigRPushSubjectField)
                .StringJoin("\n");
            var code_sigRPushes = ConsumerInterface.Methods
                .ConditionallyIf(TsClassGenConfig.SortMethodsAlphabetically, thisLinq => thisLinq.OrderBy(m => m.MethodName))
                .Select(Generate_sigRPushCode)
                .StringJoin("\n");
            return $@"
{code_makeRootProviders}

export interface I{AngularClassName} {{
  readonly event_connectivityChanged:Subject<void>;
  { code_sigRPushesSubjectFields_interface.IndentEveryLine("  ", skipFirst: true) }
  isConnected():boolean;
  getConnectionId():string;
  { code_requestCalls_interface.IndentEveryLine("  ", skipFirst: true) }
}}

@Injectable()
export class {AngularClassName} implements I{AngularClassName} {{

  public readonly HubName:string = '{SignalRHubName}';
  public readonly ReconnectRetryDelayMs:number = {ReconnectRetryDelayMs};
  
  private connection: SignalR.HubConnection;
  _isConnected: boolean = false;

  public readonly event_connectivityChanged:Subject<void> = new Subject<void>();
  { code_sigRPushesSubjectFields.IndentEveryLine("  ", skipFirst: true) }
  
  constructor(
    @Inject({AngularClassName}_apiUrl)
    private apiUrlPrefix: string,
    private ngZone: NgZone,
  ) {{
    this.connection = new SignalR.HubConnectionBuilder()
      .withUrl(apiUrlPrefix +'/'+ this.HubName)
      .configureLogging(SignalR.LogLevel.Information)
      .build();
    
    { code_sigRPushes.Trim().IndentEveryLine("    ", skipFirst: true) }
    
    this.connection.onclose(error => {{
      // connection was closed so start the reconnect attempt
      this.setMyConnectivityFlag(false);
      setTimeout(() => {{
        this.startConnection();
      }}, this.ReconnectRetryDelayMs);
    }});

    // NOTE: need to call startConnection manually
    //this.startConnection();
  }}

  //-------------------- publics -----------------------

  public startConnection() {{
    this.connection.start().then(() => {{
      //console.log('SignalR Connected successfully');
      this.setMyConnectivityFlag(true);
    }});
  }}

  public isConnected():boolean {{
    return this._isConnected;
  }}

  public getConnectionId():string {{
    //return this.proxy.connection.id;
    return 'TODO: 907f289c: find how to return connection id';
  }}

  { code_requestCalls.Trim().IndentEveryLine("  ", skipFirst: true) }

  //-------------------- privates -----------------------

  private setMyConnectivityFlag(flag:boolean) {{
    if (flag != this._isConnected) {{
      this._isConnected = flag;
      this.ngZone.run(() => {{
        this.event_connectivityChanged.next();
      }});
    }}
  }}
}}
";
        }

        private string Generate_invocation_interface(TsMethodSpec spec)
        {
            var paramsStrWithType = spec.ParamSpecs
                .Select(p => $"{p.ParamName}:{p.GetTsParamTypeName(TsClassGenConfig)}")
                .StringJoin(", ");
            return $@"signalR_{spec.MethodName}({paramsStrWithType});";
        }

        private string Generate_invocation(TsMethodSpec spec)
        {
            var paramsStrWithType = spec.ParamSpecs
                .Select(p => $"{p.ParamName}:{p.GetTsParamTypeName(TsClassGenConfig)}")
                .StringJoin(", ");
            var paramsStr = spec.ParamSpecs
                .Select(p => p.ParamName)
                .StringJoin(", ");
            return $@"
public signalR_{spec.MethodName}({paramsStrWithType}) {{
  this.connection.invoke('{spec.MethodName}', {paramsStr});
}}";
        }

        private string Generate_sigRPushSubjectField_interface(TsMethodSpec spec)
        {
            if (spec.ParamSpecs.Length > 1)
                throw new ArgumentException($"Angular invocation method should only have 1 param, because Subject<T> takes 1 param. Method: {spec.MethodName}");
            var param1 = spec.ParamSpecs[0];
            return $"readonly event_{spec.MethodName}:Subject<{param1.GetTsParamTypeName(TsClassGenConfig)}>;";
        }
        private string Generate_sigRPushSubjectField(TsMethodSpec spec)
        {
            if (spec.ParamSpecs.Length > 1)
                throw new ArgumentException($"Angular invocation method should only have 1 param, because Subject<T> takes 1 param. Method: {spec.MethodName}");
            var param1 = spec.ParamSpecs[0];
            return $"public readonly event_{spec.MethodName}:Subject<{param1.GetTsParamTypeName(TsClassGenConfig)}> = new Subject<{param1.GetTsParamTypeName(TsClassGenConfig)}>();";
        }
        private string Generate_sigRPushCode(TsMethodSpec spec)
        {
            if (spec.ParamSpecs.Length > 1)
                throw new ArgumentException($"Angular invocation method should only have 1 param, because Subject<T> takes 1 param. Method: {spec.MethodName}");
            var param1 = spec.ParamSpecs[0];
            return $@"
this.connection.on('{spec.MethodName}', ({param1.ParamName}:{param1.GetTsParamTypeName(TsClassGenConfig)}) => {{
  this.ngZone.run(() => {{
    //console.log('SignalR:{spec.MethodName}', {param1.ParamName});
    this.event_{spec.MethodName}.next({param1.ParamName});
  }});
}});";
        }
    }

}
