import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreatePostRequest, LikeResponse, PagedResult, Post } from '../models/models';

@Injectable({ providedIn: 'root' })
export class PostService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7088/api/posts';

  getAll(page = 1, pageSize = 10): Observable<PagedResult<Post>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<Post>>(this.apiUrl, { params });
  }

  // Feed personalizado: solo posts de los usuarios que sigo
  getFeed(page = 1, pageSize = 10): Observable<PagedResult<Post>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<Post>>(`${this.apiUrl}/feed`, { params });
  }

  getById(id: number): Observable<Post> {
    return this.http.get<Post>(`${this.apiUrl}/${id}`);
  }

  create(post: CreatePostRequest): Observable<Post> {
    return this.http.post<Post>(this.apiUrl, post);
  }

  // Sube una imagen (multipart/form-data) y devuelve su URL relativa.
  // El campo debe llamarse 'file' para enlazar con el IFormFile del backend.
  uploadImage(file: File): Observable<{ url: string }> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<{ url: string }>(`${this.apiUrl}/upload-image`, fd);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  like(id: number): Observable<LikeResponse> {
    // POST sin body: el backend identifica al usuario por el token
    return this.http.post<LikeResponse>(`${this.apiUrl}/${id}/like`, {});
  }

  unlike(id: number): Observable<LikeResponse> {
    return this.http.delete<LikeResponse>(`${this.apiUrl}/${id}/like`);
  }
}
