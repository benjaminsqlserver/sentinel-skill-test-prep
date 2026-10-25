import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AccountSecurityStatus } from '../../core/models/api-models';

/** The account security endpoints, and nothing else. Components never build URLs. */
@Injectable({ providedIn: 'root' })
export class SecurityApi {
  private readonly http = inject(HttpClient);

  getStatus(): Observable<AccountSecurityStatus> {
    return this.http.get<AccountSecurityStatus>('/api/me/security');
  }

  /** "That was me." The key goes in the URL, so it must be escaped: keys contain ':'. */
  acknowledge(findingKey: string): Observable<void> {
    return this.http.post<void>(`/api/me/security/findings/${encodeURIComponent(findingKey)}/acknowledge`, null);
  }
}
