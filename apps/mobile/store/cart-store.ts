import { create } from 'zustand';

import type { CartItem } from '@/types/cart';

type CartState = {
  items: CartItem[];
  addItem: (item: CartItem) => void;
  incrementItem: (productId: string) => void;
  decrementItem: (productId: string) => void;
  removeItem: (productId: string) => void;
  clearCart: () => void;
};

export const useCartStore = create<CartState>((set) => ({
  items: [],
  addItem: (item) =>
    set((state) => {
      const existingItem = state.items.find((entry) => entry.productId === item.productId);

      if (!existingItem) {
        return { items: [...state.items, item] };
      }

      const nextQuantity = Math.min(
        existingItem.maxQuantity ?? Number.POSITIVE_INFINITY,
        existingItem.quantity + item.quantity
      );

      return {
        items: state.items.map((entry) =>
          entry.productId === item.productId ? { ...entry, quantity: nextQuantity } : entry
        ),
      };
    }),
  incrementItem: (productId) =>
    set((state) => ({
      items: state.items.map((entry) => {
        if (entry.productId !== productId) {
          return entry;
        }

        const nextQuantity = Math.min(
          entry.maxQuantity ?? Number.POSITIVE_INFINITY,
          entry.quantity + 1
        );

        return { ...entry, quantity: nextQuantity };
      }),
    })),
  decrementItem: (productId) =>
    set((state) => ({
      items: state.items
        .map((entry) =>
          entry.productId === productId ? { ...entry, quantity: Math.max(0, entry.quantity - 1) } : entry
        )
        .filter((entry) => entry.quantity > 0),
    })),
  removeItem: (productId) =>
    set((state) => ({
      items: state.items.filter((entry) => entry.productId !== productId),
    })),
  clearCart: () => set({ items: [] }),
}));
