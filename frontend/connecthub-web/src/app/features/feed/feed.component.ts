import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PostService } from '../../core/services/post.service';
import { AuthService } from '../../core/services/auth.service';
import { Post } from '../../core/models/models';
import { PostCardComponent } from '../../shared/post-card.component';
import { NotificationsBellComponent } from '../../shared/notifications-bell.component';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-feed',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PostCardComponent, NotificationsBellComponent],
  template: `
    <div class="feed-container">
      <header class="top-bar">
        <h2>Hola, {{ auth.currentUser()?.username }}</h2>
        <nav>
          <app-notifications-bell />
          <a routerLink="/search">Buscar</a>
          <a [routerLink]="['/profile', auth.currentUser()?.userId]">Mi perfil</a>
          <a routerLink="/dashboard">Dashboard</a>
          <button class="theme-btn" (click)="theme.toggle()" aria-label="Cambiar tema">
            {{ theme.theme() === 'dark' ? '☀️' : '🌙' }}
          </button>
          <button (click)="logout()">Cerrar sesión</button>
        </nav>
      </header>

      <section class="new-post">
        <h3>Nuevo post</h3>
        <form [formGroup]="form" (ngSubmit)="createPost()">
          <textarea
            formControlName="content"
            placeholder="¿Qué estás pensando?"
            rows="3"
          ></textarea>

          <div class="upload-row">
            <input type="file" accept="image/*" (change)="onFileSelected($event)" />
            @if (previewUrl()) {
              <div class="preview">
                <img [src]="previewUrl()!" alt="preview" />
                <button type="button" class="link" (click)="clearImage()">Quitar</button>
              </div>
            }
          </div>

          <button type="submit" [disabled]="form.invalid || publishing()">
            {{ publishing() ? 'Publicando...' : 'Publicar' }}
          </button>
        </form>
      </section>

      <section class="posts">
        <div class="feed-toggle">
          <button [class.active]="scope() === 'global'" (click)="setScope('global')">Feed global</button>
          <button [class.active]="scope() === 'mine'" (click)="setScope('mine')">Mi feed</button>
        </div>

        @if (loading()) {
          <p>Cargando...</p>
        } @else if (posts().length === 0) {
          <p>
            @if (scope() === 'mine') {
              Aún no sigues a nadie o no han publicado. ¡Explora y sigue gente!
            } @else {
              No hay posts todavía. ¡Sé el primero!
            }
          </p>
        } @else {
          @for (post of posts(); track post.id) {
            <app-post-card
              [post]="post"
              (deleted)="onDeleted($event)"
              (commentsCount)="onCommentsCount(post.id, $event)"
            />
          }
          @if (loadingMore()) { <p class="loading-more">Cargando más...</p> }
        }

        <!-- Centinela: cuando entra en viewport, cargamos la siguiente página -->
        <div #sentinel></div>
      </section>
    </div>
  `,
  styles: [`
    .feed-container { max-width: 600px; margin: 2rem auto; padding: 1rem; }
    .top-bar { display: flex; justify-content: space-between; align-items: center; }
    .top-bar nav { display: flex; gap: 1rem; align-items: center; }
    .top-bar nav a { color: #1971c2; text-decoration: none; }
    section { margin-top: 2rem; }
    textarea { width: 100%; padding: 0.5rem; resize: vertical; }
    button { padding: 0.5rem 1rem; cursor: pointer; }
    .feed-toggle { display: flex; gap: 0.5rem; margin-bottom: 1rem; }
    .feed-toggle button { border: 1px solid #ccc; background: #fff; border-radius: 6px; }
    .feed-toggle button.active { background: #1971c2; color: #fff; border-color: #1971c2; }
    .theme-btn { background: none; border: none; font-size: 1.1rem; cursor: pointer; }
    .loading-more { text-align: center; color: var(--muted, #888); }
    .upload-row { margin: 0.5rem 0; }
    .preview { margin-top: 0.5rem; }
    .preview img { max-width: 200px; border-radius: 8px; display: block; }
    button.link { background: none; border: none; color: #c92a2a; cursor: pointer; padding: 0.25rem 0; }
  `]
})
export class FeedComponent implements OnInit, AfterViewInit, OnDestroy {
  private postService = inject(PostService);
  private fb = inject(FormBuilder);
  auth = inject(AuthService);
  theme = inject(ThemeService);

  @ViewChild('sentinel') sentinel?: ElementRef<HTMLElement>;
  private observer?: IntersectionObserver;

  posts = signal<Post[]>([]);
  loading = signal(true);
  loadingMore = signal(false);
  hasMore = signal(false);
  page = signal(1);
  scope = signal<'global' | 'mine'>('global');
  selectedFile = signal<File | null>(null);
  previewUrl = signal<string | null>(null);
  publishing = signal(false);

  form = this.fb.nonNullable.group({
    content: ['', [Validators.required, Validators.maxLength(1000)]]
  });

  ngOnInit() {
    this.loadPosts();
  }

  ngAfterViewInit() {
    // IntersectionObserver: dispara loadMore cuando el centinela se acerca al viewport.
    this.observer = new IntersectionObserver(
      entries => { if (entries[0].isIntersecting) this.loadMore(); },
      { rootMargin: '200px' }
    );
    if (this.sentinel) this.observer.observe(this.sentinel.nativeElement);
  }

  ngOnDestroy() {
    this.observer?.disconnect();
  }

  setScope(scope: 'global' | 'mine') {
    if (this.scope() === scope) return;
    this.scope.set(scope);
    this.loadPosts();
  }

  // Carga (o recarga) desde la página 1.
  loadPosts() {
    this.loading.set(true);
    this.page.set(1);
    this.fetch(1, data => {
      this.posts.set(data.items);
      this.loading.set(false);
    });
  }

  // Carga la siguiente página y la añade al final (infinite scroll).
  loadMore() {
    if (!this.hasMore() || this.loadingMore() || this.loading()) return;
    const next = this.page() + 1;
    this.loadingMore.set(true);
    this.fetch(next, data => {
      this.posts.update(curr => [...curr, ...data.items]);
      this.page.set(next);
      this.loadingMore.set(false);
    });
  }

  private fetch(page: number, done: (data: { items: Post[]; hasMore: boolean }) => void) {
    const req = this.scope() === 'mine'
      ? this.postService.getFeed(page)
      : this.postService.getAll(page);

    req.subscribe({
      next: (data) => {
        this.hasMore.set(data.hasMore);
        done(data);
      },
      error: () => {
        this.loading.set(false);
        this.loadingMore.set(false);
      }
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedFile.set(file);
    // Preview local con object URL (sin subir todavía al servidor).
    this.previewUrl.set(file ? URL.createObjectURL(file) : null);
  }

  clearImage() {
    this.selectedFile.set(null);
    this.previewUrl.set(null);
  }

  createPost() {
    if (this.form.invalid || this.publishing()) return;
    this.publishing.set(true);

    const file = this.selectedFile();
    if (file) {
      // Flujo en dos pasos: subir imagen -> crear post con la URL devuelta.
      this.postService.uploadImage(file).subscribe({
        next: (res) => this.submitPost(res.url),
        error: () => this.publishing.set(false)
      });
    } else {
      this.submitPost(undefined);
    }
  }

  private submitPost(imageUrl?: string) {
    this.postService.create({ ...this.form.getRawValue(), imageUrl }).subscribe({
      next: (newPost) => {
        this.posts.update(curr => [newPost, ...curr]);
        this.form.reset();
        this.clearImage();
        this.publishing.set(false);
      },
      error: () => this.publishing.set(false)
    });
  }

  onDeleted(id: number) {
    this.posts.update(curr => curr.filter(p => p.id !== id));
  }

  onCommentsCount(postId: number, count: number) {
    this.posts.update(curr =>
      curr.map(p => (p.id === postId ? { ...p, commentsCount: count } : p))
    );
  }

  private router = inject(Router);

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
