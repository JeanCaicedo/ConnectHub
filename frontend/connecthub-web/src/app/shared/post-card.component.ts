import { Component, inject, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PostService } from '../core/services/post.service';
import { AuthService } from '../core/services/auth.service';
import { Post } from '../core/models/models';
import { PostCommentsComponent } from '../features/feed/post-comments.component';

@Component({
  selector: 'app-post-card',
  standalone: true,
  imports: [DatePipe, RouterLink, PostCommentsComponent],
  template: `
    <article class="post">
      <header>
        <a [routerLink]="['/profile', post().userId]" class="author">
          {{ '@' + post().username }}
        </a>
        <span>{{ post().createdAt | date:'short' }}</span>
      </header>

      <p class="content">
        @for (seg of segments(post().content); track $index) {
          @if (seg.tag) {
            <a [routerLink]="['/search']" [queryParams]="{ q: '#' + seg.text }">#{{ seg.text }}</a>
          } @else {
            <span>{{ seg.text }}</span>
          }
        }
      </p>

      @if (post().imageUrl) {
        <img class="post-image" [src]="imageSrc(post().imageUrl!)" alt="imagen del post" />
      }

      <div class="post-actions">
        <button
          class="like-btn"
          [class.liked]="post().isLikedByCurrentUser"
          (click)="toggleLike()"
        >
          {{ post().isLikedByCurrentUser ? 'Te gusta' : 'Me gusta' }} ({{ post().likesCount }})
        </button>
        <button class="comment-btn" (click)="showComments.set(!showComments())">
          Comentarios ({{ post().commentsCount }})
        </button>
        @if (post().userId === auth.currentUser()?.userId) {
          <button (click)="remove()">Eliminar</button>
        }
      </div>

      @if (showComments()) {
        <app-post-comments
          [postId]="post().id"
          (countChanged)="commentsCount.emit($event)"
        />
      }
    </article>
  `,
  styles: [`
    .post { border: 1px solid var(--border, #ddd); border-radius: 8px; padding: 1rem; margin-bottom: 1rem; background: var(--card, #fff); }
    .post header { display: flex; justify-content: space-between; margin-bottom: 0.5rem; font-size: 0.9rem; color: #666; }
    .author { color: #1971c2; text-decoration: none; font-weight: 600; }
    .author:hover { text-decoration: underline; }
    .post-image { max-width: 100%; border-radius: 8px; margin: 0.5rem 0; }
    .content a { color: #1971c2; text-decoration: none; }
    .content a:hover { text-decoration: underline; }
    .post-actions { display: flex; gap: 0.5rem; margin-top: 0.5rem; }
    button { padding: 0.5rem 1rem; cursor: pointer; }
    .like-btn, .comment-btn { border: 1px solid #ccc; background: #fff; border-radius: 6px; }
    .like-btn.liked { background: #ffe3e3; border-color: #ff8787; color: #c92a2a; font-weight: 600; }
  `]
})
export class PostCardComponent {
  post = input.required<Post>();
  // Avisamos al padre cuando este post se borra, para quitarlo de su lista.
  deleted = output<number>();
  // Nuevo conteo de comentarios (para que el padre actualice su copia del post).
  commentsCount = output<number>();

  private postService = inject(PostService);
  auth = inject(AuthService);

  showComments = signal(false);

  // Las imagenes se sirven desde el backend con ruta relativa (/uploads/...).
  private apiHost = 'https://localhost:7088';
  imageSrc(url: string): string {
    return url.startsWith('http') ? url : `${this.apiHost}${url}`;
  }

  // Divide el contenido en segmentos de texto y de #hashtag para poder
  // renderizar los hashtags como enlaces a la búsqueda.
  segments(content: string): { text: string; tag: boolean }[] {
    return content.split(/(#\w+)/g)
      .filter(part => part.length > 0)
      .map(part => part.startsWith('#')
        ? { text: part.slice(1), tag: true }
        : { text: part, tag: false });
  }

  toggleLike() {
    const p = this.post();
    const wasLiked = p.isLikedByCurrentUser;

    // Optimistic update sobre el objeto (el padre mantiene la referencia).
    p.isLikedByCurrentUser = !wasLiked;
    p.likesCount += wasLiked ? -1 : 1;

    const request = wasLiked
      ? this.postService.unlike(p.id)
      : this.postService.like(p.id);

    request.subscribe({
      next: (res) => {
        p.isLikedByCurrentUser = res.liked;
        p.likesCount = res.likesCount;
      },
      error: () => {
        // Revertir si falla
        p.isLikedByCurrentUser = wasLiked;
        p.likesCount += wasLiked ? 1 : -1;
      }
    });
  }

  remove() {
    const id = this.post().id;
    this.postService.delete(id).subscribe({
      next: () => this.deleted.emit(id)
    });
  }
}
