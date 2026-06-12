import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreatePostRequest, Post } from '../models/models';

@Injectable({ providedIn: 'root' })
export class PostService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7088/api/posts';

  getAll(): Observable<Post[]> {
    return this.http.get<Post[]>(this.apiUrl);
  }

  getById(id: number): Observable<Post> {
    return this.http.get<Post>(`${this.apiUrl}/${id}`);
  }

  create(post: CreatePostRequest): Observable<Post> {
    return this.http.post<Post>(this.apiUrl, post);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
