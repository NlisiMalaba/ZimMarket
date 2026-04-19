import { Tabs } from 'expo-router';

export default function DriverTabsLayout() {
  return (
    <Tabs>
      <Tabs.Screen
        name="index"
        options={{
          title: 'Driver',
          headerTitle: 'Driver Home',
        }}
      />
    </Tabs>
  );
}
