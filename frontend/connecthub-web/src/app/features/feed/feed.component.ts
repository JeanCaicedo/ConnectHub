import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { PostService } from '../../core/services/post.service';
import { AuthService } from '../../core/services/auth.service';
import { Post } from '../../core/models/models';

@Component({
  selector: 'app-feed',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  template: `
    <div class="feed-container">
      <header>
        <h2>Hola, {{ auth.currentUser()?.username }}</h2>
        <button (click)="logout()">Cerrar sesión</button>
      </header>

      <section class="new-post">
        <h3>Nuevo post</h3>
        <form [formGroup]="form" (ngSubmit)="createPost()">
          <textarea
            formControlName="content"
            placeholder="¿Qué estás pensando?"
            rows="3"
          ></textarea>
          <button type="submit" [disabled]="form.invalid">Publicar</button>
        </form>
      </section>

      <section class="posts">
        <h3>Feed</h3>

        @if (loading()) {
          <p>Cargando...</p>
        } @else if (posts().length === 0) {
          <p>No hay posts todavía. ¡Sé el primero!</p>
        } @else {
          @for (post of posts(); track post.id) {
            <article class="post">
              <header>
                <strong>{{ '@' + post.username }}</strong>
                <span>{{ post.createdAt | date:'short' }}</span>
              </header>
              <p>{{ post.content }}</p>
              @if (post.userId === auth.currentUser()?.userId) {
                <button (click)="deletePost(post.id)">Eliminar</button>
              }
            </article>
          }
        }
      </section>
    </div>
  `,
  styles: [`
    .feed-container { max-width: 600px; margin: 2rem auto; padding: 1rem; }
    header { display: flex; justify-content: space-between; align-items: center; }
    section { margin-top: 2rem; }
    textarea { width: 100%; padding: 0.5rem; resize: vertical; }
    .post { border: 1px solid #ddd; border-radius: 8px; padding: 1rem; margin-bottom: 1rem; }
    .post header { margin-bottom: 0.5rem; font-size: 0.9rem; color: #666; }
    button { padding: 0.5rem 1rem; cursor: pointer; }
  `]
})
export class FeedComponent implements OnInit {
  private postService = inject(PostService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  auth = inject(AuthService);

  posts = signal<Post[]>([]);
  loading = signal(true);

  form = this.fb.nonNullable.group({
    content: ['', [Validators.required, Validators.maxLength(1000)]]
  });

  ngOnInit() {
    this.loadPosts();
  }

  loadPosts() {
    this.loading.set(true);
    this.postService.getAll().subscribe({
      next: (data) => {
        this.posts.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  createPost() {
    if (this.form.invalid) return;

    this.postService.create(this.form.getRawValue()).subscribe({
      next: (newPost) => {
        this.posts.update(curr => [newPost, ...curr]);
        this.form.reset();
      }
    });
  }

  deletePost(id: number) {
    this.postService.delete(id).subscribe({
      next: () => this.posts.update(curr => curr.filter(p => p.id !== id))
    });
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
