import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-time-view',
  templateUrl: './time-view.component.html',
  styleUrls: ['./time-view.component.css']
})
export class TimeViewComponent {
  time: { value: string, isFromWebSocket : boolean };

  setTime( value: any, isFromWebSocket: boolean ) {
    this.time = { value : value.toString(), isFromWebSocket };
  }

  clearTime() {
    this.time = null;
  }
}
