export const config = {
  keycloak: {
    clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID,
    authority: import.meta.env.VITE_KEYCLOAK_AUTHORITY,
    jwksUri: import.meta.env.VITE_KEYCLOAK_JWKS_URI,
  },
  api: {
    baseUrl: import.meta.env.VITE_API_URL,
  },
} as const;

const configStatus = {
  keycloak: {
    clientId: config.keycloak.clientId || '❌ MISSING',
    authority: config.keycloak.authority || '❌ MISSING',
    jwksUri: config.keycloak.jwksUri || '❌ MISSING',
  },
  api: {
    baseUrl: config.api.baseUrl || '❌ MISSING',
  },
};

console.log('[Config] Environment:', import.meta.env.MODE);
console.log('[Config] App Configuration:', configStatus);

if (!config.keycloak.clientId || !config.keycloak.authority) {
  console.error('[Config] ⚠️ CRITICAL: Keycloak configuration is incomplete!');
  console.error('[Config] Missing values will cause authentication to fail.');
  console.error('[Config] Please ensure environment variables are set during build.');
}
