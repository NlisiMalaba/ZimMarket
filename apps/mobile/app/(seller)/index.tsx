import { StyleSheet } from 'react-native';

import { Text, View } from '@/components/Themed';

export default function SellerHomeScreen() {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Seller Home</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
  },
});
