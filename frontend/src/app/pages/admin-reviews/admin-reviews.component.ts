import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PendingReview } from '../../models/order.models';
import { OrderService } from '../../services/order.service';

@Component({
  selector: 'app-admin-reviews',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="card">
      <h1>Revisión manual de órdenes</h1>
      <div class="row" style="align-items:flex-end;">
        <div style="max-width:280px;">
          <label>Operador (reviewer)</label>
          <input [(ngModel)]="reviewer" placeholder="operador-1" />
        </div>
        <button class="secondary" (click)="load()" [disabled]="loading">
          {{ loading ? 'Cargando...' : 'Refrescar' }}
        </button>
      </div>
      <p class="error" *ngIf="error">{{ error }}</p>
    </div>

    <div class="card">
      <h2>Pendientes ({{ reviews.length }})</h2>
      <table *ngIf="reviews.length > 0; else empty">
        <thead>
          <tr>
            <th>Orden</th>
            <th>Usuario</th>
            <th>Monto</th>
            <th>Creada</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let r of reviews">
            <td>{{ r.orderId }}</td>
            <td>{{ r.userId }}</td>
            <td>{{ r.totalAmount | number: '1.2-2' }}</td>
            <td class="muted">{{ r.createdAt }}</td>
            <td>
              <button class="ok" (click)="resolve(r.orderId, true)" [disabled]="!reviewer || busyId === r.orderId">Aprobar</button>
              <button class="danger" (click)="resolve(r.orderId, false)" [disabled]="!reviewer || busyId === r.orderId">Rechazar</button>
            </td>
          </tr>
        </tbody>
      </table>
      <ng-template #empty>
        <p class="muted">No hay órdenes pendientes de revisión.</p>
      </ng-template>
    </div>
  `
})
export class AdminReviewsComponent implements OnInit {
  reviews: PendingReview[] = [];
  reviewer = 'operador-1';
  loading = false;
  busyId = '';
  error = '';

  constructor(private readonly orderService: OrderService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.orderService.getPendingReviews().subscribe({
      next: (data) => {
        this.reviews = data;
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo cargar la lista de pendientes.';
        this.loading = false;
      }
    });
  }

  resolve(orderId: string, approved: boolean): void {
    this.busyId = orderId;
    this.orderService.resolveReview(orderId, approved, this.reviewer).subscribe({
      next: () => {
        this.reviews = this.reviews.filter((r) => r.orderId !== orderId);
        this.busyId = '';
      },
      error: (err) => {
        this.error = err?.error?.error ?? 'No se pudo resolver la orden.';
        this.busyId = '';
      }
    });
  }
}
