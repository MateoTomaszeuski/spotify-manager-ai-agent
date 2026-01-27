export const config = {
  google: {
    clientId: import.meta.env.VITE_GOOGLE_CLIENT_ID,
  },
  api: {
    baseUrl: import.meta.env.VITE_API_URL,
  },
} as const;

const configStatus = {
  google: {
    clientId: config.google.clientId || '❌ MISSING',
  },
  api: {
    baseUrl: config.api.baseUrl || '❌ MISSING',
  },
};

console.log('[Config] Environment:', import.meta.env.MODE);
console.log('[Config] App Configuration:', configStatus);

if (!config.google.clientId) {
  console.error('[Config] ⚠️ CRITICAL: Google OAuth configuration is incomplete!');
  console.error('[Config] Missing values will cause authentication to fail.');
  console.error('[Config] Please ensure environment variables are set during build.');
}
