Whenever you change an action, you have to bundle it again. 

If you want to bundle every action at once, run bundle-all.ps1.
If you want to bundle a particular action, copy bundle-one.ps1 into the respective folder and run it. Don't forget to not commit the script later.

To run these scripts you need to enable script execution for Powershell (if you haven't done it already). Run Powershell as admin and type "set-executionpolicy remotesigned"


All the actions are bundled, which means each action and its dependency is baked into dist/index.js. Ensure action.yml points to 'dist/index.js



