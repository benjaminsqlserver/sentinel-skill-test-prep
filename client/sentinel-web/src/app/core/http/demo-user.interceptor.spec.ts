import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Session } from '../session/session';
import { DEMO_USER_HEADER, demoUserInterceptor } from './demo-user.interceptor';

describe('demoUserInterceptor', () => {
  const email = signal<string | null>(null);
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([demoUserInterceptor])),
        provideHttpClientTesting(),
        { provide: Session, useValue: { signedInEmail: email.asReadonly() } },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('adds the header to API calls when someone is signed in', () => {
    email.set('ada@sentinel.dev');

    http.get('/api/me').subscribe();

    expect(backend.expectOne('/api/me').request.headers.get(DEMO_USER_HEADER)).toBe('ada@sentinel.dev');
  });

  it('sends nothing when signed out', () => {
    email.set(null);

    http.get('/api/me').subscribe();

    expect(backend.expectOne('/api/me').request.headers.has(DEMO_USER_HEADER)).toBe(false);
  });

  it('never leaks the identity to other origins', () => {
    email.set('ada@sentinel.dev');

    http.get('https://example.com/api/thing').subscribe();

    expect(backend.expectOne('https://example.com/api/thing').request.headers.has(DEMO_USER_HEADER)).toBe(false);
  });
});
