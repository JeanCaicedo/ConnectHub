import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SearchService } from '../../core/services/search.service';
import { Post, UserSummary } from '../../core/models/models';
import { PostCardComponent } from '../../shared/post-card.component';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [RouterLink, PostCardComponent],
  template: `
    <div class="search-page">
      <a routerLink="/feed" class="back">← Volver al feed</a>

      <div class="search-bar">
        <input
          #box
          [value]="query()"
          placeholder="Buscar personas, texto o #hashtag..."
          (keyup.enter)="go(box.value)"
        />
        <button (click)="go(box.value)">Buscar</button>
      </div>

      @if (query()) {
        <p class="muted">Resultados para «{{ query() }}»</p>

        @if (users().length > 0) {
          <h3>Personas</h3>
          <ul class="user-list">
            @for (u of users(); track u.id) {
              <li>
                <a [routerLink]="['/profile', u.id]">{{ '@' + u.username }}</a>
                @if (u.bio) { <span class="bio">{{ u.bio }}</span> }
              </li>
            }
          </ul>
        }

        <h3>Publicaciones</h3>
        @if (posts().length === 0) {
          <p class="muted">Sin publicaciones que coincidan.</p>
        } @else {
          @for (p of posts(); track p.id) {
            <app-post-card [post]="p" (deleted)="onDeleted($event)" />
          }
        }
      }
    </div>
  `,
  styles: [`
    .search-page { max-width: 600px; margin: 2rem auto; padding: 1rem; }
    .back { color: #1971c2; text-decoration: none; display: inline-block; margin-bottom: 1rem; }
    .search-bar { display: flex; gap: 0.5rem; margin-bottom: 1rem; }
    .search-bar input { flex: 1; padding: 0.5rem; }
    .search-bar button { padding: 0.5rem 1rem; cursor: pointer; }
    .user-list { list-style: none; padding: 0; }
    .user-list li { padding: 0.5rem 0; border-bottom: 1px solid var(--border, #eee); display: flex; gap: 0.75rem; align-items: baseline; }
    .user-list a { color: #1971c2; text-decoration: none; font-weight: 600; }
    .bio { color: #888; font-size: 0.85rem; }
    .muted { color: #888; }
  `]
})
export class SearchComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private searchService = inject(SearchService);

  query = signal('');
  posts = signal<Post[]>([]);
  users = signal<UserSummary[]>([]);

  ngOnInit() {
    // Reaccionar al queryParam 'q' permite que los links de #hashtag funcionen.
    this.route.queryParamMap.subscribe(params => {
      const q = params.get('q') ?? '';
      this.query.set(q);
      if (q) this.run(q);
    });
  }

  go(value: string) {
    const q = value.trim();
    if (q) this.router.navigate(['/search'], { queryParams: { q } });
  }

  private run(q: string) {
    this.searchService.search(q).subscribe(res => {
      this.posts.set(res.posts);
      this.users.set(res.users);
    });
  }

  onDeleted(id: number) {
    this.posts.update(curr => curr.filter(p => p.id !== id));
  }
}
