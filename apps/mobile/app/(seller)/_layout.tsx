import { Tabs } from 'expo-router';

export default function SellerTabsLayout() {
  return (
    <Tabs>
      <Tabs.Screen
        name="index"
        options={{
          title: 'Seller',
          headerTitle: 'Seller Home',
        }}
      />
    </Tabs>
  );
}
