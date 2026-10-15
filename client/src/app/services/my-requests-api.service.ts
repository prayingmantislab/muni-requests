import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { MyRequest, MyRequestsResponse } from '../models/my-request.model';

/** עוטף את הקריאה ל-API. הרכיב לא יודע כלום על HTTP. */
@Injectable({ providedIn: 'root' })
export class MyRequestsApiService {
  private readonly http = inject(HttpClient);

  /** כתובת יחסית: בפיתוח ה-proxy מעביר אותה לשרת ה-.NET. */
  getMyRequests(): Observable<MyRequest[]> {
    return this.http.get<MyRequestsResponse>('/api/my-requests').pipe(map((r) => r.requests));
  }
}
