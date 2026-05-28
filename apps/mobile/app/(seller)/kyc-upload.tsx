import { useEffect, useState } from 'react';
import { ActivityIndicator, Image, Pressable, StyleSheet } from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { formatKycStatusLabel, normalizeKycStatus } from '@/lib/seller-kyc';
import {
  SELLER_KYC_FILE_TYPE,
  sellerOnboardingService,
} from '@/lib/services/seller-onboarding-service';
import { useAuthStore } from '@/store/auth-store';

type PickedDocument = {
  uri: string;
  mimeType: 'image/jpeg' | 'image/png' | 'image/webp';
  fileSizeBytes: number;
};

const resolveMimeType = (value: string | null | undefined): PickedDocument['mimeType'] | null => {
  if (value === 'image/jpeg' || value === 'image/png' || value === 'image/webp') {
    return value;
  }

  return null;
};

const pickImageDocument = async (): Promise<PickedDocument | null> => {
  const permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
  if (!permission.granted) {
    throw new Error('Photo library permission is required to upload KYC documents.');
  }

  const result = await ImagePicker.launchImageLibraryAsync({
    allowsEditing: false,
    mediaTypes: ImagePicker.MediaTypeOptions.Images,
    quality: 0.9,
  });

  if (result.canceled) {
    return null;
  }

  const asset = result.assets[0];
  const mimeType = resolveMimeType(asset.mimeType);
  if (!mimeType) {
    throw new Error('Only JPG, PNG, and WEBP images are supported.');
  }

  if (!asset.fileSize || asset.fileSize <= 0) {
    throw new Error('Unable to determine image size. Please choose another file.');
  }

  return {
    uri: asset.uri,
    mimeType,
    fileSizeBytes: asset.fileSize,
  };
};

export default function SellerKycUploadScreen() {
  const kycStatus = useAuthStore((state) => state.kycStatus);
  const setSession = useAuthStore((state) => state.setSession);

  const [nationalId, setNationalId] = useState<PickedDocument | null>(null);
  const [proofOfResidence, setProofOfResidence] = useState<PickedDocument | null>(null);
  const [rejectionReason, setRejectionReason] = useState<string | null>(null);
  const [isLoadingStatus, setIsLoadingStatus] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const normalizedStatus = normalizeKycStatus(kycStatus);
  const isPendingReview = normalizedStatus === 'pendingReview';

  useEffect(() => {
    let isMounted = true;

    const loadStatus = async () => {
      try {
        const verification = await sellerOnboardingService.getVerificationStatus();
        if (!isMounted) {
          return;
        }

        setRejectionReason(verification.rejectionReason);
        setSession({ kycStatus: verification.kycStatus });

        if (normalizeKycStatus(verification.kycStatus) === 'pendingReview') {
          router.replace('/(seller)/application-submitted' as never);
        }
      } catch (loadError) {
        if (!isMounted) {
          return;
        }

        setError(loadError instanceof Error ? loadError.message : 'Failed to load verification status.');
      } finally {
        if (isMounted) {
          setIsLoadingStatus(false);
        }
      }
    };

    void loadStatus();

    return () => {
      isMounted = false;
    };
  }, [setSession]);

  const handlePickNationalId = async () => {
    setError(null);
    try {
      const picked = await pickImageDocument();
      if (picked) {
        setNationalId(picked);
      }
    } catch (pickError) {
      setError(pickError instanceof Error ? pickError.message : 'Failed to pick national ID photo.');
    }
  };

  const handlePickProof = async () => {
    setError(null);
    try {
      const picked = await pickImageDocument();
      if (picked) {
        setProofOfResidence(picked);
      }
    } catch (pickError) {
      setError(pickError instanceof Error ? pickError.message : 'Failed to pick proof of residence.');
    }
  };

  const handleSubmit = async () => {
    if (!nationalId || !proofOfResidence) {
      setError('Please upload both documents before submitting.');
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      const nationalIdPresigned = await sellerOnboardingService.getPresignedUploadUrl(
        SELLER_KYC_FILE_TYPE.nationalId,
        nationalId.mimeType,
        nationalId.fileSizeBytes
      );
      await sellerOnboardingService.uploadDocument(
        nationalIdPresigned.uploadUrl,
        nationalId.uri,
        nationalId.mimeType
      );

      const proofPresigned = await sellerOnboardingService.getPresignedUploadUrl(
        SELLER_KYC_FILE_TYPE.proofOfResidence,
        proofOfResidence.mimeType,
        proofOfResidence.fileSizeBytes
      );
      await sellerOnboardingService.uploadDocument(
        proofPresigned.uploadUrl,
        proofOfResidence.uri,
        proofOfResidence.mimeType
      );

      await sellerOnboardingService.submitKyc({
        nationalIdKey: nationalIdPresigned.fileKey,
        proofOfResidenceKey: proofPresigned.fileKey,
      });

      setSession({ kycStatus: 'pendingReview' });
      router.replace('/(seller)/application-submitted' as never);
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Failed to submit KYC application.');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoadingStatus) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#0f766e" />
      </View>
    );
  }

  if (isPendingReview) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#0f766e" />
        <Text style={styles.description}>Redirecting to application status…</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Text style={styles.heading}>Seller verification</Text>
      <Text style={styles.description}>
        Submit your national ID and proof of residence. An admin must approve your account before you can list
        products on ZimMarket.
      </Text>

      <View style={styles.statusCard}>
        <Text style={styles.statusLabel}>Current status</Text>
        <Text style={styles.statusValue}>{formatKycStatusLabel(kycStatus)}</Text>
      </View>

      {normalizedStatus === 'rejected' && rejectionReason ? (
        <View style={styles.rejectionCard}>
          <Text style={styles.rejectionTitle}>Previous rejection</Text>
          <Text style={styles.rejectionBody}>{rejectionReason}</Text>
        </View>
      ) : null}

      <View style={styles.documentCard}>
        <Text style={styles.documentTitle}>National ID photo</Text>
        {nationalId ? <Image source={{ uri: nationalId.uri }} style={styles.previewImage} /> : null}
        <Pressable style={styles.secondaryButton} onPress={handlePickNationalId}>
          <Text style={styles.secondaryButtonText}>{nationalId ? 'Replace photo' : 'Select photo'}</Text>
        </Pressable>
      </View>

      <View style={styles.documentCard}>
        <Text style={styles.documentTitle}>Proof of residence</Text>
        {proofOfResidence ? <Image source={{ uri: proofOfResidence.uri }} style={styles.previewImage} /> : null}
        <Pressable style={styles.secondaryButton} onPress={handlePickProof}>
          <Text style={styles.secondaryButtonText}>{proofOfResidence ? 'Replace photo' : 'Select photo'}</Text>
        </Pressable>
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}

      <Pressable
        style={[styles.primaryButton, (isSubmitting || !nationalId || !proofOfResidence) ? styles.disabledButton : null]}
        onPress={handleSubmit}
        disabled={isSubmitting || !nationalId || !proofOfResidence}
      >
        <Text style={styles.primaryButtonText}>{isSubmitting ? 'Submitting...' : 'Submit for review'}</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  loadingContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
    gap: 12,
  },
  container: {
    flex: 1,
    padding: 20,
    gap: 12,
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
    padding: 12,
    gap: 4,
  },
  statusLabel: {
    color: '#64748b',
    fontSize: 12,
    textTransform: 'uppercase',
    letterSpacing: 0.6,
  },
  statusValue: {
    fontSize: 18,
    fontWeight: '700',
  },
  rejectionCard: {
    borderWidth: 1,
    borderColor: '#fecaca',
    backgroundColor: '#fef2f2',
    borderRadius: 12,
    padding: 12,
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
  documentCard: {
    borderWidth: 1,
    borderColor: '#d4d4d8',
    borderRadius: 12,
    padding: 12,
    gap: 10,
  },
  documentTitle: {
    fontSize: 16,
    fontWeight: '600',
  },
  previewImage: {
    width: '100%',
    height: 180,
    borderRadius: 10,
    backgroundColor: '#f1f5f9',
  },
  secondaryButton: {
    borderWidth: 1,
    borderColor: '#0f766e',
    borderRadius: 10,
    paddingVertical: 10,
    alignItems: 'center',
  },
  secondaryButtonText: {
    color: '#0f766e',
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
    marginTop: 'auto',
  },
  disabledButton: {
    opacity: 0.6,
  },
  primaryButtonText: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '700',
  },
});
