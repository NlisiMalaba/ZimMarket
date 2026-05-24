import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Switch, TextInput } from 'react-native';
import * as ImagePicker from 'expo-image-picker';

import { Text, View } from '@/components/Themed';
import {
  sellerSettingsService,
  type PickupAddress,
  type SellerProfile,
} from '@/lib/services/seller-settings-service';
import { useAuthStore } from '@/store/auth-store';

const defaultCountry = 'Zimbabwe';

const emptyAddress: PickupAddress = {
  street: '',
  suburb: '',
  city: '',
  country: defaultCountry,
};

export default function SellerSettingsScreen() {
  const updateProfile = useAuthStore((state) => state.updateProfile);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [businessName, setBusinessName] = useState('');
  const [profilePhotoKey, setProfilePhotoKey] = useState<string | null>(null);
  const [useDefaultAddress, setUseDefaultAddress] = useState(false);
  const [pickupAddress, setPickupAddress] = useState<PickupAddress>(emptyAddress);

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');

  useEffect(() => {
    let mounted = true;

    const load = async () => {
      try {
        const profile = await sellerSettingsService.getProfile();
        if (!mounted) {
          return;
        }

        applyProfile(profile);
        setError(null);
      } catch (loadError) {
        if (mounted) {
          setError(loadError instanceof Error ? loadError.message : 'Unable to load settings.');
        }
      } finally {
        if (mounted) {
          setIsLoading(false);
        }
      }
    };

    void load();

    return () => {
      mounted = false;
    };
  }, []);

  const applyProfile = (profile: SellerProfile) => {
    setFullName(profile.fullName);
    setEmail(profile.email);
    setPhone(profile.phone);
    setBusinessName(profile.businessName);
    setProfilePhotoKey(profile.profilePhotoKey);
    setUseDefaultAddress(profile.defaultPickupAddress !== null);
    setPickupAddress(profile.defaultPickupAddress ?? { ...emptyAddress });
    updateProfile({ name: profile.fullName, phone: profile.phone });
  };

  const onPickPhoto = async () => {
    const permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (!permission.granted) {
      setError('Photo library permission is required.');
      return;
    }

    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ImagePicker.MediaTypeOptions.Images,
      quality: 0.85,
    });

    if (result.canceled || !result.assets[0]) {
      return;
    }

    const asset = result.assets[0];
    setIsSaving(true);
    setError(null);

    try {
      const mimeType = asset.mimeType ?? 'image/jpeg';
      const fileName = asset.fileName ?? `profile-${Date.now()}.jpg`;
      const key = await sellerSettingsService.uploadProfilePhoto(asset.uri, fileName, mimeType);
      setProfilePhotoKey(key);
      setMessage('Photo uploaded. Save settings to apply.');
    } catch (uploadError) {
      setError(uploadError instanceof Error ? uploadError.message : 'Unable to upload photo.');
    } finally {
      setIsSaving(false);
    }
  };

  const onSaveProfile = async () => {
    setIsSaving(true);
    setMessage(null);
    setError(null);

    try {
      await sellerSettingsService.updateProfile({
        fullName: fullName.trim(),
        email: email.trim(),
        phone: phone.trim(),
        businessName: businessName.trim(),
        profilePhotoKey,
        defaultPickupAddress: useDefaultAddress
          ? {
              street: pickupAddress.street.trim(),
              suburb: pickupAddress.suburb.trim(),
              city: pickupAddress.city.trim(),
              country: pickupAddress.country.trim() || defaultCountry,
            }
          : null,
        clearDefaultPickupAddress: !useDefaultAddress,
      });

      const refreshed = await sellerSettingsService.getProfile();
      applyProfile(refreshed);
      setMessage('Settings saved.');
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : 'Unable to save settings.');
    } finally {
      setIsSaving(false);
    }
  };

  const onChangePassword = async () => {
    if (!currentPassword || !newPassword) {
      setError('Enter current and new password.');
      return;
    }

    setIsSaving(true);
    setMessage(null);
    setError(null);

    try {
      await sellerSettingsService.changePassword(currentPassword, newPassword);
      setCurrentPassword('');
      setNewPassword('');
      setMessage('Password updated.');
    } catch (passwordError) {
      setError(passwordError instanceof Error ? passwordError.message : 'Unable to change password.');
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" color="#0f766e" />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Account settings</Text>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Business profile</Text>
        <Pressable style={styles.secondaryButton} onPress={() => void onPickPhoto()} disabled={isSaving}>
          <Text style={styles.secondaryButtonText}>Upload profile photo</Text>
        </Pressable>
        <TextInput style={styles.input} value={businessName} onChangeText={setBusinessName} placeholder="Business name" />
        <TextInput style={styles.input} value={fullName} onChangeText={setFullName} placeholder="Contact name" />
        <TextInput
          style={styles.input}
          value={phone}
          onChangeText={setPhone}
          placeholder="Phone"
          keyboardType="phone-pad"
        />
        <TextInput
          style={styles.input}
          value={email}
          onChangeText={setEmail}
          placeholder="Email"
          keyboardType="email-address"
          autoCapitalize="none"
        />
      </View>

      <View style={styles.section}>
        <View style={styles.switchRow}>
          <Text style={styles.sectionTitle}>Default pickup address</Text>
          <Switch value={useDefaultAddress} onValueChange={setUseDefaultAddress} />
        </View>
        {useDefaultAddress ? (
          <>
            <TextInput
              style={styles.input}
              value={pickupAddress.street}
              onChangeText={(street) => setPickupAddress((prev) => ({ ...prev, street }))}
              placeholder="Street"
            />
            <TextInput
              style={styles.input}
              value={pickupAddress.suburb}
              onChangeText={(suburb) => setPickupAddress((prev) => ({ ...prev, suburb }))}
              placeholder="Suburb"
            />
            <TextInput
              style={styles.input}
              value={pickupAddress.city}
              onChangeText={(city) => setPickupAddress((prev) => ({ ...prev, city }))}
              placeholder="City"
            />
            <TextInput
              style={styles.input}
              value={pickupAddress.country}
              onChangeText={(country) => setPickupAddress((prev) => ({ ...prev, country }))}
              placeholder="Country"
            />
          </>
        ) : null}
      </View>

      <Pressable style={styles.primaryButton} onPress={() => void onSaveProfile()} disabled={isSaving}>
        <Text style={styles.primaryButtonText}>{isSaving ? 'Saving…' : 'Save settings'}</Text>
      </Pressable>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Change password</Text>
        <TextInput
          style={styles.input}
          value={currentPassword}
          onChangeText={setCurrentPassword}
          placeholder="Current password"
          secureTextEntry
        />
        <TextInput
          style={styles.input}
          value={newPassword}
          onChangeText={setNewPassword}
          placeholder="New password"
          secureTextEntry
        />
        <Pressable style={styles.secondaryButton} onPress={() => void onChangePassword()} disabled={isSaving}>
          <Text style={styles.secondaryButtonText}>Update password</Text>
        </Pressable>
      </View>

      {message ? <Text style={styles.success}>{message}</Text> : null}
      {error ? <Text style={styles.error}>{error}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16, gap: 12 },
  centered: { flex: 1, alignItems: 'center', justifyContent: 'center' },
  title: { fontSize: 24, fontWeight: '700' },
  section: { borderWidth: 1, borderColor: '#e5e7eb', borderRadius: 12, padding: 12, gap: 8 },
  sectionTitle: { fontSize: 15, fontWeight: '700' },
  input: {
    borderWidth: 1,
    borderColor: '#d1d5db',
    borderRadius: 10,
    paddingHorizontal: 10,
    paddingVertical: 10,
    fontSize: 14,
  },
  switchRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  primaryButton: {
    backgroundColor: '#0f766e',
    borderRadius: 10,
    paddingVertical: 12,
    alignItems: 'center',
  },
  primaryButtonText: { color: '#fff', fontWeight: '700' },
  secondaryButton: {
    backgroundColor: '#334155',
    borderRadius: 10,
    paddingVertical: 10,
    alignItems: 'center',
  },
  secondaryButtonText: { color: '#fff', fontWeight: '700' },
  success: { color: '#0f766e', fontWeight: '600' },
  error: { color: '#dc2626', fontWeight: '600' },
});
