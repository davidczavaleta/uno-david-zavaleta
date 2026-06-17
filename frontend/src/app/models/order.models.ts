export interface OrderItem {
  productId: string;
  quantity: number;
  unitPrice: number;
}

export interface SubmitOrderRequest {
  orderId?: string;
  userId: string;
  items: OrderItem[];
  totalAmount: number;
  paymentToken: string;
}

export interface SubmitOrderResponse {
  orderId: string;
  status: string;
}

export interface OrderStatusUpdate {
  orderId: string;
  status: string;
  timestamp: string;
}

export interface PendingReview {
  orderId: string;
  userId: string;
  totalAmount: number;
  createdAt: string;
}
