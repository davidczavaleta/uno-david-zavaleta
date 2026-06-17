import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { OrderStatusUpdate } from '../models/order.models';

/**
 * Cliente SignalR para el seguimiento en tiempo real del estado de las órdenes.
 * Se conecta al hub del BFF y reemite las actualizaciones recibidas.
 */
@Injectable({ providedIn: 'root' })
export class OrderStatusHubService {
  private connection?: signalR.HubConnection;
  private readonly statusSubject = new Subject<OrderStatusUpdate>();

  /** Flujo de actualizaciones de estado recibidas del servidor. */
  readonly status$: Observable<OrderStatusUpdate> = this.statusSubject.asObservable();

  /** Establece la conexión al hub si aún no existe. */
  async connect(): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.bffBaseUrl}/hubs/orders`)
      .withAutomaticReconnect()
      .build();

    this.connection.on('OrderStatusChanged', (update: OrderStatusUpdate) => {
      this.statusSubject.next(update);
    });

    await this.connection.start();
  }

  /** Se suscribe a las actualizaciones de una orden específica. */
  async subscribe(orderId: string): Promise<void> {
    await this.connect();
    await this.connection!.invoke('Subscribe', orderId);
  }

  /** Cancela la suscripción a una orden. */
  async unsubscribe(orderId: string): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('Unsubscribe', orderId);
    }
  }
}
