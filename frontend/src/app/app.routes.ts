import { Routes } from '@angular/router';
import { SubmitOrderComponent } from './pages/submit-order/submit-order.component';
import { AdminReviewsComponent } from './pages/admin-reviews/admin-reviews.component';

export const routes: Routes = [
  { path: '', component: SubmitOrderComponent },
  { path: 'admin', component: AdminReviewsComponent },
  { path: '**', redirectTo: '' }
];
