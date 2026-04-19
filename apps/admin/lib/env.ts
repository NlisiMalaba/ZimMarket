const missingEnvMessage = "Missing NEXT_PUBLIC_API_URL environment variable.";

function getEnvValue(name: "NEXT_PUBLIC_API_URL"): string {
  const value = process.env[name];

  if (!value) {
    throw new Error(missingEnvMessage);
  }

  return value;
}

export const env = {
  apiUrl: getEnvValue("NEXT_PUBLIC_API_URL"),
} as const;
