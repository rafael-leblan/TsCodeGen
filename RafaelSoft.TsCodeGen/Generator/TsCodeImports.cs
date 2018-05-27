using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RafaelSoft.TsCodeGen.Generator
{
    public static class TsCodeImports
    {
        public const string Lodash =
            "import * as _ from 'lodash';";
        public const string Injectable =
            "import { Injectable } from '@angular/core';";
        public const string NgZone =
            "import { NgZone } from '@angular/core';";
        public const string Subject =
            "import { Subject } from 'rxjs';";
        public const string Signalr =
            "import * as SignalR from '@aspnet/signalr';";
        public const string Environment =
            "import { environment } from 'environments/environment';";
        public const string HttpImports =
            "import { HttpClient, HttpHeaders, HttpParams, HttpParameterCodec } from '@angular/common/http';";
        public const string InjectionTokenImports =
            "import { InjectionToken, Inject, Provider } from '@angular/core';";
        public const string rxjs_take =
            "import { take } from 'rxjs/operators';";
        public const string ionicNativeHttp =
            "import { HTTP } from '@ionic-native/http';";


    }
}
