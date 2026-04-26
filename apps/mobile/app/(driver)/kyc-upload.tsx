import { useState } from 'react';
import { Image, Pressable, StyleSheet, TextInput } from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { driverOnboardingService } from '@/lib/services/driver-onboarding-service';
import type { UploadableImage } from '@/lib/services/file-upload-service';

type PickedDocument = UploadableImage;

const resolveMimeType = (value: string | null | undefined): PickedDocument['contentType'] | null => {
  if (value === 'image/jpeg' || value === 'image/png' || value === 'image/webp') {
    return value;
  }

  return null;
};

const pickImageDocument = async (): Promise<PickedDocument | null> => {
  const permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
  if (!permission.granted) {
    throw new Error('Photo library permission is required to upload driver documents.');
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
    contentType: mimeType,
    fileSizeBytes: asset.fileSize,
  };
};

export default function DriverKycUploadScreen() {
  const [licenseDocument, setLicenseDocument] = useState<PickedDocument | null>(null);
  const [vehicleDocument, setVehicleDocument] = useState<PickedDocument | null>(null);
  const [licenseNumber, setLicenseNumber] = useState('');
  const [vehicleRegistration, setVehicleRegistration] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handlePickLicense = async () => {
    setError(null);
    try {
      const picked = await pickImageDocument();
      if (picked) {
        setLicenseDocument(picked);
      }
    } catch (pickError) {
      setError(pickError instanceof Error ? pickError.message : 'Failed to pick driver license document.');
    }
  };

  const handlePickVehicle = async () => {
    setError(null);
    try {
      const picked = await pickImageDocument();
      if (picked) {
        setVehicleDocument(picked);
      }
    } catch (pickError) {
      setError(pickError instanceof Error ? pickError.message : 'Failed to pick vehicle registration document.');
    }
  };

  const canSubmit =
    !!licenseDocument &&
    !!vehicleDocument &&
    licenseNumber.trim().length >= 3 &&
    vehicleRegistration.trim().length >= 3 &&
    !isSubmitting;

  const handleSubmit = async () => {
    if (!licenseDocument || !vehicleDocument) {
      setError('Please upload both license and vehicle registration documents.');
      return;
    }

    if (licenseNumber.trim().length < 3 || vehicleRegistration.trim().length < 3) {
      setError('Please provide a valid license number and vehicle registration.');
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      const licensePresigned = await driverOnboardingService.getPresignedUploadUrl(4, licenseDocument);
      await driverOnboardingService.uploadDocument(licensePresigned.uploadUrl, licenseDocument);

      const vehiclePresigned = await driverOnboardingService.getPresignedUploadUrl(5, vehicleDocument);
      await driverOnboardingService.uploadDocument(vehiclePresigned.uploadUrl, vehicleDocument);

      await driverOnboardingService.submitKyc({
        licenseDocKey: licensePresigned.fileKey,
        vehicleDocKey: vehiclePresigned.fileKey,
        licenseNumber: licenseNumber.trim(),
        vehicleRegistration: vehicleRegistration.trim(),
      });

      router.replace('/(driver)/under-review' as never);
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Failed to submit driver onboarding documents.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.heading}>Upload onboarding documents</Text>
      <Text style={styles.description}>
        Submit your driver license and vehicle registration for verification before you can start deliveries.
      </Text>

      <TextInput
        style={styles.input}
        value={licenseNumber}
        placeholder="License number"
        autoCapitalize="characters"
        onChangeText={setLicenseNumber}
      />

      <View style={styles.documentCard}>
        <Text style={styles.documentTitle}>Driver license document</Text>
        {licenseDocument ? <Image source={{ uri: licenseDocument.uri }} style={styles.previewImage} /> : null}
        <Pressable style={styles.secondaryButton} onPress={handlePickLicense}>
          <Text style={styles.secondaryButtonText}>{licenseDocument ? 'Replace document' : 'Select document'}</Text>
        </Pressable>
      </View>

      <TextInput
        style={styles.input}
        value={vehicleRegistration}
        placeholder="Vehicle registration number"
        autoCapitalize="characters"
        onChangeText={setVehicleRegistration}
      />

      <View style={styles.documentCard}>
        <Text style={styles.documentTitle}>Vehicle registration document</Text>
        {vehicleDocument ? <Image source={{ uri: vehicleDocument.uri }} style={styles.previewImage} /> : null}
        <Pressable style={styles.secondaryButton} onPress={handlePickVehicle}>
          <Text style={styles.secondaryButtonText}>{vehicleDocument ? 'Replace document' : 'Select document'}</Text>
        </Pressable>
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}

      <Pressable
        style={[styles.primaryButton, !canSubmit ? styles.disabledButton : null]}
        onPress={handleSubmit}
        disabled={!canSubmit}
      >
        <Text style={styles.primaryButtonText}>{isSubmitting ? 'Submitting...' : 'Submit for review'}</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
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
  input: {
    borderWidth: 1,
    borderColor: '#d4d4d8',
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 16,
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
