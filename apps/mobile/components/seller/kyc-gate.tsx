import { Pressable, StyleSheet } from 'react-native';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { formatKycStatusLabel } from '@/lib/seller-kyc';
import { useAuthStore } from '@/store/auth-store';

type KycGateProps = {
  actionLabel?: string;
};

export function KycGate({ actionLabel = 'View verification' }: KycGateProps) {
  const kycStatus = useAuthStore((state) => state.kycStatus);

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Verification required</Text>
      <Text style={styles.description}>
        Your seller account must be KYC-approved before you can create or edit product listings. Upload your national
        ID and proof of residence, then wait for admin approval.
      </Text>
      <Text style={styles.status}>
        Current status: <Text style={styles.statusValue}>{formatKycStatusLabel(kycStatus)}</Text>
      </Text>
      <Pressable
        style={styles.button}
        onPress={() => {
          router.push('/(seller)/kyc-upload' as never);
        }}
      >
        <Text style={styles.buttonText}>{actionLabel}</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    borderWidth: 1,
    borderColor: '#fcd34d',
    backgroundColor: '#fffbeb',
    borderRadius: 12,
    padding: 16,
    gap: 10,
  },
  title: {
    fontSize: 18,
    fontWeight: '700',
    color: '#92400e',
  },
  description: {
    color: '#78350f',
    lineHeight: 20,
  },
  status: {
    color: '#78350f',
    fontSize: 14,
  },
  statusValue: {
    fontWeight: '700',
  },
  button: {
    marginTop: 4,
    backgroundColor: '#0f766e',
    borderRadius: 10,
    paddingVertical: 12,
    alignItems: 'center',
  },
  buttonText: {
    color: '#ffffff',
    fontWeight: '700',
  },
});
