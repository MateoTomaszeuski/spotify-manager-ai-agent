import { useEffect, useState, useCallback } from 'react';
import { useAuth } from './useGoogleAuth';
import { websocketService, type AgentStatusUpdate } from '../services/websocket';

export function useWebSocket() {
  const auth = useAuth();
  const [isConnected, setIsConnected] = useState(false);
  const [latestStatus, setLatestStatus] = useState<AgentStatusUpdate | null>(null);

  const connect = useCallback(async () => {
    if (!auth.idToken || !auth.user?.email) {
      console.warn('❌ Cannot connect to WebSocket: no auth token or email');
      console.log('Auth state:', { 
        isAuthenticated: auth.isAuthenticated, 
        hasToken: !!auth.idToken,
        hasEmail: !!auth.user?.email 
      });
      return;
    }

    console.log('🔌 Attempting WebSocket connection...', {
      email: auth.user.email,
      hasToken: !!auth.idToken
    });

    try {
      await websocketService.connect(auth.idToken, auth.user.email);
      setIsConnected(true);
      console.log('✅ WebSocket connected successfully');
    } catch (error) {
      console.error('❌ Failed to connect WebSocket:', error);
      setIsConnected(false);
    }
  }, [auth.isAuthenticated, auth.idToken, auth.user?.email]);

  const disconnect = useCallback(async () => {
    await websocketService.disconnect();
    setIsConnected(false);
  }, []);

  useEffect(() => {
    console.log('🔄 useWebSocket effect triggered', {
      isAuthenticated: auth.isAuthenticated,
      hasToken: !!auth.idToken
    });
    
    if (auth.isAuthenticated && auth.idToken) {
      connect();
    }

    return () => {
      console.log('🔌 Cleaning up WebSocket connection');
      disconnect();
    };
  }, [auth.isAuthenticated, auth.idToken, connect, disconnect]);

  useEffect(() => {
    const unsubscribe = websocketService.onStatusUpdate((update) => {
      setLatestStatus(update);
      setIsConnected(websocketService.isConnected());
    });

    return unsubscribe;
  }, []);

  return {
    isConnected,
    latestStatus,
    connect,
    disconnect,
  };
}
