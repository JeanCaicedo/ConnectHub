import { Component, inject, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommentService } from '../../core/services/comment.service';
import { AuthService } from '../../core/services/auth.service';
import { Comment } from '../../core/models/models';

@Component({
  selector: 'app-post-comments',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  template: `
    <div class="comments">
      @if (loading()) {
        <p class="muted">Cargando comentarios...</p>
      } @else {
        <form class="comment-form" [formGroup]="form" (ngSubmit)="submit()">
          <input
            formControlName="content"
            placeholder="Escribe un comentario..."
            [attr.aria-label]="replyingTo() ? 'Responder' : 'Comentar'"
          />
          <button type="submit" [disabled]="form.invalid">
            {{ replyingTo() ? 'Responder' : 'Comentar' }}
          </button>
          @if (replyingTo()) {
            <button type="button" class="link" (click)="cancelReply()">Cancelar</button>
          }
        </form>

        @for (c of comments(); track c.id) {
          <div class="comment">
            <div class="comment-head">
              <strong>{{ '@' + c.username }}</strong>
              <span class="muted">{{ c.createdAt | date:'short' }}</span>
            </div>
            <p>{{ c.content }}</p>
            <div class="comment-actions">
              <button class="link" (click)="startReply(c.id)">Responder</button>
              @if (c.userId === auth.currentUser()?.userId) {
                <button class="link danger" (click)="remove(c.id)">Eliminar</button>
              }
            </div>

            @for (r of c.replies; track r.id) {
              <div class="comment reply">
                <div class="comment-head">
                  <strong>{{ '@' + r.username }}</strong>
                  <span class="muted">{{ r.createdAt | date:'short' }}</span>
                </div>
                <p>{{ r.content }}</p>
                @if (r.userId === auth.currentUser()?.userId) {
                  <div class="comment-actions">
                    <button class="link danger" (click)="remove(r.id, c.id)">Eliminar</button>
                  </div>
                }
              </div>
            }
          </div>
        } @empty {
          <p class="muted">Sé el primero en comentar.</p>
        }
      }
    </div>
  `,
  styles: [`
    .comments { margin-top: 0.75rem; border-top: 1px solid var(--border, #eee); padding-top: 0.75rem; }
    .comment-form { display: flex; gap: 0.5rem; margin-bottom: 0.75rem; }
    .comment-form input { flex: 1; padding: 0.4rem; }
    .comment { margin-bottom: 0.6rem; }
    .comment p { margin: 0.2rem 0; }
    .comment.reply { margin-left: 1.5rem; padding-left: 0.5rem; border-left: 2px solid var(--border, #eee); }
    .comment-head { display: flex; gap: 0.5rem; align-items: baseline; font-size: 0.85rem; }
    .comment-actions { display: flex; gap: 0.75rem; }
    .muted { color: #888; font-size: 0.8rem; }
    button.link { background: none; border: none; color: #1971c2; cursor: pointer; padding: 0; font-size: 0.8rem; }
    button.link.danger { color: #c92a2a; }
  `]
})
export class PostCommentsComponent {
  // input() de señal: el padre pasa el postId. Angular 18 moderno.
  postId = input.required<number>();
  // Avisamos al padre cuando cambia el conteo para que actualice su badge.
  countChanged = output<number>();

  private commentService = inject(CommentService);
  private fb = inject(FormBuilder);
  auth = inject(AuthService);

  comments = signal<Comment[]>([]);
  loading = signal(true);
  replyingTo = signal<number | null>(null);

  form = this.fb.nonNullable.group({
    content: ['', [Validators.required, Validators.maxLength(1000)]]
  });

  ngOnInit() {
    this.commentService.getForPost(this.postId()).subscribe({
      next: (data) => {
        this.comments.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  startReply(commentId: number) {
    this.replyingTo.set(commentId);
  }

  cancelReply() {
    this.replyingTo.set(null);
  }

  submit() {
    if (this.form.invalid) return;

    const parentId = this.replyingTo() ?? undefined;
    this.commentService.add(this.postId(), {
      content: this.form.getRawValue().content,
      parentCommentId: parentId
    }).subscribe({
      next: (created) => {
        if (created.parentCommentId) {
          // Es una respuesta: la colgamos del comentario raiz correspondiente.
          this.comments.update(list => list.map(c =>
            c.id === created.parentCommentId
              ? { ...c, replies: [...c.replies, created] }
              : c
          ));
        } else {
          this.comments.update(list => [...list, created]);
        }
        this.form.reset();
        this.replyingTo.set(null);
        this.emitCount();
      }
    });
  }

  remove(commentId: number, parentId?: number) {
    this.commentService.delete(commentId).subscribe({
      next: () => {
        if (parentId) {
          // Borrar una respuesta: la quitamos de su raiz.
          this.comments.update(list => list.map(c =>
            c.id === parentId
              ? { ...c, replies: c.replies.filter(r => r.id !== commentId) }
              : c
          ));
        } else {
          // Borrar un raiz: se va con todas sus respuestas.
          this.comments.update(list => list.filter(c => c.id !== commentId));
        }
        this.emitCount();
      }
    });
  }

  // Conteo total = raices + todas sus respuestas.
  private emitCount() {
    const total = this.comments().reduce((sum, c) => sum + 1 + c.replies.length, 0);
    this.countChanged.emit(total);
  }
}
