import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SearchResult } from '../models/models';

@Injectable({ providedIn: 'root' })
export class SearchService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7088/api/search';

  search(q: string): Observable<SearchResult> {
    const params = new HttpParams().set('q', q);
    return this.http.get<SearchResult>(this.apiUrl, { params });
  }
}
