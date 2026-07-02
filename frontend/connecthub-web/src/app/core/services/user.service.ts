import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FollowResponse, Post, UserProfile } from '../models/models';

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7088/api/users';

  getProfile(id: number): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.apiUrl}/${id}`);
  }

  getUserPosts(id: number): Observable<Post[]> {
    return this.http.get<Post[]>(`${this.apiUrl}/${id}/posts`);
  }

  follow(id: number): Observable<FollowResponse> {
    return this.http.post<FollowResponse>(`${this.apiUrl}/${id}/follow`, {});
  }

  unfollow(id: number): Observable<FollowResponse> {
    return this.http.delete<FollowResponse>(`${this.apiUrl}/${id}/follow`);
  }

  updateBio(bio: string): Observable<{ id: number; username: string; bio?: string; avatarUrl?: string }> {
    return this.http.put<{ id: number; username: string; bio?: string; avatarUrl?: string }>(
      `${this.apiUrl}/me`, { bio });
  }

  uploadAvatar(file: File): Observable<{ avatarUrl: string }> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<{ avatarUrl: string }>(`${this.apiUrl}/me/avatar`, fd);
  }
}
