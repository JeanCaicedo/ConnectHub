import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { environment } from '../../../environments/environment';
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
          <div class="identity">
            @if (p.avatarUrl) {
              <img class="avatar" [src]="imageSrc(p.avatarUrl)" alt="avatar" />
            } @else {
              <div class="avatar placeholder">{{ p.username.charAt(0).toUpperCase() }}</div>
            }
            <div>
              <h2>{{ '@' + p.username }}</h2>
              @if (p.bio) { <p class="bio">{{ p.bio }}</p> }
              <p class="joined">Se unió el {{ p.createdAt | date:'longDate' }}</p>
            </div>
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

        @if (isOwnProfile()) {
          <div class="edit-box">
            <label class="avatar-upload">
              Cambiar avatar
              <input type="file" accept="image/*" (change)="onAvatarSelected($event)" hidden />
            </label>
            <div class="bio-edit">
              <input [value]="p.bio ?? ''" #bioInput placeholder="Escribe tu bio..." maxlength="500" />
              <button (click)="saveBio(bioInput.value)">Guardar bio</button>
            </div>
          </div>
        }

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
    .identity { display: flex; gap: 1rem; align-items: center; }
    .avatar { width: 64px; height: 64px; border-radius: 50%; object-fit: cover; }
    .avatar.placeholder { display: flex; align-items: center; justify-content: center; background: #1971c2; color: #fff; font-size: 1.5rem; font-weight: 600; }
    .bio { margin: 0.3rem 0; }
    .joined { color: #888; font-size: 0.85rem; }
    .edit-box { display: flex; gap: 1rem; align-items: center; margin: 1rem 0; flex-wrap: wrap; }
    .avatar-upload { color: #1971c2; cursor: pointer; text-decoration: underline; }
    .bio-edit { display: flex; gap: 0.5rem; flex: 1; }
    .bio-edit input { flex: 1; padding: 0.4rem; }
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

  private apiHost = environment.apiHost;
  imageSrc(url: string): string {
    return url.startsWith('http') ? url : `${this.apiHost}${url}`;
  }

  onAvatarSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.userService.uploadAvatar(file).subscribe({
      next: (res) => this.profile.update(curr =>
        curr ? { ...curr, avatarUrl: res.avatarUrl } : curr)
    });
  }

  saveBio(bio: string) {
    this.userService.updateBio(bio).subscribe({
      next: () => this.profile.update(curr => curr ? { ...curr, bio } : curr)
    });
  }
}
