import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  PendingReview,
  SubmitOrderRequest,
  SubmitOrderResponse
} from '../models/order.models';

/**
 * Cliente REST del BFF. Encapsula las llamadas HTTP para enviar órdenes y gestionar
 * la revisión manual.
 */
@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly baseUrl = `${environment.bffBaseUrl}/api/orders`;

  constructor(private readonly http: HttpClient) {}

  submitOrder(request: SubmitOrderRequest): Observable<SubmitOrderResponse> {
    return this.http.post<SubmitOrderResponse>(this.baseUrl, request);
  }

  getPendingReviews(): Observable<PendingReview[]> {
    return this.http.get<PendingReview[]>(`${this.baseUrl}/pending-reviews`);
  }

  resolveReview(orderId: string, approved: boolean, reviewer: string): Observable<SubmitOrderResponse> {
    return this.http.post<SubmitOrderResponse>(`${this.baseUrl}/${orderId}/review`, {
      approved,
      reviewer
    });
  }
}
