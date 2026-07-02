import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { UserService } from '../../core/services/user.service';
import { AuthService } from '../../core/services/auth.service';
import { Post, UserProfile } from '../../core/models/models';
import { PostCardComponent } from '../../shared/post-card.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [DatePipe, RouterLink, PostCardComponent],
  template: `
    <div class="profile-container">
      <a routerLink="/feed" class="back">← Volver al feed</a>

      @if (loading()) {
        <p>Cargando perfil...</p>
      } @else if (profile() === null) {
        <p>Usuario no encontrado.</p>
      }

      @if (!loading() && profile(); as p) {
        <header class="profile-head">
          <div>
            <h2>{{ '@' + p.username }}</h2>
            @if (p.bio) { <p class="bio">{{ p.bio }}</p> }
            <p class="joined">Se unió el {{ p.createdAt | date:'longDate' }}</p>
          </div>

          @if (!isOwnProfile()) {
            <button
              class="follow-btn"
              [class.following]="p.isFollowedByCurrentUser"
              (click)="toggleFollow(p)"
            >
              {{ p.isFollowedByCurrentUser ? 'Siguiendo' : 'Seguir' }}
            </button>
          }
        </header>

        <div class="stats">
          <span><strong>{{ p.postsCount }}</strong> posts</span>
          <span><strong>{{ p.followersCount }}</strong> seguidores</span>
          <span><strong>{{ p.followingCount }}</strong> siguiendo</span>
        </div>

        <section class="user-posts">
          @if (posts().length === 0) {
            <p>Este usuario aún no ha publicado nada.</p>
          } @else {
            @for (post of posts(); track post.id) {
              <app-post-card [post]="post" (deleted)="onDeleted($event)" />
            }
          }
        </section>
      }
    </div>
  `,
  styles: [`
    .profile-container { max-width: 600px; margin: 2rem auto; padding: 1rem; }
    .back { color: #1971c2; text-decoration: none; display: inline-block; margin-bottom: 1rem; }
    .profile-head { display: flex; justify-content: space-between; align-items: flex-start; }
    .bio { margin: 0.3rem 0; }
    .joined { color: #888; font-size: 0.85rem; }
    .stats { display: flex; gap: 1.5rem; margin: 1rem 0; padding: 0.75rem 0; border-top: 1px solid #eee; border-bottom: 1px solid #eee; }
    .follow-btn { padding: 0.5rem 1.25rem; cursor: pointer; border: 1px solid #1971c2; background: #1971c2; color: #fff; border-radius: 6px; }
    .follow-btn.following { background: #fff; color: #1971c2; }
    .user-posts { margin-top: 1.5rem; }
  `]
})
export class ProfileComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private userService = inject(UserService);
  auth = inject(AuthService);

  profile = signal<UserProfile | null>(null);
  posts = signal<Post[]>([]);
  loading = signal(true);

  ngOnInit() {
    // paramMap como observable: si navegas de un perfil a otro, recarga.
    this.route.paramMap.subscribe(params => {
      const id = Number(params.get('id'));
      this.load(id);
    });
  }

  isOwnProfile(): boolean {
    return this.profile()?.id === this.auth.currentUser()?.userId;
  }

  private load(id: number) {
    this.loading.set(true);
    this.userService.getProfile(id).subscribe({
      next: (p) => {
        this.profile.set(p);
        this.loading.set(false);
      },
      error: () => {
        this.profile.set(null);
        this.loading.set(false);
      }
    });
    this.userService.getUserPosts(id).subscribe({
      next: (data) => this.posts.set(data)
    });
  }

  toggleFollow(p: UserProfile) {
    const wasFollowing = p.isFollowedByCurrentUser;

    // Optimistic update
    this.profile.update(curr => curr ? {
      ...curr,
      isFollowedByCurrentUser: !wasFollowing,
      followersCount: curr.followersCount + (wasFollowing ? -1 : 1)
    } : curr);

    const request = wasFollowing
      ? this.userService.unfollow(p.id)
      : this.userService.follow(p.id);

    request.subscribe({
      next: (res) => this.profile.update(curr => curr ? {
        ...curr,
        isFollowedByCurrentUser: res.following,
        followersCount: res.followersCount
      } : curr),
      error: () => this.profile.update(curr => curr ? {
        ...curr,
        isFollowedByCurrentUser: wasFollowing,
        followersCount: curr.followersCount + (wasFollowing ? 1 : -1)
      } : curr)
    });
  }

  onDeleted(id: number) {
    this.posts.update(curr => curr.filter(p => p.id !== id));
  }
}
