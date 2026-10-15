import { Component } from '@angular/core';
import { MyRequests } from './my-requests/my-requests';

@Component({
  selector: 'app-root',
  imports: [MyRequests],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
