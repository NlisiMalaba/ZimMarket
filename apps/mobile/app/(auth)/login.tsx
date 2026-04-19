import { Pressable, StyleSheet } from 'react-native';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';

export default function LoginScreen() {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Login</Text>
      <Text style={styles.description}>
        Login form will be finalized in the next task.
      </Text>
      <Pressable onPress={() => router.push('/(auth)/register')}>
        <Text style={styles.action}>New here? Create an account</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
    gap: 12,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
  },
  description: {
    textAlign: 'center',
  },
  action: {
    color: '#0f766e',
    fontWeight: '600',
  },
});
