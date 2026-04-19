import { Tabs } from 'expo-router';

export default function CustomerTabsLayout() {
  return (
    <Tabs>
      <Tabs.Screen
        name="index"
        options={{
          title: 'Browse',
          headerTitle: 'Browse Products',
        }}
      />
      <Tabs.Screen
        name="cart"
        options={{
          title: 'Cart',
          headerTitle: 'Your Cart',
        }}
      />
      <Tabs.Screen
        name="orders"
        options={{
          title: 'Orders',
          headerTitle: 'Your Orders',
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{
          title: 'Profile',
          headerTitle: 'My Profile',
        }}
      />
      <Tabs.Screen
        name="product/[id]"
        options={{
          href: null,
          title: 'Product',
          headerTitle: 'Product detail',
        }}
      />
      <Tabs.Screen
        name="checkout"
        options={{
          href: null,
          title: 'Checkout',
          headerTitle: 'Checkout',
        }}
      />
      <Tabs.Screen
        name="payment"
        options={{
          href: null,
          title: 'Payment',
          headerTitle: 'Payment',
        }}
      />
      <Tabs.Screen
        name="order-confirmed"
        options={{
          href: null,
          title: 'Order Confirmed',
          headerTitle: 'Order Confirmed',
        }}
      />
      <Tabs.Screen
        name="orders/[orderId]/tracking"
        options={{
          href: null,
          title: 'Track Order',
          headerTitle: 'Track Order',
        }}
      />
    </Tabs>
  );
}
