Write-Host "--- Starting Bundle Process for $($ExecutionContext.SessionState.Path.CurrentLocation.Leaf) ---" -ForegroundColor Cyan

# 1. Install dependencies locally based on the package.json in this folder
Write-Host "Installing dependencies..."
npm install

# 2. Bundle index.js into the dist/ folder
Write-Host "Bundling with ncc..."
npx @vercel/ncc build index.js -o dist --minify

# 3. Cleanup local temporary files to keep the folder clean
Write-Host "Cleaning up..." -ForegroundColor Gray
Remove-Item -Recurse -Force node_modules -ErrorAction SilentlyContinue
Remove-Item package-lock.json -ErrorAction SilentlyContinue

Write-Host "Success! Bundled file is ready in ./dist/index.js" -ForegroundColor Green
Write-Host "Reminder: Ensure action.yml points to 'dist/index.js'" -ForegroundColor Yellow