import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MyRequest } from '../models/my-request.model';
import { MyRequests } from './my-requests';

const req = (over: Partial<MyRequest> = {}): MyRequest => ({
  requestId: 'F-1',
  serviceName: 'הנחה בארנונה',
  authorityName: 'עיריית חדרה',
  status: 'בטיפול',
  actionRequired: false,
  isOverdue: false,
  submittedAt: '02/09/2026',
  lastUpdate: '20/09/2026',
  ...over,
});

describe('MyRequests', () => {
  let http: HttpTestingController;

  function create() {
    TestBed.configureTestingModule({
      imports: [MyRequests],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(MyRequests);
    fixture.detectChanges();
    return fixture;
  }

  const text = (f: { nativeElement: HTMLElement }) => f.nativeElement.textContent ?? '';

  it('shows the empty message when there are no requests', async () => {
    const fixture = create();
    http.expectOne('/api/my-requests').flush({ requests: [] });
    await fixture.whenStable();

    expect(text(fixture)).toContain('עדיין לא הגשת בקשות');
  });

  it('shows a friendly error and retries when the service is unavailable', async () => {
    const fixture = create();
    http.expectOne('/api/my-requests').flush('boom', { status: 503, statusText: 'Unavailable' });
    await fixture.whenStable();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[role="alert"]')).toBeTruthy();

    (Array.from(root.querySelectorAll('button')).find((b) => b.textContent?.includes('נסה שוב')) as HTMLButtonElement).click();
    http.expectOne('/api/my-requests').flush({ requests: [req()] });
    await fixture.whenStable();

    expect(root.querySelector('[role="alert"]')).toBeNull();
    expect(text(fixture)).toContain('הנחה בארנונה');
  });

  it('highlights action-required requests with a call to action', async () => {
    const fixture = create();
    http.expectOne('/api/my-requests').flush({ requests: [req({ actionRequired: true })] });
    await fixture.whenStable();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.card--action')).toBeTruthy();
    expect(text(fixture)).toContain('נדרשת פעולה מצדך');
    expect(text(fixture)).toContain('השלמת פעולה');
  });

  it('marks overdue requests', async () => {
    const fixture = create();
    http.expectOne('/api/my-requests').flush({ requests: [req({ isOverdue: true })] });
    await fixture.whenStable();

    expect(text(fixture)).toContain('חריגה מזמן הטיפול');
  });
});
