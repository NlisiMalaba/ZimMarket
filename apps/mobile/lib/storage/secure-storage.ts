import * as SecureStore from 'expo-secure-store';
import type { StateStorage } from 'zustand/middleware';

const AUTH_STORAGE_PREFIX = 'zimmarket.auth.';

export const secureStorage: StateStorage = {
  getItem: async (name) => {
    return SecureStore.getItemAsync(`${AUTH_STORAGE_PREFIX}${name}`);
  },
  setItem: async (name, value) => {
    await SecureStore.setItemAsync(`${AUTH_STORAGE_PREFIX}${name}`, value);
  },
  removeItem: async (name) => {
    await SecureStore.deleteItemAsync(`${AUTH_STORAGE_PREFIX}${name}`);
  },
};
