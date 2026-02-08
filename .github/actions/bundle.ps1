# Navigate to the actions directory
$actionsDir = Get-ChildItem -Path "." -Directory

foreach ($dir in $actionsDir) {
    Write-Host "--- Bundling $($dir.Name) ---" -ForegroundColor Cyan
    
    # Move into the action folder
    Push-Location $dir.FullName
    
    # 1. Install dependencies locally (creates temporary node_modules)
    npm install
    
    # 2. Bundle the code into a single file in the 'dist' folder
    # We use npx so you don't have to install ncc globally
    npx @vercel/ncc build index.js -o dist --minify
    
    # 3. Cleanup local clutter (keep your local repo clean)
    Remove-Item -Recurse -Force node_modules -ErrorAction SilentlyContinue
    Remove-Item package-lock.json -ErrorAction SilentlyContinue
    
    # Move back to root
    Pop-Location
}

Write-Host "All actions bundled successfully!" -ForegroundColor Green