import { Component, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-connection-status',
  templateUrl: './connection-status.component.html',
  styleUrls: ['./connection-status.component.css']
})
export class ConnectionStatusComponent {

  status: string;

  setStatus(status:string) {
    this.status = status;
  }

  setHttpStatus(err: HttpErrorResponse ) {
    if (err.status) {
      this.setStatus(`HTTP error ${err.status}: ${err.statusText}`);
    }
    else {
      this.setStatus("Unknown HTTP error occurred, possibly a CORS issue" )
    }
  }

  clearStatus() {
    this.status = null;
  }

}
