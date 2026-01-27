#!/bin/bash

# Script to verify GitHub secrets are set correctly for deployment

echo "Checking GitHub repository secrets..."
echo ""
echo "Required secrets for CORS configuration:"
echo "  - FRONTEND_URL should be: https://spotify.mateo.tomaszeuski.com"
echo ""
echo "To verify your secrets:"
echo "1. Go to: https://github.com/YOUR_USERNAME/YOUR_REPO/settings/secrets/actions"
echo "2. Check that FRONTEND_URL is set correctly"
echo ""
echo "To trigger a new deployment:"
echo "1. Go to: https://github.com/YOUR_USERNAME/YOUR_REPO/actions"
echo "2. Click on 'Build and deploy ASP.Net Core app to Azure Web App - spotify-api'"
echo "3. Click 'Run workflow' > 'Run workflow'"
echo ""
echo "Or push this commit to trigger automatic deployment:"
echo "  git add ."
echo "  git commit -m 'fix: update CORS configuration with logging'"
echo "  git push origin main"
