import { useAuth } from 'react-oidc-context';
import { useEffect } from 'react';
import { AuthLayout } from '../components/layout/AuthLayout';
import { HeroSection } from '../components/landing/HeroSection';
import { FeaturesGrid } from '../components/landing/FeaturesGrid';
import { CTASection } from '../components/landing/CTASection';

export function LandingPage() {
  const auth = useAuth();

  useEffect(() => {
    console.log('[LandingPage] Auth state:', {
      isLoading: auth.isLoading,
      isAuthenticated: auth.isAuthenticated,
      error: auth.error,
      activeNavigator: auth.activeNavigator,
    });
    
    if (auth.error) {
      console.error('[LandingPage] Auth error:', auth.error);
    }
  }, [auth.isLoading, auth.isAuthenticated, auth.error, auth.activeNavigator]);

  if (auth.isAuthenticated) {
    console.log('[LandingPage] User authenticated, redirecting to dashboard');
    window.location.href = '/dashboard';
    return null;
  }

  const handleSignIn = async () => {
    console.log('[LandingPage] Initiating sign-in redirect');
    try {
      await auth.signinRedirect();
      console.log('[LandingPage] Sign-in redirect initiated successfully');
    } catch (error) {
      console.error('[LandingPage] Error during signinRedirect:', error);
    }
  };

  return (
    <AuthLayout>
      <div className="bg-white rounded-2xl shadow-2xl p-10 border border-slate-100">
        <HeroSection />
        <FeaturesGrid />
        <CTASection onSignIn={handleSignIn} />
      </div>
    </AuthLayout>
  );
}

