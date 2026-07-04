import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DailyCount, Engagement, TopPost } from '../models/models';

@Injectable({ providedIn: 'root' })
export class StatsService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiHost}/api/me/stats`;

  postsPerDay(): Observable<DailyCount[]> {
    return this.http.get<DailyCount[]>(`${this.apiUrl}/posts-per-day`);
  }

  likesReceived(): Observable<DailyCount[]> {
    return this.http.get<DailyCount[]>(`${this.apiUrl}/likes-received`);
  }

  followersGrowth(): Observable<DailyCount[]> {
    return this.http.get<DailyCount[]>(`${this.apiUrl}/followers-growth`);
  }

  topPosts(): Observable<TopPost[]> {
    return this.http.get<TopPost[]>(`${this.apiUrl}/top-posts`);
  }

  engagement(): Observable<Engagement> {
    return this.http.get<Engagement>(`${this.apiUrl}/engagement-rate`);
  }
}
