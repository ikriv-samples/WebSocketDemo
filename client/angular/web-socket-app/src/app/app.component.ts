import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import {webSocket, WebSocketSubject} from 'rxjs/webSocket';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {

  httpUrl: string;
  wsUrl: string;
  wsRequestButtonText: string = "WS Request";
  time: { value: string, isFromWebSocket : boolean };
  selectedServerType: string;
  selectedServerUrl: string;
  updateSocket: WebSocketSubject<string>;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    const hostPort = "localhost:20235";
    const endpoinUrl = "/api/time";
    this.httpUrl = "http://" + hostPort + endpoinUrl;
    this.wsUrl = "ws://" + hostPort + endpoinUrl;
  }

  connectHttp() {
    this.selectedServerType = "HTTP server";
    this.selectedServerUrl = this.httpUrl;

    this.http.get(this.selectedServerUrl)
      .subscribe( 
        data => {this.time = { value: data.toString(), isFromWebSocket: false }; },
        err => { console.log("Error: " + JSON.stringify(err)); this.time = err.message; });
  }

  connectWebSocket() {
    if (this.updateSocket) {
      this.disconnectWebSocket();
      return;
    }

    this.selectedServerType = "web socket";
    this.selectedServerUrl = this.wsUrl;
    this.wsRequestButtonText = "Disconnect";

    this.updateSocket = webSocket(this.wsUrl);
    this.updateSocket.subscribe( data => 
      {
        console.log("Received web socket " + data);
        this.time = { value: data, isFromWebSocket : true };
      });
  }

  disconnectWebSocket() {
    this.updateSocket.unsubscribe();
    this.updateSocket = null;
    this.wsRequestButtonText = "WS Request";
  }
}
