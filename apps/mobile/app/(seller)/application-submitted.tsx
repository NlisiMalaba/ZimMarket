import { useEffect, useMemo, useState } from 'react';
import { Pressable, StyleSheet } from 'react-native';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { formatKycStatusLabel, normalizeKycStatus } from '@/lib/seller-kyc';
import { sellerOnboardingService } from '@/lib/services/seller-onboarding-service';
import { useAuthStore } from '@/store/auth-store';
import type { KycStatus } from '@/types/auth';

const POLL_INTERVAL_MS = 10000;

export default function SellerApplicationSubmittedScreen() {
  const { accessToken, refreshToken, setSession, clearAuth } = useAuthStore((state) => ({
    accessToken: state.accessToken,
    refreshToken: state.refreshToken,
    setSession: state.setSession,
    clearAuth: state.clearAuth,
  }));
  const [status, setStatus] = useState<KycStatus>(useAuthStore.getState().kycStatus ?? 'pendingReview');
  const [rejectionReason, setRejectionReason] = useState<string | null>(null);
  const [pollError, setPollError] = useState<string | null>(null);

  useEffect(() => {
    if (!accessToken || !refreshToken) {
      return;
    }

    let isMounted = true;
    const pollStatus = async () => {
      try {
        const [refreshResult, verification] = await Promise.all([
          sellerOnboardingService.pollKycStatus(accessToken, refreshToken),
          sellerOnboardingService.getVerificationStatus(),
        ]);

        if (!isMounted) {
          return;
        }

        setStatus(refreshResult.kycStatus);
        setRejectionReason(verification.rejectionReason);
        setPollError(null);
        setSession({
          accessToken: refreshResult.accessToken,
          refreshToken: refreshResult.refreshToken,
          kycStatus: refreshResult.kycStatus,
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

  const statusText = useMemo(() => formatKycStatusLabel(status), [status]);
  const normalizedStatus = normalizeKycStatus(status);

  return (
    <View style={styles.container}>
      <Text style={styles.heading}>Application submitted</Text>
      <Text style={styles.description}>
        Your KYC documents are under review. We refresh your status every 10 seconds and will notify you when a
        decision is made.
      </Text>

      <View style={styles.statusCard}>
        <Text style={styles.statusLabel}>Current status</Text>
        <Text style={styles.statusValue}>{statusText}</Text>
      </View>

      {normalizedStatus === 'rejected' && rejectionReason ? (
        <View style={styles.rejectionCard}>
          <Text style={styles.rejectionTitle}>Rejection reason</Text>
          <Text style={styles.rejectionBody}>{rejectionReason}</Text>
        </View>
      ) : null}

      {pollError ? <Text style={styles.error}>{pollError}</Text> : null}

      {normalizedStatus === 'approved' ? (
        <Pressable style={styles.primaryButton} onPress={() => router.replace('/(seller)' as never)}>
          <Text style={styles.primaryButtonText}>Continue to seller dashboard</Text>
        </Pressable>
      ) : null}

      {normalizedStatus === 'rejected' ? (
        <Pressable style={styles.primaryButton} onPress={() => router.replace('/(seller)/kyc-upload' as never)}>
          <Text style={styles.primaryButtonText}>Resubmit documents</Text>
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
  rejectionCard: {
    borderWidth: 1,
    borderColor: '#fecaca',
    backgroundColor: '#fef2f2',
    borderRadius: 12,
    padding: 14,
    gap: 6,
  },
  rejectionTitle: {
    fontWeight: '700',
    color: '#991b1b',
  },
  rejectionBody: {
    color: '#7f1d1d',
    lineHeight: 20,
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
