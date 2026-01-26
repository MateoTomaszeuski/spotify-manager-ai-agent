import { AuthProvider } from 'react-oidc-context';
import type { AuthProviderProps } from 'react-oidc-context';
import { config } from '../config';

const getRedirectUri = () => {
  if (import.meta.env.DEV) {
    return 'https://127.0.0.1:5173/';
  }
  return window.location.origin + '/';
};

const redirectUri = getRedirectUri();

console.log('[KeycloakProvider] Initializing with config:', {
  authority: config.keycloak.authority,
  clientId: config.keycloak.clientId,
  redirectUri,
  origin: window.location.origin,
  isDev: import.meta.env.DEV,
});

const oidcConfig: AuthProviderProps = {
  authority: config.keycloak.authority,
  client_id: config.keycloak.clientId,
  redirect_uri: redirectUri,
  post_logout_redirect_uri: redirectUri,
  response_type: 'code',
  scope: 'openid profile email',
  automaticSilentRenew: true,
  loadUserInfo: true,
  monitorSession: false,
  onSigninCallback: () => {
    console.log('[KeycloakProvider] Sign-in callback triggered');
    window.history.replaceState({}, document.title, window.location.pathname);
  },
};

interface KeycloakProviderProps {
  children: React.ReactNode;
}

export function KeycloakProvider({ children }: KeycloakProviderProps) {
  return (
    <AuthProvider
      {...oidcConfig}
      onSigninCallback={(user) => {
        console.log('[KeycloakProvider] Sign-in successful:', user);
        window.history.replaceState({}, document.title, window.location.pathname);
      }}
    >
      {children}
    </AuthProvider>
  );
}
