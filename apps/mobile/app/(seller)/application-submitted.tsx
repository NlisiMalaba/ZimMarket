import { useEffect, useMemo, useState } from 'react';
import { Pressable, StyleSheet } from 'react-native';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { sellerOnboardingService } from '@/lib/services/seller-onboarding-service';
import { useAuthStore } from '@/store/auth-store';
import type { KycStatus } from '@/types/auth';

const POLL_INTERVAL_MS = 10000;

const formatKycStatus = (status: KycStatus): string => {
  const normalized = status.trim().toLowerCase();
  if (normalized === 'approved') {
    return 'Approved';
  }

  if (normalized === 'rejected') {
    return 'Rejected';
  }

  if (normalized === 'notsubmitted') {
    return 'Not submitted';
  }

  return 'Pending review';
};

export default function SellerApplicationSubmittedScreen() {
  const { accessToken, refreshToken, setSession, clearAuth } = useAuthStore((state) => ({
    accessToken: state.accessToken,
    refreshToken: state.refreshToken,
    setSession: state.setSession,
    clearAuth: state.clearAuth,
  }));
  const [status, setStatus] = useState<KycStatus>('pending');
  const [pollError, setPollError] = useState<string | null>(null);

  useEffect(() => {
    if (!accessToken || !refreshToken) {
      return;
    }

    let isMounted = true;
    const pollStatus = async () => {
      try {
        const response = await sellerOnboardingService.pollKycStatus(accessToken, refreshToken);
        if (!isMounted) {
          return;
        }

        setStatus(response.kycStatus);
        setPollError(null);
        setSession({
          accessToken: response.accessToken,
          refreshToken: response.refreshToken,
        });
      } catch (error) {
        if (!isMounted) {
          return;
        }

        setPollError(error instanceof Error ? error.message : 'Could not refresh application status.');
      }
    };

    void pollStatus();
    const timer = setInterval(() => {
      void pollStatus();
    }, POLL_INTERVAL_MS);

    return () => {
      isMounted = false;
      clearInterval(timer);
    };
  }, [accessToken, refreshToken, setSession]);

  const statusText = useMemo(() => formatKycStatus(status), [status]);

  return (
    <View style={styles.container}>
      <Text style={styles.heading}>Application submitted</Text>
      <Text style={styles.description}>
        Your KYC documents are under review. We are checking your status every 10 seconds.
      </Text>

      <View style={styles.statusCard}>
        <Text style={styles.statusLabel}>Current status</Text>
        <Text style={styles.statusValue}>{statusText}</Text>
      </View>

      {pollError ? <Text style={styles.error}>{pollError}</Text> : null}

      {status.trim().toLowerCase() === 'approved' ? (
        <Pressable style={styles.primaryButton} onPress={() => router.replace('/(seller)')}>
          <Text style={styles.primaryButtonText}>Continue to seller dashboard</Text>
        </Pressable>
      ) : null}

      <Pressable
        style={styles.secondaryButton}
        onPress={() => {
          clearAuth();
          router.replace('/(auth)/login');
        }}
      >
        <Text style={styles.secondaryButtonText}>Sign out</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 24,
    gap: 12,
    justifyContent: 'center',
  },
  heading: {
    fontSize: 24,
    fontWeight: '700',
  },
  description: {
    color: '#334155',
    lineHeight: 20,
  },
  statusCard: {
    borderWidth: 1,
    borderColor: '#d4d4d8',
    borderRadius: 12,
    padding: 16,
    gap: 8,
    marginTop: 8,
  },
  statusLabel: {
    color: '#64748b',
    fontSize: 13,
    textTransform: 'uppercase',
    letterSpacing: 0.8,
  },
  statusValue: {
    fontSize: 20,
    fontWeight: '700',
  },
  error: {
    color: '#dc2626',
    fontWeight: '500',
  },
  primaryButton: {
    backgroundColor: '#0f766e',
    borderRadius: 12,
    paddingVertical: 14,
    alignItems: 'center',
    marginTop: 8,
  },
  primaryButtonText: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '700',
  },
  secondaryButton: {
    borderWidth: 1,
    borderColor: '#0f766e',
    borderRadius: 12,
    paddingVertical: 12,
    alignItems: 'center',
    marginTop: 4,
  },
  secondaryButtonText: {
    color: '#0f766e',
    fontSize: 15,
    fontWeight: '700',
  },
});
