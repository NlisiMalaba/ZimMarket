export type OrderStatusTab = 'Active' | 'Completed' | 'Cancelled';

export type OrderItem = {
  id: string;
  status: OrderStatusTab;
  createdAt: string;
};
