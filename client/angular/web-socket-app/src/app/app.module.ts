import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule } from "@angular/forms";

import { AppComponent } from './app.component';
import { ServerInfoComponent } from './server-info/server-info.component';
import { TimeViewComponent } from './time-view/time-view.component';
import { ConnectionStatusComponent } from './connection-status/connection-status.component';

@NgModule({
  declarations: [
    AppComponent,
    ServerInfoComponent,
    TimeViewComponent,
    ConnectionStatusComponent
  ],
  imports: [
    BrowserModule,
    FormsModule,
    HttpClientModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
