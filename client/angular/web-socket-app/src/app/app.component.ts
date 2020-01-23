import { Component, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import {webSocket, WebSocketSubject} from 'rxjs/webSocket';
import { ServerInfoComponent } from './server-info/server-info.component';
import { TimeViewComponent } from './time-view/time-view.component';
import { ConnectionStatusComponent } from './connection-status/connection-status.component';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {

  httpUrl: string;
  wsUrl: string;
  wsRequestButtonText: string = "WS Request";
  updateSocket: WebSocketSubject<string>;

  @ViewChild(ServerInfoComponent)
  serverInfo : ServerInfoComponent;

  @ViewChild(TimeViewComponent)
  time : TimeViewComponent;

  @ViewChild(ConnectionStatusComponent)
  status : ConnectionStatusComponent;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    const hostPort = "localhost:20235";
    const endpoinUrl = "/api/time";
    this.httpUrl = "http://" + hostPort + endpoinUrl;
    this.wsUrl = "ws://" + hostPort + endpoinUrl;
  }

  connectHttp() {
    this.serverInfo.setServerInfo("HTTP sevrer", this.httpUrl);

    this.http.get(this.httpUrl)
      .subscribe( 
        data => {this.time.setTime(data, false);},
        err => { this.status.setHttpStatus(err); });
  }

  connectWebSocket() {
    if (this.updateSocket) {
      this.onWebSocketDisconnected("Web socket closed by client");
      return;
    }

    this.serverInfo.setServerInfo("web socket", this.wsUrl);
    this.wsRequestButtonText = "Disconnect";
    this.time.clearTime();
    this.status.clearStatus();

    this.updateSocket = webSocket(this.wsUrl);
    this.updateSocket.subscribe( data => 
      {
        this.time.setTime(data, true);
        this.status.clearStatus();
      },
      err => { this.onWebSocketDisconnected("Web socket error: " + err); },
      () => { this.onWebSocketDisconnected("Web socket closed by server"); });
  }

  onWebSocketDisconnected(status) {
    this.updateSocket.unsubscribe();
    this.status.setStatus(status);
    this.updateSocket = null;
    this.wsRequestButtonText = "WS Request";
  }
}
