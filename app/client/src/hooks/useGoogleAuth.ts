import { useContext } from 'react';
import { AuthContext } from '../providers/GoogleAuthProvider';

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within GoogleAuthProvider');
  }
  return context;
}
