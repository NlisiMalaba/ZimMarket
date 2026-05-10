const missingEnvMessage = "Missing NEXT_PUBLIC_API_URL environment variable.";
const defaultDevApiUrl = "http://localhost:8080";

function getEnvValue(name: "NEXT_PUBLIC_API_URL"): string {
  const value = process.env[name];

  if (value) {
    return value;
  }

  // Keep production strict, but provide a safe local default for developer workflows.
  if (process.env.NODE_ENV !== "production") {
    return defaultDevApiUrl;
  }

  throw new Error(missingEnvMessage);
}

export const env = {
  apiUrl: getEnvValue("NEXT_PUBLIC_API_URL"),
} as const;
