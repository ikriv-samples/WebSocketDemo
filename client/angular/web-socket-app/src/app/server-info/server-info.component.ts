import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-server-info',
  templateUrl: './server-info.component.html',
  styleUrls: ['./server-info.component.css']
})
export class ServerInfoComponent {

  serverType : string;
  serverUrl : string;

  setServerInfo(serverType: string, serverUrl: string) {
    this.serverType = serverType;
    this.serverUrl = serverUrl;
  }
}
