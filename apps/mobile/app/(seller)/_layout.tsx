import { Stack } from 'expo-router';

export default function SellerLayout() {
  return (
    <Stack>
      <Stack.Screen
        name="index"
        options={{
          title: 'Seller',
          headerTitle: 'Seller Home',
        }}
      />
      <Stack.Screen name="orders" options={{ title: 'Seller orders' }} />
      <Stack.Screen name="orders/[orderId]" options={{ title: 'Order detail' }} />
      <Stack.Screen name="listings" options={{ title: 'My listings' }} />
      <Stack.Screen name="create-listing" options={{ title: 'Create listing' }} />
      <Stack.Screen name="edit-listing/[id]" options={{ title: 'Edit listing' }} />
      <Stack.Screen name="kyc-upload" options={{ title: 'Seller KYC upload' }} />
      <Stack.Screen name="application-submitted" options={{ title: 'Application submitted' }} />
    </Stack>
  );
}
