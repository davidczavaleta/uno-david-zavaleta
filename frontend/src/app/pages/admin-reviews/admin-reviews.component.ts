import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { PendingReview } from '../../models/order.models';
import { OrderService } from '../../services/order.service';

@Component({
  selector: 'app-admin-reviews',
  standalone: true,
  imports: [DecimalPipe],
  template: `
    <div class="card">
      <h1>Revisión manual de órdenes</h1>
      <div class="row" style="align-items:flex-end;">
        <button class="secondary" (click)="load()" [disabled]="loading">
          {{ loading ? 'Cargando...' : 'Refrescar' }}
        </button>
      </div>
      @if (error) {
        <p class="error">{{ error }}</p>
      }
    </div>

    <div class="card">
      <h2>Pendientes ({{ reviews.length }})</h2>
      @if (reviews.length > 0) {
        <table>
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
            @for (r of reviews; track r.orderId) {
              <tr>
                <td>{{ r.orderId }}</td>
                <td>{{ r.userId }}</td>
                <td>{{ r.totalAmount | number: '1.2-2' }}</td>
                <td class="muted">{{ r.createdAt }}</td>
                <td>
                  <button class="ok" (click)="resolve(r.orderId, true)" [disabled]="busyId === r.orderId">Aprobar</button>
                  <button class="danger" (click)="resolve(r.orderId, false)" [disabled]="busyId === r.orderId">Rechazar</button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      } @else {
        <p class="muted">No hay órdenes pendientes de revisión.</p>
      }
    </div>
  `
})
export class AdminReviewsComponent implements OnInit {
  reviews: PendingReview[] = [];
  loading = false;
  busyId = '';
  error = '';

  constructor(private readonly orderService: OrderService, private readonly cdr: ChangeDetectorRef) {}

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
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'No se pudo cargar la lista de pendientes.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  resolve(orderId: string, approved: boolean): void {
    this.busyId = orderId;
    this.orderService.resolveReview(orderId, approved, 'Admin').subscribe({
      next: () => {
        this.reviews = this.reviews.filter((r) => r.orderId !== orderId);
        this.busyId = '';
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.error = err?.error?.error ?? 'No se pudo resolver la orden.';
        this.busyId = '';
        this.cdr.detectChanges();
      }
    });
  }
}
