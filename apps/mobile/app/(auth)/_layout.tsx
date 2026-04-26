import { Stack } from 'expo-router';

export default function AuthLayout() {
  return (
    <Stack>
      <Stack.Screen name="splash" options={{ title: 'Welcome', headerShown: false }} />
      <Stack.Screen name="register" options={{ title: 'Create account' }} />
      <Stack.Screen name="register-seller" options={{ title: 'Create seller account' }} />
      <Stack.Screen name="register-driver" options={{ title: 'Create driver account' }} />
      <Stack.Screen name="verify-otp" options={{ title: 'Verify phone' }} />
      <Stack.Screen name="login" options={{ title: 'Login' }} />
    </Stack>
  );
}
