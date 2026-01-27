import { useAuth } from '../hooks/useGoogleAuth';
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
    });
  }, [auth.isLoading, auth.isAuthenticated]);

  if (auth.isAuthenticated) {
    console.log('[LandingPage] User authenticated, redirecting to dashboard');
    window.location.href = '/dashboard';
    return null;
  }

  const handleSignIn = () => {
    console.log('[LandingPage] Initiating Google sign-in');
    auth.signIn();
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

