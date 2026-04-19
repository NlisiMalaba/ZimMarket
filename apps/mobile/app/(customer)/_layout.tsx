import { Tabs } from 'expo-router';

export default function CustomerTabsLayout() {
  return (
    <Tabs>
      <Tabs.Screen
        name="index"
        options={{
          title: 'Customer',
          headerTitle: 'Customer Home',
        }}
      />
    </Tabs>
  );
}
