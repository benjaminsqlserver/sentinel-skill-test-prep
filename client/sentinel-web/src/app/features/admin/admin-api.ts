import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AccountSecurityStatus, PageAccess, PagedResponse, Role, UserSummary } from '../../core/models/api-models';

@Injectable({ providedIn: 'root' })
export class AdminApi {
  private readonly http = inject(HttpClient);

  getUsers(search: string, page = 1, pageSize = 25): Observable<PagedResponse<UserSummary>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<PagedResponse<UserSummary>>('/api/users', { params });
  }

  getRoles(): Observable<Role[]> {
    return this.http.get<Role[]>('/api/roles');
  }

  getPageAccess(userId: string): Observable<PageAccess[]> {
    return this.http.get<PageAccess[]>(`/api/users/${encodeURIComponent(userId)}/page-access`);
  }

  getUserSecurity(userId: string): Observable<AccountSecurityStatus> {
    return this.http.get<AccountSecurityStatus>(`/api/users/${encodeURIComponent(userId)}/security`);
  }
}
