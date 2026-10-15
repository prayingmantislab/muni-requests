import { HttpClient, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, inject, isDevMode } from '@angular/core';
import { Observable, map, shareReplay, switchMap } from 'rxjs';

/**
 * פיתוח בלבד: מביא טוקן מ-/dev/token. באזור האישי האמיתי הטוקן כבר קיים
 * ב-session של המשתמש, ולכן שם ה-interceptor הזה לא פעיל.
 */
@Injectable({ providedIn: 'root' })
export class DevTokenService {
  private readonly http = inject(HttpClient);
  readonly token$: Observable<string> = this.http
    .post<{ token: string }>('/dev/token?idNumber=012345678', null)
    .pipe(
      map((r) => r.token),
      shareReplay(1), // מבקשים טוקן פעם אחת; אם נכשל, הניסיון הבא ינסה שוב
    );
}

export const devAuthInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isDevMode() || req.url.startsWith('/dev/')) {
    return next(req);
  }
  return inject(DevTokenService).token$.pipe(
    switchMap((token) => next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }))),
  );
};
