import { router } from 'expo-router';

type UnauthorizedNavigationHandler = () => void;

let unauthorizedNavigationHandler: UnauthorizedNavigationHandler = () => {
  router.replace('/(auth)/login');
};

export const setUnauthorizedNavigationHandler = (
  handler: UnauthorizedNavigationHandler
): void => {
  unauthorizedNavigationHandler = handler;
};

export const navigateToLogin = (): void => {
  unauthorizedNavigationHandler();
};
