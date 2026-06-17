import { Component, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { Subscription } from 'rxjs';
import { OrderStatusUpdate } from '../../models/order.models';
import { OrderService } from '../../services/order.service';
import { OrderStatusHubService } from '../../services/order-status-hub.service';

@Component({
  selector: 'app-submit-order',
  standalone: true,
  imports: [ReactiveFormsModule, DecimalPipe],
  template: `
    <div class="card">
      <h1>Enviar nueva orden</h1>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>Usuario (UserId)</label>
        <input formControlName="userId" placeholder="user-1" />

        <label>Token de pago (idempotencia)</label>
        <input formControlName="paymentToken" placeholder="tok-123" />

        <h2 style="margin-top:20px;">Items</h2>
        <div formArrayName="items">
          @for (item of items.controls; track item; let i = $index) {
            <div [formGroupName]="i" class="row" style="align-items:flex-end;">
              <div>
                <label>Producto (GUID)</label>
                <input formControlName="productId" readonly />
              </div>
              <div>
                <label>Cantidad</label>
                <input type="number" formControlName="quantity" min="1" />
              </div>
              <div>
                <label>Precio unitario</label>
                <input type="number" formControlName="unitPrice" min="0.01" step="0.01" />
              </div>
              <button type="button" class="secondary" (click)="removeItem(i)" [disabled]="items.length === 1">Quitar</button>
            </div>
          }
        </div>
        <button type="button" class="secondary" style="margin-top:12px;" (click)="addItem()">+ Agregar item</button>

        <p class="muted" style="margin-top:16px;">Total calculado: {{ total | number: '1.2-2' }}</p>

        <div style="margin-top:16px;">
          <button type="submit" [disabled]="form.invalid || submitting">
            {{ submitting ? 'Enviando...' : 'Enviar orden' }}
          </button>
        </div>
        @if (error) {
          <p class="error">{{ error }}</p>
        }
      </form>
    </div>

    @if (trackedOrderId) {
      <div class="card">
        <h2>Seguimiento en vivo — {{ trackedOrderId }}</h2>
        <ul class="timeline">
          @for (u of updates; track u.timestamp) {
            <li>
              <span class="badge" [class.ok]="isOk(u.status)" [class.warn]="isWarn(u.status)" [class.err]="isErr(u.status)">
                {{ u.status }}
              </span>
              <span class="muted">{{ u.timestamp }}</span>
            </li>
          }
        </ul>
        @if (updates.length === 0) {
          <p class="muted">Esperando actualizaciones...</p>
        }
      </div>
    }
  `
})
export class SubmitOrderComponent implements OnInit, OnDestroy {
  form: FormGroup;
  submitting = false;
  error = '';
  trackedOrderId = '';
  updates: OrderStatusUpdate[] = [];

  private sub?: Subscription;

  constructor(
    private readonly fb: FormBuilder,
    private readonly orderService: OrderService,
    private readonly hub: OrderStatusHubService,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      userId: ['user-1', Validators.required],
      paymentToken: ['', Validators.required],
      items: this.fb.array([this.createItem()])
    });
  }

  ngOnInit(): void {
    this.sub = this.hub.status$.subscribe((update) => {
      if (update.orderId === this.trackedOrderId) {
        this.updates = [...this.updates, update];
        this.cdr.detectChanges();
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  get items(): FormArray {
    return this.form.get('items') as FormArray;
  }

  get total(): number {
    return this.items.controls.reduce((acc, group) => {
      const quantity = Number(group.get('quantity')?.value) || 0;
      const unitPrice = Number(group.get('unitPrice')?.value) || 0;
      return acc + quantity * unitPrice;
    }, 0);
  }

  createItem(): FormGroup {
    return this.fb.group({
      productId: [crypto.randomUUID(), Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [100, [Validators.required, Validators.min(0.01)]]
    });
  }

  addItem(): void {
    this.items.push(this.createItem());
  }

  removeItem(index: number): void {
    if (this.items.length > 1) {
      this.items.removeAt(index);
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      return;
    }

    this.submitting = true;
    this.error = '';

    const request = {
      userId: this.form.value.userId,
      paymentToken: this.form.value.paymentToken,
      totalAmount: this.total,
      items: this.items.value
    };

    this.orderService.submitOrder(request).subscribe({
      next: async (response) => {
        this.trackedOrderId = response.orderId;
        this.updates = [];
        await this.hub.subscribe(response.orderId);
        this.submitting = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.error = err?.error?.error ?? 'No se pudo enviar la orden.';
        this.submitting = false;
        this.cdr.detectChanges();
      }
    });
  }

  isOk(status: string): boolean {
    return ['Approved', 'PaymentProcessed', 'ShipmentRequested'].includes(status);
  }

  isWarn(status: string): boolean {
    return status === 'ManualReviewRequired' || status === 'FraudCheckPending';
  }

  isErr(status: string): boolean {
    return status === 'Rejected';
  }
}
