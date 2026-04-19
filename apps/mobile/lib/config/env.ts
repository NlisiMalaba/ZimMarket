import { z } from 'zod';

const DEFAULT_API_BASE_URL = 'http://localhost:8080/api';

const envSchema = z.object({
  EXPO_PUBLIC_API_BASE_URL: z.url(),
});

const resolveApiBaseUrl = (): string => {
  const rawValue = process.env.EXPO_PUBLIC_API_BASE_URL?.trim();
  const candidate = rawValue && rawValue.length > 0 ? rawValue : DEFAULT_API_BASE_URL;
  const parseResult = z.url().safeParse(candidate);

  if (!parseResult.success) {
    const issues = parseResult.error.issues.map((issue) => issue.message).join(', ');
    throw new Error(`Invalid EXPO_PUBLIC_API_BASE_URL value: ${issues}`);
  }

  if (!rawValue) {
    console.warn(
      `EXPO_PUBLIC_API_BASE_URL is not set. Falling back to ${DEFAULT_API_BASE_URL}.`
    );
  }

  return parseResult.data;
};

const envParseResult = envSchema.safeParse({
  EXPO_PUBLIC_API_BASE_URL: resolveApiBaseUrl(),
});

if (!envParseResult.success) {
  const issues = envParseResult.error.issues
    .map((issue) => `${issue.path.join('.')}: ${issue.message}`)
    .join(', ');
  throw new Error(`Invalid environment configuration: ${issues}`);
}

export const env = envParseResult.data;
