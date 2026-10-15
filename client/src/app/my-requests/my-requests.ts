import { Component, DestroyRef, OnInit, inject, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MyRequest } from '../models/my-request.model';
import { MyRequestsApiService } from '../services/my-requests-api.service';

type ViewState = 'loading' | 'ready' | 'error';

/** רכיב "הבקשות שלי" באזור האישי. */
@Component({
  selector: 'app-my-requests',
  templateUrl: './my-requests.html',
  styleUrl: './my-requests.scss',
})
export class MyRequests implements OnInit {
  private readonly api = inject(MyRequestsApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly state = signal<ViewState>('loading');
  protected readonly requests = signal<MyRequest[]>([]);

  /** האזור האישי מחליט לאן מובילה הקריאה לפעולה (הנחה #6). */
  readonly actionRequested = output<MyRequest>();

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.api
      .getMyRequests()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (requests) => {
          this.requests.set(requests);
          this.state.set('ready');
        },
        error: () => this.state.set('error'),
      });
  }
}
