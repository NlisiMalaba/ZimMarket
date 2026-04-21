import { Stack } from 'expo-router';

export default function DriverTabsLayout() {
  return (
    <Stack>
      <Stack.Screen
        name="index"
        options={{
          title: 'Driver',
          headerTitle: 'Driver Home',
        }}
      />
      <Stack.Screen name="batches/[batchId]" options={{ title: 'Batch detail' }} />
      <Stack.Screen name="active-delivery/[batchId]" options={{ title: 'Active delivery' }} />
      <Stack.Screen name="kyc-upload" options={{ title: 'Driver document upload' }} />
      <Stack.Screen name="under-review" options={{ title: 'Under review' }} />
    </Stack>
  );
}
