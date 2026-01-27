-- Migration: Add spotify_authorized column to users table
-- This migration adds a new column to track which users have been authorized to use Spotify integration

ALTER TABLE users ADD COLUMN IF NOT EXISTS spotify_authorized TIMESTAMP;

-- Optionally, you can set existing users with Spotify tokens as authorized
-- Uncomment the following line if you want to automatically authorize existing connected users:
-- UPDATE users SET spotify_authorized = CURRENT_TIMESTAMP WHERE spotify_access_token IS NOT NULL;
