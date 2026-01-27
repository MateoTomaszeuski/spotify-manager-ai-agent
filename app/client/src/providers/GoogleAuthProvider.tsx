import { createContext, useEffect, useState, useCallback } from 'react';
import type { ReactNode } from 'react';

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: GoogleIdConfiguration) => void;
          prompt: () => void;
          renderButton: (parent: HTMLElement, options: GoogleButtonConfiguration) => void;
          disableAutoSelect: () => void;
        };
      };
    };
  }
}

interface GoogleIdConfiguration {
  client_id: string;
  callback: (response: CredentialResponse) => void;
  auto_select?: boolean;
  cancel_on_tap_outside?: boolean;
}

interface GoogleButtonConfiguration {
  theme?: 'outline' | 'filled_blue' | 'filled_black';
  size?: 'large' | 'medium' | 'small';
  text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin';
  shape?: 'rectangular' | 'pill' | 'circle' | 'square';
  logo_alignment?: 'left' | 'center';
  width?: number;
}

interface CredentialResponse {
  credential: string;
  select_by?: string;
}

export interface GoogleUser {
  email: string;
  name: string;
  picture?: string;
  sub: string;
}

interface AuthContextType {
  user: GoogleUser | null;
  idToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  signIn: () => void;
  signOut: () => void;
  renderGoogleButton: (element: HTMLElement, options?: GoogleButtonConfiguration) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export { AuthContext };

interface GoogleAuthProviderProps {
  children: ReactNode;
}

export function GoogleAuthProvider({ children }: GoogleAuthProviderProps) {
  const [user, setUser] = useState<GoogleUser | null>(null);
  const [idToken, setIdToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;

  const decodeJwt = (token: string): GoogleUser => {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  };

  const handleCredentialResponse = useCallback((response: CredentialResponse) => {
    try {
      const userInfo = decodeJwt(response.credential);
      setUser(userInfo);
      setIdToken(response.credential);
      sessionStorage.setItem('google_id_token', response.credential);
      sessionStorage.setItem('google_user', JSON.stringify(userInfo));
      console.log('[GoogleAuth] User signed in:', userInfo.email);
    } catch (error) {
      console.error('[GoogleAuth] Error processing credential:', error);
    }
  }, []);

  useEffect(() => {
    const storedToken = sessionStorage.getItem('google_id_token');
    const storedUser = sessionStorage.getItem('google_user');

    if (storedToken && storedUser) {
      try {
        setIdToken(storedToken);
        setUser(JSON.parse(storedUser));
      } catch (error) {
        console.error('[GoogleAuth] Error restoring session:', error);
        sessionStorage.removeItem('google_id_token');
        sessionStorage.removeItem('google_user');
      }
    }

    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => {
      if (window.google) {
        window.google.accounts.id.initialize({
          client_id: clientId,
          callback: handleCredentialResponse,
          auto_select: false,
        });
        setIsLoading(false);
      }
    };
    document.body.appendChild(script);

    return () => {
      document.body.removeChild(script);
    };
  }, [clientId, handleCredentialResponse]);

  const signIn = () => {
    if (window.google) {
      window.google.accounts.id.prompt();
    }
  };

  const signOut = () => {
    setUser(null);
    setIdToken(null);
    sessionStorage.removeItem('google_id_token');
    sessionStorage.removeItem('google_user');
    if (window.google) {
      window.google.accounts.id.disableAutoSelect();
    }
  };

  const renderGoogleButton = (element: HTMLElement, options?: GoogleButtonConfiguration) => {
    if (window.google) {
      window.google.accounts.id.renderButton(element, {
        theme: 'outline',
        size: 'large',
        ...options,
      });
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        idToken,
        isAuthenticated: !!user,
        isLoading,
        signIn,
        signOut,
        renderGoogleButton,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
