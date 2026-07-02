import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions } from 'chart.js';
import { StatsService } from '../../core/services/stats.service';
import { DailyCount, Engagement, TopPost } from '../../core/models/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, BaseChartDirective],
  template: `
    <div class="dash">
      <header>
        <h2>Tu panel</h2>
        <a routerLink="/feed">← Volver al feed</a>
      </header>

      @if (engagement(); as e) {
        <div class="cards">
          <div class="card"><span class="num">{{ e.postsCount }}</span><span>Posts</span></div>
          <div class="card"><span class="num">{{ e.followersCount }}</span><span>Seguidores</span></div>
          <div class="card"><span class="num">{{ e.likesReceived }}</span><span>Likes recibidos</span></div>
          <div class="card"><span class="num">{{ e.commentsReceived }}</span><span>Comentarios recibidos</span></div>
          <div class="card highlight"><span class="num">{{ e.engagementRate }}</span><span>Interacción media / post</span></div>
        </div>
      }

      <div class="charts">
        <section class="chart-box">
          <h3>Posts por día (30 días)</h3>
          <canvas baseChart type="line" [data]="postsChart()" [options]="lineOptions"></canvas>
        </section>

        <section class="chart-box">
          <h3>Likes recibidos por día</h3>
          <canvas baseChart type="line" [data]="likesChart()" [options]="lineOptions"></canvas>
        </section>

        <section class="chart-box">
          <h3>Crecimiento de seguidores</h3>
          <canvas baseChart type="line" [data]="followersChart()" [options]="lineOptions"></canvas>
        </section>

        <section class="chart-box">
          <h3>Tus posts con más interacción</h3>
          @if (topPosts().length === 0) {
            <p class="muted">Aún no hay datos suficientes.</p>
          } @else {
            <ul class="top-list">
              @for (p of topPosts(); track p.id) {
                <li>
                  <span class="content">{{ p.content }}</span>
                  <span class="metrics">♥ {{ p.likesCount }} · 💬 {{ p.commentsCount }}</span>
                </li>
              }
            </ul>
          }
        </section>
      </div>
    </div>
  `,
  styles: [`
    .dash { max-width: 900px; margin: 2rem auto; padding: 1rem; }
    header { display: flex; justify-content: space-between; align-items: center; }
    header a { color: #1971c2; text-decoration: none; }
    .cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 1rem; margin: 1.5rem 0; }
    .card { display: flex; flex-direction: column; gap: 0.25rem; padding: 1rem; border: 1px solid var(--border, #e0e0e0); border-radius: 10px; background: var(--card, #fff); }
    .card .num { font-size: 1.75rem; font-weight: 700; color: #1971c2; }
    .card.highlight { background: #1971c2; color: #fff; }
    .card.highlight .num { color: #fff; }
    .charts { display: grid; grid-template-columns: repeat(auto-fit, minmax(360px, 1fr)); gap: 1.5rem; }
    .chart-box { border: 1px solid var(--border, #e0e0e0); border-radius: 10px; padding: 1rem; background: var(--card, #fff); }
    .chart-box h3 { margin-top: 0; font-size: 1rem; }
    .top-list { list-style: none; padding: 0; margin: 0; }
    .top-list li { display: flex; justify-content: space-between; gap: 1rem; padding: 0.5rem 0; border-bottom: 1px solid var(--border, #f0f0f0); font-size: 0.9rem; }
    .top-list .metrics { white-space: nowrap; color: #666; }
    .muted { color: #888; }
  `]
})
export class DashboardComponent implements OnInit {
  private stats = inject(StatsService);

  postsPerDay = signal<DailyCount[]>([]);
  likes = signal<DailyCount[]>([]);
  followers = signal<DailyCount[]>([]);
  topPosts = signal<TopPost[]>([]);
  engagement = signal<Engagement | null>(null);

  lineOptions: ChartOptions<'line'> = {
    responsive: true,
    plugins: { legend: { display: false } },
    scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
  };

  // computed(): el gráfico se recalcula solo cuando cambia su señal fuente.
  postsChart = computed<ChartData<'line'>>(() => this.toLine(this.postsPerDay(), '#1971c2'));
  likesChart = computed<ChartData<'line'>>(() => this.toLine(this.likes(), '#e8590c'));
  followersChart = computed<ChartData<'line'>>(() => this.toLine(this.followers(), '#2f9e44'));

  ngOnInit() {
    this.stats.postsPerDay().subscribe(d => this.postsPerDay.set(d));
    this.stats.likesReceived().subscribe(d => this.likes.set(d));
    this.stats.followersGrowth().subscribe(d => this.followers.set(d));
    this.stats.topPosts().subscribe(d => this.topPosts.set(d));
    this.stats.engagement().subscribe(d => this.engagement.set(d));
  }

  private toLine(data: DailyCount[], color: string): ChartData<'line'> {
    return {
      labels: data.map(d => this.shortDate(d.date)),
      datasets: [{
        data: data.map(d => d.count),
        borderColor: color,
        backgroundColor: color + '33',
        fill: true,
        tension: 0.3,
        pointRadius: 0
      }]
    };
  }

  private shortDate(iso: string): string {
    const d = new Date(iso);
    return `${d.getDate()}/${d.getMonth() + 1}`;
  }
}
