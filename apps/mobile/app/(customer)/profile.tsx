import { useMemo, useState } from 'react';
import { Pressable, StyleSheet, Switch, TextInput } from 'react-native';

import { Text, View } from '@/components/Themed';
import { useAuth } from '@/hooks/useAuth';

const readString = (value: unknown): string => (typeof value === 'string' ? value : '');

export default function ProfileScreen() {
  const { user, updateProfile, logout, isLoading } = useAuth();
  const [name, setName] = useState(readString(user?.name));
  const [phone, setPhone] = useState(readString(user?.phone));
  const [newAddress, setNewAddress] = useState('');
  const [addresses, setAddresses] = useState<string[]>([
    '221 Samora Machel Ave, Harare',
    '14 Josiah Tongogara St, Bulawayo',
  ]);
  const [notificationsEnabled, setNotificationsEnabled] = useState(true);
  const [orderAlertsEnabled, setOrderAlertsEnabled] = useState(true);
  const [savedMessage, setSavedMessage] = useState<string | null>(null);

  const canSaveProfile = useMemo(
    () => name.trim().length >= 2 && phone.trim().length >= 8,
    [name, phone]
  );

  const handleSaveProfile = () => {
    if (!canSaveProfile) {
      setSavedMessage('Enter a valid name and phone number.');
      return;
    }

    updateProfile({
      name: name.trim(),
      phone: phone.trim(),
    });
    setSavedMessage('Profile updated.');
  };

  const handleAddAddress = () => {
    const normalizedAddress = newAddress.trim();
    if (normalizedAddress.length < 8) {
      setSavedMessage('Please enter a complete delivery address.');
      return;
    }

    setAddresses((previous) => [normalizedAddress, ...previous.filter((item) => item !== normalizedAddress)]);
    setNewAddress('');
    setSavedMessage('Address added.');
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Profile</Text>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Personal details</Text>
        <TextInput style={styles.input} value={name} onChangeText={setName} placeholder="Full name" />
        <TextInput
          style={styles.input}
          value={phone}
          onChangeText={setPhone}
          placeholder="Phone number"
          keyboardType="phone-pad"
        />
        <Pressable
          style={[styles.primaryButton, !canSaveProfile ? styles.disabledButton : null]}
          onPress={handleSaveProfile}
        >
          <Text style={styles.primaryButtonText}>Save profile</Text>
        </Pressable>
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Delivery addresses</Text>
        {addresses.map((address) => (
          <View key={address} style={styles.addressRow}>
            <Text style={styles.addressText}>{address}</Text>
            <Pressable onPress={() => setAddresses((prev) => prev.filter((item) => item !== address))}>
              <Text style={styles.removeText}>Remove</Text>
            </Pressable>
          </View>
        ))}
        <TextInput
          style={styles.input}
          value={newAddress}
          onChangeText={setNewAddress}
          placeholder="Add new address"
        />
        <Pressable style={styles.secondaryButton} onPress={handleAddAddress}>
          <Text style={styles.secondaryButtonText}>Add address</Text>
        </Pressable>
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Push notifications</Text>
        <View style={styles.switchRow}>
          <Text>Enable notifications</Text>
          <Switch value={notificationsEnabled} onValueChange={setNotificationsEnabled} />
        </View>
        <View style={styles.switchRow}>
          <Text>Order status alerts</Text>
          <Switch
            value={orderAlertsEnabled}
            onValueChange={setOrderAlertsEnabled}
            disabled={!notificationsEnabled}
          />
        </View>
      </View>

      {savedMessage ? <Text style={styles.message}>{savedMessage}</Text> : null}

      <Pressable style={styles.logoutButton} onPress={() => void logout()} disabled={isLoading}>
        <Text style={styles.logoutButtonText}>{isLoading ? 'Signing out...' : 'Logout'}</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
    gap: 10,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
  },
  section: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    padding: 12,
    gap: 8,
  },
  sectionTitle: {
    fontSize: 15,
    fontWeight: '700',
  },
  input: {
    borderWidth: 1,
    borderColor: '#d1d5db',
    borderRadius: 10,
    paddingHorizontal: 10,
    paddingVertical: 10,
    fontSize: 14,
  },
  addressRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: 8,
  },
  addressText: {
    flex: 1,
    color: '#374151',
    fontSize: 13,
  },
  removeText: {
    color: '#dc2626',
    fontWeight: '600',
  },
  switchRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  primaryButton: {
    backgroundColor: '#0f766e',
    borderRadius: 10,
    paddingVertical: 10,
    alignItems: 'center',
  },
  primaryButtonText: {
    color: '#ffffff',
    fontWeight: '700',
  },
  secondaryButton: {
    backgroundColor: '#334155',
    borderRadius: 10,
    paddingVertical: 10,
    alignItems: 'center',
  },
  secondaryButtonText: {
    color: '#ffffff',
    fontWeight: '700',
  },
  logoutButton: {
    marginTop: 'auto',
    borderRadius: 10,
    backgroundColor: '#b91c1c',
    paddingVertical: 12,
    alignItems: 'center',
  },
  logoutButtonText: {
    color: '#ffffff',
    fontWeight: '700',
  },
  disabledButton: {
    opacity: 0.6,
  },
  message: {
    color: '#0f766e',
    fontWeight: '600',
  },
});
